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

public sealed class ConfigAuditArtifactsTests
{
    // acceptance: ACC:T37.10
    [Fact]
    public void ShouldEmitStandardizedAuditEntriesAndCiSummaryArtifacts_WhenValidationDecisionIsRejected()
    {
        using var workspace = TempWorkspace.Create();
        var configJson = "{\"profile\":\"unknown-profile\",\"difficulty\":\"normal\",\"maxPlayers\":4,\"fallbackProfile\":null}";

        var result = EvaluateWithProductionPipeline(workspace.LogsCiDirectory, configJson, "reject-unknown-profile");

        result.TerminalOutcome.Should().Be("reject");
        result.ReasonIdentifiers.Should().Contain("CONFIG_PROFILE_UNKNOWN");
        AssertArtifactLocatedUnderLogsCi(result.AuditRecordPath, workspace.LogsCiDirectory);
        AssertArtifactLocatedUnderLogsCi(result.CiSummaryPath, workspace.LogsCiDirectory);

        using var auditRecord = ReadJsonFile(result.AuditRecordPath);
        using var summaryArtifact = ReadJsonFile(result.CiSummaryPath);
        AssertStandardizedArtifact(auditRecord.RootElement, result, requireReasons: true);
        AssertStandardizedArtifact(summaryArtifact.RootElement, result, requireReasons: true);
        GetRequiredString(auditRecord.RootElement, "caller").Should().Be("ConfigValidationPipeline");
        GetRequiredString(auditRecord.RootElement, "target").Should().Be("runtime-config");
        GetRequiredString(summaryArtifact.RootElement, "caller").Should().Be("ConfigValidationPipeline");
        GetRequiredString(summaryArtifact.RootElement, "target").Should().Be("runtime-config");
    }

    // acceptance: ACC:T37.17
    [Fact]
    public void ShouldProducePolicyConsistentTerminalOutcomes_WhenValidInvalidAndBoundaryMatrixIsEvaluated()
    {
        using var workspace = TempWorkspace.Create();
        var matrix = new[]
        {
            new ConfigCase(
                "valid-standard",
                "accept",
                Array.Empty<string>(),
                "{\"profile\":\"standard\",\"difficulty\":\"normal\",\"maxPlayers\":4,\"fallbackProfile\":null}"),
            new ConfigCase(
                "invalid-unknown-profile",
                "reject",
                new[] { "CONFIG_PROFILE_UNKNOWN" },
                "{\"profile\":\"unknown-profile\",\"difficulty\":\"normal\",\"maxPlayers\":4,\"fallbackProfile\":null}"),
            new ConfigCase(
                "boundary-fallback-minimum-players",
                "fallback",
                new[] { "CONFIG_MAX_PLAYERS_BELOW_MINIMUM" },
                "{\"profile\":\"standard\",\"difficulty\":\"normal\",\"maxPlayers\":0,\"fallbackProfile\":\"standard\"}")
        };

        foreach (var caseDefinition in matrix)
        {
            var result = EvaluateWithProductionPipeline(workspace.LogsCiDirectory, caseDefinition.ConfigJson, caseDefinition.ScenarioId);

            result.TerminalOutcome.Should().Be(caseDefinition.ExpectedOutcome);
            if (caseDefinition.ExpectedReasonIdentifiers.Count > 0)
            {
                result.ReasonIdentifiers.Should().Contain(caseDefinition.ExpectedReasonIdentifiers);
            }
            AssertArtifactLocatedUnderLogsCi(result.AuditRecordPath, workspace.LogsCiDirectory);
            AssertArtifactLocatedUnderLogsCi(result.CiSummaryPath, workspace.LogsCiDirectory);
        }
    }

