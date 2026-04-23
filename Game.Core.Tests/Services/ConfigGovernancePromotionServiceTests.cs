using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigGovernancePromotionServiceTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // acceptance: ACC:T38.18
    [Fact]
    public void ShouldEmitAllowAndRejectAuditArtifacts_WhenPromotionAttemptsAreEvaluated()
    {
        using var workspace = TempWorkspace.Create();
        var attempts = new[]
        {
            PromotionAttemptDefinition.Allow("promotion-allow"),
            PromotionAttemptDefinition.Reject("promotion-reject")
        };

        var results = attempts
            .Select(attempt => new
            {
                Attempt = attempt,
                Result = EvaluatePromotionAttempt(workspace.LogsCiDirectory, attempt)
            })
            .ToArray();

        results.Should().HaveCount(2);

        foreach (var entry in results)
        {
            AssertAuditArtifacts(entry.Attempt, entry.Result, workspace.LogsCiDirectory);
        }

        results.Select(entry => entry.Result.Decision)
            .Should()
            .BeEquivalentTo(new[] { "allow", "reject" }, options => options.WithStrictOrdering());
    }

    // acceptance: ACC:T38.18
    [Fact]
    public void ShouldNotReuseOrOmitAuditArtifacts_WhenMultiplePromotionAttemptsShareSameWorkspace()
    {
        using var workspace = TempWorkspace.Create();
        var allowAttempt = PromotionAttemptDefinition.Allow("promotion-unique-allow");
        var rejectAttempt = PromotionAttemptDefinition.Reject("promotion-unique-reject");

        var allowResult = EvaluatePromotionAttempt(workspace.LogsCiDirectory, allowAttempt);
        var rejectResult = EvaluatePromotionAttempt(workspace.LogsCiDirectory, rejectAttempt);

        AssertAuditArtifacts(allowAttempt, allowResult, workspace.LogsCiDirectory);
        AssertAuditArtifacts(rejectAttempt, rejectResult, workspace.LogsCiDirectory);

        var artifactPaths = new[]
        {
            NormalizePath(allowResult.AuditRecordPath),
            NormalizePath(allowResult.CiSummaryPath),
            NormalizePath(rejectResult.AuditRecordPath),
            NormalizePath(rejectResult.CiSummaryPath)
        };

        artifactPaths.Should().OnlyHaveUniqueItems("each promotion attempt must emit dedicated audit artifacts");

        var emittedFiles = Directory.EnumerateFiles(workspace.LogsCiDirectory, "*.json", SearchOption.AllDirectories)
            .Select(NormalizePath)
            .ToArray();

        emittedFiles.Should().Contain(artifactPaths[0]);
        emittedFiles.Should().Contain(artifactPaths[1]);
        emittedFiles.Should().Contain(artifactPaths[2]);
        emittedFiles.Should().Contain(artifactPaths[3]);
        emittedFiles.Should().HaveCountGreaterOrEqualTo(4, "missing audit output for any attempt fails governance acceptance");
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

        serviceType.Should().NotBeNull("Task 38 requires a production config governance promotion service that evaluates gameplay tuning promotion attempts");

        var method = FindEvaluationMethod(serviceType!);
        method.Should().NotBeNull("the governance promotion service must expose a machine-readable evaluation method");

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

        if (parameterType == typeof(string))
        {
            if (ContainsAny(parameterName, "log", "ci", "artifact", "output"))
            {
                return logsCiDirectory;
            }

            if (ContainsAny(parameterName, "scenario", "case", "correlation", "promotionid", "promotion_id", "attemptid", "attempt_id"))
            {
                return attempt.ScenarioId;
            }

            if (ContainsAny(parameterName, "decision", "outcome"))
            {
                return attempt.ExpectedDecision;
            }

            if (ContainsAny(parameterName, "criteria", "policy", "governance"))
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
            return JsonDocument.Parse(ContainsAny(parameterName, "config", "candidate") ? attempt.ConfigJson : BuildRequestJson(attempt, logsCiDirectory));
        }

        if (parameterType == typeof(JsonElement))
        {
            return JsonDocument.Parse(ContainsAny(parameterName, "config", "candidate") ? attempt.ConfigJson : BuildRequestJson(attempt, logsCiDirectory)).RootElement.Clone();
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
            return BuildEnumValue(parameterType, parameterName, attempt);
        }

        if (parameterType.IsClass)
        {
            var request = CreateRequestInstance(parameterType, BuildRequestJson(attempt, logsCiDirectory));
            request.Should().NotBeNull($"request type {parameterType.FullName} must be constructible from promotion attempt JSON");
            return request;
        }

        return null;
    }

    private static object BuildEnumValue(Type enumType, string parameterName, PromotionAttemptDefinition attempt)
    {
        var names = Enum.GetNames(enumType);

        if (ContainsAny(parameterName, "decision", "outcome"))
        {
            var matchedName = names.FirstOrDefault(name => string.Equals(name, attempt.ExpectedDecision, StringComparison.OrdinalIgnoreCase));
            if (matchedName is not null)
            {
                return Enum.Parse(enumType, matchedName, ignoreCase: true);
            }
        }

        if (ContainsAny(parameterName, "target", "scope"))
        {
            var matchedName = names.FirstOrDefault(name => name.Contains("gameplay", StringComparison.OrdinalIgnoreCase));
            if (matchedName is not null)
            {
                return Enum.Parse(enumType, matchedName, ignoreCase: true);
            }
        }

        var values = Enum.GetValues(enumType);
        values.Length.Should().BeGreaterThan(0, $"enum parameter type {enumType.FullName} must define at least one value");
        return values.GetValue(0)!;
    }

    private static object? CreateRequestInstance(Type parameterType, string requestJson)
    {
        try
        {
            var deserialized = JsonSerializer.Deserialize(requestJson, parameterType, SerializerOptions);
            if (deserialized is not null)
            {
                return deserialized;
            }
        }
        catch (JsonException)
        {
        }
        catch (NotSupportedException)
        {
        }

        var defaultConstructor = parameterType.GetConstructor(Type.EmptyTypes);
        if (defaultConstructor is null)
        {
            return null;
        }

        var instance = defaultConstructor.Invoke(Array.Empty<object>());
        using var document = JsonDocument.Parse(requestJson);

        foreach (var property in parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(candidate => candidate.CanWrite))
        {
            if (!TryGetJsonProperty(document.RootElement, property.Name, out var jsonProperty))
            {
                continue;
            }

            var value = ConvertJsonProperty(jsonProperty, property.PropertyType);
            if (value is not null)
            {
                property.SetValue(instance, value);
            }
        }

        return instance;
    }

    private static object? ConvertJsonProperty(JsonElement jsonProperty, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return jsonProperty.ValueKind == JsonValueKind.String ? jsonProperty.GetString() : jsonProperty.GetRawText();
        }

        if (targetType == typeof(string[]))
        {
            return jsonProperty.ValueKind == JsonValueKind.Array
                ? jsonProperty.EnumerateArray().Select(ConvertJsonElementToString).Where(value => value.Length > 0).ToArray()
                : SplitValues(jsonProperty.ToString()).ToArray();
        }

        if (targetType == typeof(List<string>))
        {
            return jsonProperty.ValueKind == JsonValueKind.Array
                ? jsonProperty.EnumerateArray().Select(ConvertJsonElementToString).Where(value => value.Length > 0).ToList()
                : SplitValues(jsonProperty.ToString()).ToList();
        }

        if (targetType == typeof(IReadOnlyList<string>) || targetType == typeof(IEnumerable<string>))
        {
            return jsonProperty.ValueKind == JsonValueKind.Array
                ? jsonProperty.EnumerateArray().Select(ConvertJsonElementToString).Where(value => value.Length > 0).ToArray()
                : SplitValues(jsonProperty.ToString()).ToArray();
        }

        if (targetType == typeof(JsonElement))
        {
            return jsonProperty.Clone();
        }

        if (targetType == typeof(JsonDocument))
        {
            return JsonDocument.Parse(jsonProperty.GetRawText());
        }

        try
        {
            return JsonSerializer.Deserialize(jsonProperty.GetRawText(), targetType, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildRequestJson(PromotionAttemptDefinition attempt, string logsCiDirectory)
    {
        using var configDocument = JsonDocument.Parse(attempt.ConfigJson);

        var payload = new Dictionary<string, object?>
        {
            ["scenarioId"] = attempt.ScenarioId,
            ["scenario"] = attempt.ScenarioId,
            ["promotionId"] = attempt.ScenarioId,
            ["attemptId"] = attempt.ScenarioId,
            ["decision"] = attempt.ExpectedDecision,
            ["expectedDecision"] = attempt.ExpectedDecision,
            ["target"] = "gameplay-tuning",
            ["scope"] = "gameplay-tuning",
            ["requester"] = "ci",
            ["requestedBy"] = "ci",
            ["caller"] = "ci",
            ["logsCiDirectory"] = logsCiDirectory,
            ["artifactDirectory"] = logsCiDirectory,
            ["outputDirectory"] = logsCiDirectory,
            ["configJson"] = attempt.ConfigJson,
            ["candidateConfig"] = configDocument.RootElement.Clone(),
            ["config"] = configDocument.RootElement.Clone(),
            ["candidate"] = configDocument.RootElement.Clone(),
            ["governanceCriteria"] = attempt.GovernanceCriteria,
            ["criteria"] = attempt.GovernanceCriteria
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static object? ResolveTaskResult(object? rawResult)
    {
        if (rawResult is not Task task)
        {
            return rawResult;
        }

        task.GetAwaiter().GetResult();
        var resultProperty = rawResult!.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
        return resultProperty?.GetValue(rawResult);
    }

    private static void AssertAuditArtifacts(PromotionAttemptDefinition attempt, PromotionAttemptResult result, string logsCiDirectory)
    {
        result.Decision.Should().Be(attempt.ExpectedDecision);
        result.EvaluatedCriteria.Should().NotBeEmpty();

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

        File.ReadAllText(result.AuditRecordPath).Should().Contain(attempt.ScenarioId);
        File.ReadAllText(result.CiSummaryPath).Should().Contain(attempt.ScenarioId);
    }

    private static JsonDocument ReadJsonFile(string path)
    {
        File.Exists(path).Should().BeTrue($"expected governance artifact file {path} to exist");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static void AssertArtifactLocatedUnderLogsCi(string artifactPath, string logsCiDirectory)
    {
        artifactPath.Should().NotBeNullOrWhiteSpace();

        var fullArtifactPath = NormalizePath(artifactPath);
        var fullLogsCiDirectory = NormalizePath(logsCiDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        fullArtifactPath.Should().StartWith(fullLogsCiDirectory, "governance audit artifacts must stay under logs/ci");
        File.Exists(fullArtifactPath).Should().BeTrue("the governance audit artifact path must point to an existing file");
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
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
                if (TryGetJsonProperty(element, name, out var property) && property.ValueKind == JsonValueKind.String)
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

    private static bool TryGetJsonProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
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

        if (value is JsonElement element)
        {
            return ConvertJsonElementToString(element);
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
        return value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
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
                CreateConfigJson("standard", "normal", 4, null),
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
                CreateConfigJson("unknown-profile", "normal", 0, null),
                new[]
                {
                    "deterministic-runtime",
                    "runtime-safety",
                    "audit-handoff"
                });
        }

        private static string CreateConfigJson(string profile, string difficulty, int maxPlayers, string? fallbackProfile)
        {
            return JsonSerializer.Serialize(new
            {
                profile,
                difficulty,
                maxPlayers,
                fallbackProfile
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

            if (rawResult is JsonDocument document)
            {
                return FromJsonElement(document.RootElement);
            }

            if (rawResult is JsonElement element)
            {
                return FromJsonElement(element);
            }

            return new PromotionAttemptResult(
                GetRequiredObjectString(rawResult!, "Decision", "PromotionDecision", "TerminalOutcome", "Outcome"),
                GetRequiredObjectStringArray(rawResult!, "EvaluatedCriteria", "GovernanceCriteria", "Criteria", "PolicyTrace"),
                GetRequiredObjectString(rawResult!, "AuditRecordPath", "AuditPath"),
                GetRequiredObjectString(rawResult!, "CiSummaryPath", "SummaryArtifactPath", "SummaryPath"));
        }

        private static PromotionAttemptResult FromJsonElement(JsonElement element)
        {
            return new PromotionAttemptResult(
                GetRequiredString(element, "decision", "promotionDecision", "terminalOutcome", "outcome"),
                GetRequiredStringArray(element, "evaluatedCriteria", "governanceCriteria", "criteria", "criteriaEvaluations"),
                GetRequiredString(element, "auditRecordPath", "auditPath"),
                GetRequiredString(element, "ciSummaryPath", "summaryArtifactPath", "summaryPath"));
        }
    }

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            LogsCiDirectory = Path.Combine(rootDirectory, "logs", "ci", "task-38-governance-promotion");
            Directory.CreateDirectory(LogsCiDirectory);
        }

        public string RootDirectory { get; }

        public string LogsCiDirectory { get; }

        public static TempWorkspace Create()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "lastking-config-governance-promotion-" + Guid.NewGuid().ToString("N"));
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
