using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigGovernanceAuditTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // acceptance: ACC:T38.18
    [Fact]
    public void ShouldEmitAuditArtifacts_WhenPromotionAttemptIsEvaluated()
    {
        using var workspace = TempWorkspace.Create();
        var attempts = new[]
        {
            PromotionAttemptDefinition.Allow("promotion-allow"),
            PromotionAttemptDefinition.Reject("promotion-reject")
        };

        foreach (var attempt in attempts)
        {
            var result = EvaluatePromotionAttempt(workspace.LogsCiDirectory, attempt);

            result.Decision.Should().Be(attempt.ExpectedDecision);
            result.EvaluatedCriteria.Should().NotBeEmpty();
            AssertAuditArtifacts(attempt, result, workspace.LogsCiDirectory);
        }
    }

    [Fact]
    public void ShouldIncludeRejectDecisionAndEvaluatedCriteria_WhenPromotionAttemptIsRejected()
    {
        using var workspace = TempWorkspace.Create();
        var attempt = PromotionAttemptDefinition.Reject("promotion-reject-artifact-contract");

        var result = EvaluatePromotionAttempt(workspace.LogsCiDirectory, attempt);

        result.Decision.Should().Be("reject");
        result.EvaluatedCriteria.Should().NotBeEmpty();
        AssertArtifactLocatedUnderLogsCi(result.AuditRecordPath, workspace.LogsCiDirectory);
        AssertArtifactLocatedUnderLogsCi(result.CiSummaryPath, workspace.LogsCiDirectory);

        using var auditRecord = ReadJsonFile(result.AuditRecordPath);
        using var summaryRecord = ReadJsonFile(result.CiSummaryPath);
        GetRequiredString(auditRecord.RootElement, "decision", "promotionDecision", "terminalOutcome", "outcome").Should().Be("reject");
        GetRequiredStringArray(auditRecord.RootElement, "evaluatedCriteria", "governanceCriteria", "criteria", "criteriaEvaluations").Should().NotBeEmpty();
        GetRequiredString(summaryRecord.RootElement, "decision", "promotionDecision", "terminalOutcome", "outcome").Should().Be("reject");
        GetRequiredStringArray(summaryRecord.RootElement, "evaluatedCriteria", "governanceCriteria", "criteria", "criteriaEvaluations").Should().NotBeEmpty();
    }

    private static PromotionAttemptResult EvaluatePromotionAttempt(string logsCiDirectory, PromotionAttemptDefinition attempt)
    {
        LoadCoreAssembly();
        var serviceType = FindType(
            "Game.Core.Services.ConfigGovernancePromotionService",
            "Game.Core.Services.ConfigPromotionGovernanceService",
            "Game.Core.Services.ConfigGovernanceService",
            "Game.Core.Services.GameplayTuningGovernanceService",
            "Game.Core.Services.ConfigPromotionService");
        serviceType.Should().NotBeNull("Task 38 requires a production config governance promotion service that evaluates promotion attempts");

        var method = FindEvaluationMethod(serviceType!);
        method.Should().NotBeNull("the governance promotion service must expose an evaluation method that returns machine-readable audit metadata");

        var instance = method!.IsStatic ? null : CreateInstance(serviceType!, logsCiDirectory);
        var arguments = BuildArguments(method.GetParameters(), logsCiDirectory, attempt);
        var rawResult = method.Invoke(instance, arguments);

        return PromotionAttemptResult.FromObject(ResolveTaskResult(rawResult));
    }

    private static void LoadCoreAssembly()
    {
        foreach (var assemblyName in new[] { "Game.Core", "Lastking.Game.Core" })
        {
            try
            {
                Assembly.Load(assemblyName);
            }
            catch
            {
            }
        }
    }

    private static Type? FindType(params string[] fullNames)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type => type.FullName is not null && fullNames.Contains(type.FullName, StringComparer.Ordinal));
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    private static MethodInfo? FindEvaluationMethod(Type serviceType)
    {
        var methodNames = new[]
        {
            "EvaluatePromotionAttempt",
            "EvaluatePromotion",
            "Promote",
            "PromoteConfig",
            "ExecutePromotionAttempt",
            "Evaluate",
            "ValidatePromotion"
        };

        return serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => methodNames.Contains(method.Name, StringComparer.Ordinal))
            .Where(method => method.ReturnType != typeof(void))
            .FirstOrDefault(method => CanBuildArguments(method.GetParameters()));
    }

    private static object CreateInstance(Type serviceType, string logsCiDirectory)
    {
        var stringConstructor = serviceType.GetConstructor(new[] { typeof(string) });
        if (stringConstructor is not null)
        {
            return stringConstructor.Invoke(new object[] { logsCiDirectory });
        }

        var directoryConstructor = serviceType.GetConstructor(new[] { typeof(DirectoryInfo) });
        if (directoryConstructor is not null)
        {
            return directoryConstructor.Invoke(new object[] { new DirectoryInfo(logsCiDirectory) });
        }

        var defaultConstructor = serviceType.GetConstructor(Type.EmptyTypes);
        defaultConstructor.Should().NotBeNull("the governance promotion service must be constructible without engine-only infrastructure");
        return defaultConstructor!.Invoke(Array.Empty<object>());
    }

    private static bool CanBuildArguments(ParameterInfo[] parameters)
    {
        return parameters.All(parameter =>
            parameter.ParameterType == typeof(string)
            || parameter.ParameterType == typeof(DirectoryInfo)
            || parameter.ParameterType == typeof(JsonDocument)
            || parameter.ParameterType == typeof(JsonElement)
            || parameter.ParameterType == typeof(bool)
            || parameter.ParameterType == typeof(string[])
            || parameter.ParameterType == typeof(List<string>)
            || parameter.ParameterType == typeof(IReadOnlyList<string>)
            || parameter.ParameterType == typeof(IEnumerable<string>)
            || parameter.ParameterType.IsEnum
            || parameter.ParameterType.IsClass);
    }

    private static object?[] BuildArguments(ParameterInfo[] parameters, string logsCiDirectory, PromotionAttemptDefinition attempt)
    {
        return parameters.Select(parameter => BuildArgument(parameter, logsCiDirectory, attempt)).ToArray();
    }

    private static object? BuildArgument(ParameterInfo parameter, string logsCiDirectory, PromotionAttemptDefinition attempt)
    {
        var parameterName = parameter.Name ?? string.Empty;
        var parameterType = parameter.ParameterType;
        var requestJson = BuildRequestJson(attempt);

        if (parameterType == typeof(string))
        {
            if (ContainsAny(parameterName, "log", "ci", "artifact", "output"))
            {
                return logsCiDirectory;
            }

            if (ContainsAny(parameterName, "scenario", "case", "correlation", "promotionid", "promotion_id", "attemptid"))
            {
                return attempt.ScenarioId;
            }

            if (ContainsAny(parameterName, "decision", "outcome"))
            {
                return attempt.ExpectedDecision;
            }

            if (ContainsAny(parameterName, "criteria"))
            {
                return string.Join(",", attempt.GovernanceCriteria);
            }

            if (ContainsAny(parameterName, "target", "scope"))
            {
                return "gameplay-tuning";
            }

            if (ContainsAny(parameterName, "requester", "requestedby", "requested_by", "caller"))
            {
                return "ci";
            }

            return attempt.ConfigJson;
        }

        if (parameterType == typeof(DirectoryInfo))
        {
            return new DirectoryInfo(logsCiDirectory);
        }

        if (parameterType == typeof(JsonDocument))
        {
            return JsonDocument.Parse(ContainsAny(parameterName, "config", "candidate") ? attempt.ConfigJson : requestJson);
        }

        if (parameterType == typeof(JsonElement))
        {
            return JsonDocument.Parse(ContainsAny(parameterName, "config", "candidate") ? attempt.ConfigJson : requestJson).RootElement.Clone();
        }

        if (parameterType == typeof(bool))
        {
            return false;
        }

        if (parameterType == typeof(string[]))
        {
            return attempt.GovernanceCriteria.ToArray();
        }

        if (parameterType == typeof(List<string>))
        {
            return attempt.GovernanceCriteria.ToList();
        }

        if (parameterType == typeof(IReadOnlyList<string>) || parameterType == typeof(IEnumerable<string>))
        {
            return attempt.GovernanceCriteria.ToArray();
        }

        if (parameterType.IsEnum)
        {
            return BuildEnumValue(parameterType);
        }

        if (parameterType.IsClass)
        {
            var request = CreateRequestInstance(parameterType, requestJson);
            request.Should().NotBeNull($"request type {parameterType.FullName} must be constructible from promotion attempt JSON");
            SetWritableStringProperty(request!, "LogsCiDirectory", logsCiDirectory);
            SetWritableStringProperty(request!, "LogDirectory", logsCiDirectory);
            SetWritableStringProperty(request!, "ArtifactDirectory", logsCiDirectory);
            SetWritableStringProperty(request!, "ScenarioId", attempt.ScenarioId);
            SetWritableStringProperty(request!, "CorrelationId", attempt.ScenarioId);
            SetWritableStringProperty(request!, "PromotionId", attempt.ScenarioId);
            SetWritableStringProperty(request!, "AttemptId", attempt.ScenarioId);
            SetWritableStringProperty(request!, "ConfigJson", attempt.ConfigJson);
            SetWritableStringProperty(request!, "CandidateConfigJson", attempt.ConfigJson);
            SetWritableStringProperty(request!, "PromotionTarget", "gameplay-tuning");
            SetWritableStringProperty(request!, "RequestedBy", "ci");
            SetWritableStringListProperty(request!, "GovernanceCriteria", attempt.GovernanceCriteria);
            SetWritableStringListProperty(request!, "EvaluatedCriteria", attempt.GovernanceCriteria);
            return request;
        }

        throw new InvalidOperationException($"Unsupported promotion parameter {parameter.Name} of type {parameterType.FullName}.");
    }

    private static object? CreateRequestInstance(Type parameterType, string requestJson)
    {
        try
        {
            return JsonSerializer.Deserialize(requestJson, parameterType, SerializerOptions);
        }
        catch
        {
            try
            {
                return Activator.CreateInstance(parameterType);
            }
            catch
            {
                return null;
            }
        }
    }

    private static object BuildEnumValue(Type enumType)
    {
        var names = Enum.GetNames(enumType);
        var preferred = names.FirstOrDefault(name => string.Equals(name, "Ci", StringComparison.OrdinalIgnoreCase))
            ?? names.FirstOrDefault(name => string.Equals(name, "Manual", StringComparison.OrdinalIgnoreCase))
            ?? names.FirstOrDefault(name => string.Equals(name, "Default", StringComparison.OrdinalIgnoreCase))
            ?? names.FirstOrDefault();

        preferred.Should().NotBeNull($"enum {enumType.FullName} must define at least one value");
        return Enum.Parse(enumType, preferred!, ignoreCase: true);
    }

    private static string BuildRequestJson(PromotionAttemptDefinition attempt)
    {
        var payload = new
        {
            scenarioId = attempt.ScenarioId,
            promotionId = attempt.ScenarioId,
            attemptId = attempt.ScenarioId,
            promotionTarget = "gameplay-tuning",
            requestedBy = "ci",
            configJson = attempt.ConfigJson,
            candidateConfigJson = attempt.ConfigJson,
            governanceCriteria = attempt.GovernanceCriteria,
            metadata = new
            {
                handoff = "ci-manual",
                expectedDecision = attempt.ExpectedDecision
            }
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetWritableStringProperty(object target, string propertyName, string value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null || !property.CanWrite || property.PropertyType != typeof(string))
        {
            return;
        }

        try
        {
            property.SetValue(target, value);
        }
        catch
        {
        }
    }

    private static void SetWritableStringListProperty(object target, string propertyName, IReadOnlyList<string> values)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null || !property.CanWrite)
        {
            return;
        }

        try
        {
            if (property.PropertyType == typeof(string[]))
            {
                property.SetValue(target, values.ToArray());
                return;
            }

            if (property.PropertyType == typeof(List<string>))
            {
                property.SetValue(target, values.ToList());
                return;
            }

            if (property.PropertyType == typeof(IReadOnlyList<string>) || property.PropertyType == typeof(IEnumerable<string>))
            {
                property.SetValue(target, values.ToArray());
            }
        }
        catch
        {
        }
    }

    private static object? ResolveTaskResult(object? rawResult)
    {
        if (rawResult is null)
        {
            return null;
        }

        var resultType = rawResult.GetType();
        if (!typeof(System.Threading.Tasks.Task).IsAssignableFrom(resultType))
        {
            return rawResult;
        }

        ((System.Threading.Tasks.Task)rawResult).GetAwaiter().GetResult();
        return resultType.GetProperty("Result")?.GetValue(rawResult);
    }

    private static void AssertAuditArtifacts(PromotionAttemptDefinition attempt, PromotionAttemptResult result, string logsCiDirectory)
    {
        AssertArtifactLocatedUnderLogsCi(result.AuditRecordPath, logsCiDirectory);
        AssertArtifactLocatedUnderLogsCi(result.CiSummaryPath, logsCiDirectory);

        using var auditRecord = ReadJsonFile(result.AuditRecordPath);
        using var summaryRecord = ReadJsonFile(result.CiSummaryPath);
        var auditDecision = GetRequiredString(auditRecord.RootElement, "decision", "promotionDecision", "terminalOutcome", "outcome");
        var summaryDecision = GetRequiredString(summaryRecord.RootElement, "decision", "promotionDecision", "terminalOutcome", "outcome");
        var auditCriteria = GetRequiredStringArray(auditRecord.RootElement, "evaluatedCriteria", "governanceCriteria", "criteria", "criteriaEvaluations");
        var summaryCriteria = GetRequiredStringArray(summaryRecord.RootElement, "evaluatedCriteria", "governanceCriteria", "criteria", "criteriaEvaluations");

        auditDecision.Should().Be(attempt.ExpectedDecision);
        summaryDecision.Should().Be(attempt.ExpectedDecision);
        auditCriteria.Should().NotBeEmpty();
        summaryCriteria.Should().NotBeEmpty();
        auditCriteria.Should().Contain(result.EvaluatedCriteria);
        summaryCriteria.Should().Contain(result.EvaluatedCriteria);
    }

    private static JsonDocument ReadJsonFile(string path)
    {
        File.Exists(path).Should().BeTrue($"expected governance artifact file {path} to exist");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static void AssertArtifactLocatedUnderLogsCi(string artifactPath, string logsCiDirectory)
    {
        artifactPath.Should().NotBeNullOrWhiteSpace();
        var fullArtifactPath = Path.GetFullPath(artifactPath);
        var fullLogsCiDirectory = Path.GetFullPath(logsCiDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        fullArtifactPath.Should().StartWith(fullLogsCiDirectory, "governance audit artifacts must stay under logs/ci");
        File.Exists(fullArtifactPath).Should().BeTrue("the governance audit artifact path must point to an existing file");
    }

    private static string GetRequiredString(JsonElement element, params string[] names)
    {
        var property = FindProperty(element, names);
        property.Should().NotBeNull($"artifact must contain one of: {string.Join(", ", names)}");
        property!.Value.ValueKind.Should().Be(JsonValueKind.String);
        return property.Value.GetString() ?? string.Empty;
    }

    private static IReadOnlyList<string> GetRequiredStringArray(JsonElement element, params string[] names)
    {
        var property = FindProperty(element, names);
        property.Should().NotBeNull($"artifact must contain one of: {string.Join(", ", names)}");

        if (property!.Value.ValueKind == JsonValueKind.String)
        {
            return SplitValues(property.Value.GetString() ?? string.Empty);
        }

        property.Value.ValueKind.Should().Be(JsonValueKind.Array);
        return property.Value.EnumerateArray()
            .Select(ConvertJsonElementToString)
            .Where(value => value.Length > 0)
            .ToArray();
    }

    private static string ConvertJsonElementToString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString() ?? string.Empty;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in new[] { "name", "key", "code", "id", "reasonKey" })
            {
                if (element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString() ?? string.Empty;
                }
            }
        }

        return element.ToString();
    }

    private static JsonElement? FindProperty(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static string GetRequiredObjectString(object target, params string[] names)
    {
        var value = GetOptionalObjectValue(target, names);
        value.Should().NotBeNull($"promotion result must expose one of: {string.Join(", ", names)}");
        return ConvertValueToString(value);
    }

    private static IReadOnlyList<string> GetRequiredObjectStringArray(object target, params string[] names)
    {
        var value = GetOptionalObjectValue(target, names);
        value.Should().NotBeNull($"promotion result must expose one of: {string.Join(", ", names)}");

        if (value is string text)
        {
            return SplitValues(text);
        }

        if (value is IEnumerable values)
        {
            return values.Cast<object?>()
                .Select(ConvertValueToString)
                .Where(item => item.Length > 0)
                .ToArray();
        }

        return new[] { ConvertValueToString(value) };
    }

    private static object? GetOptionalObjectValue(object target, params string[] names)
    {
        if (target is IDictionary<string, object?> dictionary)
        {
            var key = dictionary.Keys.FirstOrDefault(candidate => names.Any(name => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)));
            return key is null ? null : dictionary[key];
        }

        var property = target.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(candidate => names.Any(name => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)));
        return property?.GetValue(target);
    }

    private static string ConvertValueToString(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return text;
        }

        var type = value.GetType();
        if (!type.IsPrimitive && value is not decimal)
        {
            foreach (var name in new[] { "Name", "Key", "Code", "Id", "ReasonKey" })
            {
                var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property is not null)
                {
                    var propertyValue = property.GetValue(value);
                    if (propertyValue is not null)
                    {
                        return Convert.ToString(propertyValue, CultureInfo.InvariantCulture) ?? string.Empty;
                    }
                }
            }
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static IReadOnlyList<string> SplitValues(string value)
    {
        return value.Split(new[] { ',', ';', '|'}, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();
    }

    private sealed record PromotionAttemptDefinition(
        string ScenarioId,
        string ExpectedDecision,
        string ConfigJson,
        IReadOnlyList<string> GovernanceCriteria)
    {
        public static PromotionAttemptDefinition Allow(string scenarioId)
        {
            return new PromotionAttemptDefinition(
                scenarioId,
                "allow",
                "{\"profile\":\"standard\",\"difficulty\":\"normal\",\"maxPlayers\":4,\"fallbackProfile\":null}",
                new[]
                {
                    "deterministic-runtime",
                    "runtime-safety",
                    "audit-handoff"
                });
        }

        public static PromotionAttemptDefinition Reject(string scenarioId)
        {
            return new PromotionAttemptDefinition(
                scenarioId,
                "reject",
                "{\"profile\":\"unknown-profile\",\"difficulty\":\"normal\",\"maxPlayers\":0,\"fallbackProfile\":null}",
                new[]
                {
                    "deterministic-runtime",
                    "runtime-safety",
                    "audit-handoff"
                });
        }
    }

    private sealed record PromotionAttemptResult(
        string Decision,
        IReadOnlyList<string> EvaluatedCriteria,
        string AuditRecordPath,
        string CiSummaryPath)
    {
        public static PromotionAttemptResult FromObject(object? rawResult)
        {
            rawResult.Should().NotBeNull("config governance promotion must return a machine-readable result for each promotion attempt");
            return new PromotionAttemptResult(
                GetRequiredObjectString(rawResult!, "Decision", "PromotionDecision", "TerminalOutcome", "Outcome"),
                GetRequiredObjectStringArray(rawResult!, "EvaluatedCriteria", "GovernanceCriteria", "Criteria", "PolicyTrace"),
                GetRequiredObjectString(rawResult!, "AuditRecordPath", "AuditPath"),
                GetRequiredObjectString(rawResult!, "CiSummaryPath", "SummaryArtifactPath", "SummaryPath"));
        }
    }

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            LogsCiDirectory = Path.Combine(rootDirectory, "logs", "ci", "task-38-governance");
            Directory.CreateDirectory(LogsCiDirectory);
        }

        public string RootDirectory { get; }

        public string LogsCiDirectory { get; }

        public static TempWorkspace Create()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "lastking-config-governance-audit-" + Guid.NewGuid().ToString("N"));
            return new TempWorkspace(rootDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, recursive: true);
            }
        }
    }
}