    // acceptance: ACC:T37.7
    [Fact]
    public void ShouldExposeMachineVerifiableReasonIdentifiers_WhenRejectOrFallbackOutcomeIsProduced()
    {
        using var workspace = TempWorkspace.Create();
        var negativeCases = new[]
        {
            new ConfigCase(
                "reject-unknown-difficulty",
                "reject",
                new[] { "CONFIG_DIFFICULTY_UNKNOWN" },
                "{\"profile\":\"standard\",\"difficulty\":\"impossible\",\"maxPlayers\":4,\"fallbackProfile\":null}"),
            new ConfigCase(
                "fallback-empty-profile",
                "fallback",
                new[] { "CONFIG_PROFILE_EMPTY" },
                "{\"profile\":\"\",\"difficulty\":\"normal\",\"maxPlayers\":4,\"fallbackProfile\":\"standard\"}")
        };

        foreach (var caseDefinition in negativeCases)
        {
            var result = EvaluateWithProductionPipeline(workspace.LogsCiDirectory, caseDefinition.ConfigJson, caseDefinition.ScenarioId);
            using var auditRecord = ReadJsonFile(result.AuditRecordPath);
            var auditReasonIdentifiers = GetRequiredStringArray(auditRecord.RootElement, "reasonIdentifiers", "reason_ids", "reasons");

            result.TerminalOutcome.Should().Be(caseDefinition.ExpectedOutcome);
            result.ReasonIdentifiers.Should().Contain(caseDefinition.ExpectedReasonIdentifiers);
            auditReasonIdentifiers.Should().Contain(caseDefinition.ExpectedReasonIdentifiers);
        }
    }

    // acceptance: ACC:T37.8
    [Fact]
    public void ShouldWriteOneAuditRecordAndOneSummaryArtifact_WhenConfigEvaluationCompletes()
    {
        using var workspace = TempWorkspace.Create();
        var configJson = "{\"profile\":\"standard\",\"difficulty\":\"normal\",\"maxPlayers\":4,\"fallbackProfile\":null}";

        var result = EvaluateWithProductionPipeline(workspace.LogsCiDirectory, configJson, "single-accept-artifact");

        var auditFiles = Directory.EnumerateFiles(workspace.LogsCiDirectory, "*.json", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).Contains("audit", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var summaryFiles = Directory.EnumerateFiles(workspace.LogsCiDirectory, "*.json", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path).Contains("summary", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        auditFiles.Should().ContainSingle().Which.Should().Be(result.AuditRecordPath);
        summaryFiles.Should().ContainSingle().Which.Should().Be(result.CiSummaryPath);

        using var auditRecord = ReadJsonFile(result.AuditRecordPath);
        using var summaryArtifact = ReadJsonFile(result.CiSummaryPath);
        AssertStandardizedArtifact(auditRecord.RootElement, result, requireReasons: false);
        AssertStandardizedArtifact(summaryArtifact.RootElement, result, requireReasons: false);
    }

    [Fact]
    public void ShouldRejectAbsoluteLogsCiPath_WhenPipelineConstructedWithOutOfBoundaryPath()
    {
        var absolutePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "lastking-outside-logs-ci"));
        var configJson = "{\"profile\":\"standard\",\"difficulty\":\"normal\",\"maxPlayers\":4}";
        var pipeline = new Game.Core.Services.ConfigValidationPipeline(absolutePath);

        var action = () => pipeline.Evaluate(configJson, "absolute-path-reject");

        action.Should().Throw<InvalidOperationException>();
    }

    private static EvaluationResult EvaluateWithProductionPipeline(string logsCiDirectory, string configJson, string scenarioId)
    {
        LoadCoreAssembly();
        var pipelineType = FindType(
            "Game.Core.Services.ConfigValidationPipeline",
            "Game.Core.Services.ConfigPolicyPipeline",
            "Game.Core.Configuration.ConfigValidationPipeline");
        pipelineType.Should().NotBeNull("Task 37 requires a production config validation pipeline that emits audit artifacts");

        var method = FindEvaluationMethod(pipelineType!);
        method.Should().NotBeNull("the pipeline must expose an evaluation method that accepts config input and a logs/ci artifact directory");

        var instance = method!.IsStatic ? null : CreatePipelineInstance(pipelineType!, logsCiDirectory);
        var arguments = BuildArguments(method.GetParameters(), logsCiDirectory, configJson, scenarioId);
        var rawResult = method.Invoke(instance, arguments);
        var resolvedResult = ResolveTaskResult(rawResult);

        return EvaluationResult.From(resolvedResult);
    }

    private static void LoadCoreAssembly()
    {
        foreach (var assemblyName in new[] { "Game.Core", "Lastking.Game.Core" })
        {
            try
            {
                Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException)
            {
            }
        }
    }

    private static Type? FindType(params string[] fullNames)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => fullNames.Contains(type.FullName, StringComparer.Ordinal));
    }

    private static MethodInfo? FindEvaluationMethod(Type pipelineType)
    {
        var methodNames = new[]
        {
            "Evaluate",
            "EvaluateConfig",
            "Validate",
            "ValidateConfig",
            "EvaluateAndWriteArtifacts",
            "ValidateAndWriteArtifacts"
        };

        return pipelineType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => methodNames.Contains(method.Name, StringComparer.Ordinal))
            .Where(method => method.ReturnType != typeof(void))
            .FirstOrDefault(method => CanBuildArguments(method.GetParameters()));
    }

    private static object CreatePipelineInstance(Type pipelineType, string logsCiDirectory)
    {
        var stringConstructor = pipelineType.GetConstructor(new[] { typeof(string) });
        if (stringConstructor is not null)
        {
            return stringConstructor.Invoke(new object[] { logsCiDirectory });
        }

        var directoryConstructor = pipelineType.GetConstructor(new[] { typeof(DirectoryInfo) });
        if (directoryConstructor is not null)
        {
            return directoryConstructor.Invoke(new object[] { new DirectoryInfo(logsCiDirectory) });
        }

        var defaultConstructor = pipelineType.GetConstructor(Type.EmptyTypes);
        defaultConstructor.Should().NotBeNull("the pipeline must be constructible without external infrastructure");
        return defaultConstructor!.Invoke(Array.Empty<object>());
    }

    private static bool CanBuildArguments(ParameterInfo[] parameters)
    {
        return parameters.All(parameter =>
            parameter.ParameterType == typeof(string)
            || parameter.ParameterType == typeof(DirectoryInfo)
            || parameter.ParameterType == typeof(JsonDocument)
            || parameter.ParameterType == typeof(JsonElement)
            || parameter.ParameterType.IsClass);
    }

    private static object?[] BuildArguments(ParameterInfo[] parameters, string logsCiDirectory, string configJson, string scenarioId)
    {
        return parameters.Select(parameter => BuildArgument(parameter, logsCiDirectory, configJson, scenarioId)).ToArray();
    }

    private static object? BuildArgument(ParameterInfo parameter, string logsCiDirectory, string configJson, string scenarioId)
    {
        var parameterName = parameter.Name ?? string.Empty;
        var parameterType = parameter.ParameterType;

        if (parameterType == typeof(string))
        {
            if (ContainsAny(parameterName, "log", "ci", "artifact", "output"))
            {
                return logsCiDirectory;
            }

            if (ContainsAny(parameterName, "scenario", "case", "id", "correlation"))
            {
                return scenarioId;
            }

            return configJson;
        }

        if (parameterType == typeof(DirectoryInfo))
        {
            return new DirectoryInfo(logsCiDirectory);
        }

        if (parameterType == typeof(JsonDocument))
        {
            return JsonDocument.Parse(configJson);
        }

        if (parameterType == typeof(JsonElement))
        {
            return JsonDocument.Parse(configJson).RootElement.Clone();
        }

        if (parameterType.IsClass)
        {
            var request = JsonSerializer.Deserialize(configJson, parameterType, SerializerOptions)
                ?? Activator.CreateInstance(parameterType);
            request.Should().NotBeNull($"request type {parameterType.FullName} must be constructible from config JSON");
            SetWritableStringProperty(request!, "LogsCiDirectory", logsCiDirectory);
            SetWritableStringProperty(request!, "LogDirectory", logsCiDirectory);
            SetWritableStringProperty(request!, "ArtifactDirectory", logsCiDirectory);
            SetWritableStringProperty(request!, "ScenarioId", scenarioId);
            SetWritableStringProperty(request!, "CorrelationId", scenarioId);
            return request;
        }

        throw new InvalidOperationException($"Unsupported pipeline parameter {parameter.Name} of type {parameterType.FullName}.");
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetWritableStringProperty(object target, string propertyName, string value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is not null && property.CanWrite && property.PropertyType == typeof(string))
        {
            property.SetValue(target, value);
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

    private static JsonDocument ReadJsonFile(string path)
    {
        File.Exists(path).Should().BeTrue($"expected artifact file {path} to exist");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static void AssertArtifactLocatedUnderLogsCi(string artifactPath, string logsCiDirectory)
    {
        artifactPath.Should().NotBeNullOrWhiteSpace();
        var fullArtifactPath = Path.GetFullPath(artifactPath);
        var fullLogsCiDirectory = Path.GetFullPath(logsCiDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        fullArtifactPath.Should().StartWith(fullLogsCiDirectory, "config audit artifacts must be emitted under logs/ci");
        File.Exists(fullArtifactPath).Should().BeTrue("the emitted artifact path must point to a file");
    }

    private static void AssertStandardizedArtifact(JsonElement element, EvaluationResult result, bool requireReasons)
    {
        GetRequiredString(element, "terminalOutcome", "terminal_outcome", "outcome").Should().Be(result.TerminalOutcome);
        GetRequiredString(element, "decisionStage", "decision_stage", "stage").Should().NotBeNullOrWhiteSpace();
        GetRequiredStringArray(element, "policyTrace", "policy_trace", "decisionTrace", "decision_trace").Should().NotBeEmpty();

        var artifactReasons = GetRequiredStringArray(element, "reasonIdentifiers", "reason_ids", "reasons");
        if (requireReasons)
        {
            artifactReasons.Should().Contain(result.ReasonIdentifiers);
        }
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
            var value = property.Value.GetString() ?? string.Empty;
            return SplitReasonIdentifiers(value);
        }

        property.Value.ValueKind.Should().Be(JsonValueKind.Array);
        return property.Value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).Where(item => item.Length > 0).ToArray();
    }

    private static JsonElement? FindProperty(JsonElement element, params string[] names)
    {
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
        value.Should().NotBeNull($"evaluation result must expose one of: {string.Join(", ", names)}");
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static IReadOnlyList<string> GetRequiredObjectStringArray(object target, params string[] names)
    {
        var value = GetOptionalObjectValue(target, names);
        value.Should().NotBeNull($"evaluation result must expose one of: {string.Join(", ", names)}");

        if (value is string text)
        {
            return SplitReasonIdentifiers(text);
        }

        if (value is IEnumerable values)
        {
            return values.Cast<object?>()
                .Select(item => Convert.ToString(item, CultureInfo.InvariantCulture) ?? string.Empty)
                .Where(item => item.Length > 0)
                .ToArray();
        }

        return new[] { Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty };
    }

    private static IReadOnlyList<string> SplitReasonIdentifiers(string value)
    {
        return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToArray();
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

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record ConfigCase(string ScenarioId, string ExpectedOutcome, IReadOnlyList<string> ExpectedReasonIdentifiers, string ConfigJson);

    private sealed record EvaluationResult(
        string TerminalOutcome,
        IReadOnlyList<string> ReasonIdentifiers,
        string AuditRecordPath,
        string CiSummaryPath)
    {
        public static EvaluationResult From(object? rawResult)
        {
            rawResult.Should().NotBeNull("config validation must return a machine-readable evaluation result");
            return new EvaluationResult(
                GetRequiredObjectString(rawResult!, "TerminalOutcome", "Outcome", "PolicyOutcome"),
                GetRequiredObjectStringArray(rawResult!, "ReasonIdentifiers", "ReasonIds", "Reasons"),
                GetRequiredObjectString(rawResult!, "AuditRecordPath", "AuditPath"),
                GetRequiredObjectString(rawResult!, "CiSummaryPath", "SummaryArtifactPath", "SummaryPath"));
        }
    }

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string rootDirectory)
        {
            RootDirectory = rootDirectory;
            LogsCiDirectory = Path.Combine(rootDirectory, "logs", "ci");
            Directory.CreateDirectory(LogsCiDirectory);
        }

        public string RootDirectory { get; }

        public string LogsCiDirectory { get; }

        public static TempWorkspace Create()
        {
            var rootDirectory = Path.Combine(Path.GetTempPath(), "lastking-config-audit-" + Guid.NewGuid().ToString("N"));
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
