using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigPolicyRoutingTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // acceptance: ACC:T37.5
    [Fact]
    public void ShouldSkipSemanticPolicyRouting_WhenStructuralValidationFails()
    {
        using var workspace = TempWorkspace.Create();
        const string configJson = """
                                  {
                                    "profile": 42,
                                    "difficulty": "normal",
                                    "maxPlayers": -1,
                                    "fallbackProfile": "standard"
                                  }
                                  """;

        var result = EvaluateWithProductionPipeline(workspace.LogsCiDirectory, configJson, "structural-failure-skips-semantic-routing");

        result.StructuralPassed.Should().BeFalse();
        result.SemanticChecksExecuted.Should().BeFalse("semantic rule checks must only run after the structural phase passes");
        result.PolicyActions.Should().BeEmpty("semantic policy actions must not be emitted when the structural phase already failed");
        result.ReasonIdentifiers.Should().NotBeEmpty();
    }

    // acceptance: ACC:T37.10
    [Fact]
    public void ShouldExposeExplicitReasonIdentifiersInAssertionsAndAudit_WhenOutcomeIsRejectOrFallback()
    {
        using var workspace = TempWorkspace.Create();
        var cases = new[]
        {
            new ConfigCase(
                "reject-unknown-profile",
                """
                {
                  "profile": "unknown-profile",
                  "difficulty": "normal",
                  "maxPlayers": 4,
                  "fallbackProfile": null
                }
                """,
                "reject"),
            new ConfigCase(
                "fallback-empty-profile",
                """
                {
                  "profile": "",
                  "difficulty": "normal",
                  "maxPlayers": 4,
                  "fallbackProfile": "standard"
                }
                """,
                "fallback")
        };

        foreach (var configCase in cases)
        {
            var result = EvaluateWithProductionPipeline(workspace.LogsCiDirectory, configCase.ConfigJson, configCase.ScenarioId);
            using var auditRecord = ReadJsonFile(result.AuditRecordPath);
            var auditReasonIdentifiers = GetRequiredStringArray(auditRecord.RootElement, "reasonIdentifiers", "reason_ids", "reasons");

            result.TerminalOutcome.Should().Be(configCase.ExpectedOutcome);
            result.ReasonIdentifiers.Should().NotBeEmpty();
            result.ReasonIdentifiers.Should().OnlyContain(reasonKey => !string.IsNullOrWhiteSpace(reasonKey));
            auditReasonIdentifiers.Should().BeEquivalentTo(result.ReasonIdentifiers);
        }
    }

    // acceptance: ACC:T37.15
    [Fact]
    public void ShouldReturnExactlyOnePolicyActionWithReasonKey_WhenEachSemanticViolationIsRouted()
    {
        using var workspace = TempWorkspace.Create();
        const string configJson = """
                                  {
                                    "profile": "",
                                    "difficulty": "impossible",
                                    "maxPlayers": 0,
                                    "fallbackProfile": "standard"
                                  }
                                  """;

        var result = EvaluateWithProductionPipeline(workspace.LogsCiDirectory, configJson, "semantic-routing-one-action-per-violation");
        var policyReasonKeys = result.PolicyActions.Select(action => action.ReasonKey).ToArray();

        result.StructuralPassed.Should().BeTrue("the semantic routing contract can only be evaluated after the structural phase passes");
        result.SemanticChecksExecuted.Should().BeTrue();
        result.SemanticViolations.Should().NotBeEmpty();
        result.PolicyActions.Should().HaveCount(result.SemanticViolations.Count, "each semantic violation must map to exactly one explicit policy action");
        result.PolicyActions.Should().OnlyContain(action =>
            !string.IsNullOrWhiteSpace(action.ActionName) &&
            !string.IsNullOrWhiteSpace(action.ReasonKey));
        policyReasonKeys.Should().OnlyHaveUniqueItems("each semantic violation should map to exactly one machine-verifiable reason key");
        result.ReasonIdentifiers.Should().Contain(policyReasonKeys);
    }

    // acceptance: ACC:T37.19
    [Fact]
    public void ShouldRejectWithExplicitReasonIdentifier_WhenPolicyMappingIsMissingForSemanticViolation()
    {
        var result = RouteSemanticViolations(new[] { "CONFIG_REASON_NOT_MAPPED" });

        result.TerminalOutcome.Should().Be("reject");
        result.PolicyActions.Should().ContainSingle();
        result.PolicyActions.Single().ActionName.Should().Be("reject");
        result.PolicyActions.Single().ReasonKey.Should().Be("CONFIG_REASON_NOT_MAPPED");
        result.ReasonIdentifiers.Should().Contain("CONFIG_REASON_NOT_MAPPED");
    }

    private static PipelineEvaluationResult EvaluateWithProductionPipeline(string logsCiDirectory, string configJson, string scenarioId)
    {
        LoadCoreAssembly();
        var pipelineType = FindType(
            "Game.Core.Services.ConfigValidationPipeline",
            "Game.Core.Services.ConfigPolicyPipeline",
            "Game.Core.Configuration.ConfigValidationPipeline");
        pipelineType.Should().NotBeNull("Task 37 requires a production config validation pipeline with policy routing");

        var method = FindEvaluationMethod(pipelineType!);
        method.Should().NotBeNull("the pipeline must expose an evaluation method that accepts config input and a logs/ci directory");

        var instance = method!.IsStatic ? null : CreateInstance(pipelineType!, logsCiDirectory);
        var arguments = BuildEvaluationArguments(method.GetParameters(), logsCiDirectory, configJson, scenarioId);
        var rawResult = method.Invoke(instance, arguments);

        return PipelineEvaluationResult.FromObject(ResolveTaskResult(rawResult));
    }

    private static PolicyRoutingResult RouteSemanticViolations(IReadOnlyList<string> semanticReasonIdentifiers)
    {
        LoadCoreAssembly();
        var routerType = FindType(
            "Game.Core.Services.ConfigPolicyRouter",
            "Game.Core.Services.ConfigPolicyRoutingService",
            "Game.Core.Configuration.ConfigPolicyRouter");
        routerType.Should().NotBeNull("Task 37 requires a dedicated policy router that can route semantic violations");

        var method = FindRoutingMethod(routerType!);
        method.Should().NotBeNull("the policy router must expose a route method that accepts semantic reason identifiers");

        var instance = method!.IsStatic ? null : CreateInstance(routerType!, null);
        var arguments = BuildRoutingArguments(method.GetParameters(), semanticReasonIdentifiers);
        var rawResult = method.Invoke(instance, arguments);

        return PolicyRoutingResult.FromObject(ResolveTaskResult(rawResult));
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
            .FirstOrDefault(method => CanBuildEvaluationArguments(method.GetParameters()));
    }

    private static MethodInfo? FindRoutingMethod(Type routerType)
    {
        var methodNames = new[]
        {
            "Route",
            "RouteSemanticViolations",
            "ResolvePolicy",
            "Resolve",
            "EvaluateSemanticPolicy"
        };

        return routerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => methodNames.Contains(method.Name, StringComparer.Ordinal))
            .Where(method => method.ReturnType != typeof(void))
            .FirstOrDefault(method => CanBuildRoutingArguments(method.GetParameters()));
    }

    private static bool CanBuildEvaluationArguments(ParameterInfo[] parameters)
    {
        return parameters.All(parameter =>
            parameter.HasDefaultValue
            || parameter.ParameterType == typeof(string)
            || parameter.ParameterType == typeof(DirectoryInfo)
            || parameter.ParameterType == typeof(JsonDocument)
            || parameter.ParameterType == typeof(JsonElement)
            || parameter.ParameterType == typeof(CancellationToken)
            || (parameter.ParameterType.IsClass && parameter.ParameterType != typeof(string)));
    }

    private static bool CanBuildRoutingArguments(ParameterInfo[] parameters)
    {
        return parameters.All(parameter =>
            parameter.HasDefaultValue
            || parameter.ParameterType == typeof(string)
            || parameter.ParameterType == typeof(string[])
            || parameter.ParameterType == typeof(List<string>)
            || parameter.ParameterType == typeof(IReadOnlyList<string>)
            || parameter.ParameterType == typeof(IEnumerable<string>)
            || parameter.ParameterType == typeof(CancellationToken)
            || (parameter.ParameterType.IsClass && parameter.ParameterType != typeof(string)));
    }

    private static object CreateInstance(Type type, string? logsCiDirectory)
    {
        var stringConstructor = type.GetConstructor(new[] { typeof(string) });
        if (stringConstructor is not null)
        {
            return stringConstructor.Invoke(new object?[] { logsCiDirectory ?? Path.GetTempPath() });
        }

        var directoryConstructor = type.GetConstructor(new[] { typeof(DirectoryInfo) });
        if (directoryConstructor is not null)
        {
            return directoryConstructor.Invoke(new object?[] { new DirectoryInfo(logsCiDirectory ?? Path.GetTempPath()) });
        }

        var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
        defaultConstructor.Should().NotBeNull($"type {type.FullName} must be constructible for Task 37 tests");
        return defaultConstructor!.Invoke(Array.Empty<object>());
    }

    private static object?[] BuildEvaluationArguments(ParameterInfo[] parameters, string logsCiDirectory, string configJson, string scenarioId)
    {
        return parameters.Select(parameter => BuildEvaluationArgument(parameter, logsCiDirectory, configJson, scenarioId)).ToArray();
    }

    private static object? BuildEvaluationArgument(ParameterInfo parameter, string logsCiDirectory, string configJson, string scenarioId)
    {
        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

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

        if (parameterType == typeof(CancellationToken))
        {
            return CancellationToken.None;
        }

        var request = TryDeserialize(configJson, parameterType) ?? Activator.CreateInstance(parameterType);
        request.Should().NotBeNull($"request type {parameterType.FullName} must be constructible from config JSON");

        SetWritableStringProperty(request!, logsCiDirectory, "LogsCiDirectory", "AuditRootDirectory", "OutputDirectory");
        SetWritableStringProperty(request!, scenarioId, "ScenarioId", "CaseId", "CorrelationId");
        SetWritableStringProperty(request!, configJson, "ConfigJson", "Json", "Payload");
        SetWritableDirectoryProperty(request!, logsCiDirectory, "LogsCiDirectory", "AuditRootDirectory", "OutputDirectory");
        SetWritableJsonProperty(request!, configJson, "Config", "Payload", "Document");

        return request;
    }

    private static object?[] BuildRoutingArguments(ParameterInfo[] parameters, IReadOnlyList<string> semanticReasonIdentifiers)
    {
        return parameters.Select(parameter => BuildRoutingArgument(parameter, semanticReasonIdentifiers)).ToArray();
    }

    private static object? BuildRoutingArgument(ParameterInfo parameter, IReadOnlyList<string> semanticReasonIdentifiers)
    {
        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        var parameterType = parameter.ParameterType;

        if (parameterType == typeof(string))
        {
            return string.Join(",", semanticReasonIdentifiers);
        }

        if (parameterType == typeof(string[]))
        {
            return semanticReasonIdentifiers.ToArray();
        }

        if (parameterType == typeof(List<string>))
        {
            return semanticReasonIdentifiers.ToList();
        }

        if (parameterType == typeof(IReadOnlyList<string>) || parameterType == typeof(IEnumerable<string>))
        {
            return semanticReasonIdentifiers.ToArray();
        }

        if (parameterType == typeof(CancellationToken))
        {
            return CancellationToken.None;
        }

        var payloadJson = JsonSerializer.Serialize(new
        {
            semanticReasonIdentifiers,
            reasonIdentifiers = semanticReasonIdentifiers,
            semanticViolations = semanticReasonIdentifiers,
            violations = semanticReasonIdentifiers
        });
        var request = TryDeserialize(payloadJson, parameterType) ?? Activator.CreateInstance(parameterType);
        request.Should().NotBeNull($"routing request type {parameterType.FullName} must be constructible");

        SetWritableStringArrayProperty(request!, semanticReasonIdentifiers, "SemanticReasonIdentifiers", "ReasonIdentifiers", "SemanticViolations", "Violations", "Reasons");
        SetWritableStringProperty(request!, string.Join(",", semanticReasonIdentifiers), "Reason", "ReasonKey", "ReasonIdentifier");

        return request;
    }

    private static object? TryDeserialize(string json, Type targetType)
    {
        try
        {
            return JsonSerializer.Deserialize(json, targetType, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsAny(string source, params string[] fragments)
    {
        return fragments.Any(fragment => source.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetWritableStringProperty(object instance, string value, params string[] propertyNames)
    {
        var property = FindWritableProperty(instance.GetType(), propertyNames);
        if (property is not null && property.PropertyType == typeof(string))
        {
            property.SetValue(instance, value);
        }
    }

    private static void SetWritableDirectoryProperty(object instance, string path, params string[] propertyNames)
    {
        var property = FindWritableProperty(instance.GetType(), propertyNames);
        if (property is not null && property.PropertyType == typeof(DirectoryInfo))
        {
            property.SetValue(instance, new DirectoryInfo(path));
        }
    }

    private static void SetWritableJsonProperty(object instance, string json, params string[] propertyNames)
    {
        var property = FindWritableProperty(instance.GetType(), propertyNames);
        if (property is null)
        {
            return;
        }

        if (property.PropertyType == typeof(JsonElement))
        {
            property.SetValue(instance, JsonDocument.Parse(json).RootElement.Clone());
        }
        else if (property.PropertyType == typeof(JsonDocument))
        {
            property.SetValue(instance, JsonDocument.Parse(json));
        }
    }

    private static void SetWritableStringArrayProperty(object instance, IReadOnlyList<string> values, params string[] propertyNames)
    {
        var property = FindWritableProperty(instance.GetType(), propertyNames);
        if (property is null)
        {
            return;
        }

        if (property.PropertyType == typeof(string[]))
        {
            property.SetValue(instance, values.ToArray());
            return;
        }

        if (property.PropertyType == typeof(List<string>))
        {
            property.SetValue(instance, values.ToList());
            return;
        }

        if (property.PropertyType.IsAssignableFrom(typeof(string[])))
        {
            property.SetValue(instance, values.ToArray());
        }
    }

    private static PropertyInfo? FindWritableProperty(Type type, params string[] propertyNames)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property =>
                property.CanWrite &&
                propertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase));
    }

    private static object ResolveTaskResult(object? rawResult)
    {
        rawResult.Should().NotBeNull("the production pipeline must return a result object");

        if (rawResult is Task task)
        {
            task.GetAwaiter().GetResult();
            rawResult = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(task);
        }

        rawResult.Should().NotBeNull("the awaited production result must not be null");
        return rawResult!;
    }

    private static JsonDocument ReadJsonFile(string path)
    {
        File.Exists(path).Should().BeTrue($"audit artifact {path} must be created");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string GetRequiredObjectString(object instance, params string[] memberNames)
    {
        var value = GetRequiredMemberValue(instance, memberNames);
        var text = value switch
        {
            string stringValue => stringValue,
            Enum enumValue => enumValue.ToString(),
            _ => value.ToString() ?? string.Empty
        };

        text.Should().NotBeNullOrWhiteSpace($"one of [{string.Join(", ", memberNames)}] must contain a non-empty string value");
        return text;
    }

    private static bool GetRequiredObjectBoolean(object instance, params string[] memberNames)
    {
        var value = GetRequiredMemberValue(instance, memberNames);
        if (value is bool booleanValue)
        {
            return booleanValue;
        }

        bool.TryParse(value.ToString(), out var parsed).Should().BeTrue($"one of [{string.Join(", ", memberNames)}] must contain a boolean value");
        return parsed;
    }

    private static IReadOnlyList<string> GetRequiredObjectStringArray(object instance, params string[] memberNames)
    {
        var value = GetRequiredMemberValue(instance, memberNames);

        if (value is string stringValue)
        {
            return SplitReasonIdentifiers(stringValue);
        }

        if (value is IEnumerable<string> stringEnumerable)
        {
            return stringEnumerable.Where(entry => !string.IsNullOrWhiteSpace(entry)).ToArray();
        }

        if (value is IEnumerable enumerable and not string)
        {
            return enumerable.Cast<object?>()
                .Select(entry => entry?.ToString())
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Cast<string>()
                .ToArray();
        }

        throw new InvalidOperationException($"Member [{string.Join(", ", memberNames)}] is not a string array");
    }

    private static IReadOnlyList<PolicyActionRecord> GetRequiredPolicyActions(object instance, params string[] memberNames)
    {
        var value = GetRequiredMemberValue(instance, memberNames);
        var items = value is IEnumerable enumerable and not string
            ? enumerable.Cast<object?>().Where(item => item is not null).Cast<object>().ToArray()
            : new[] { value };

        return items.Select(item => new PolicyActionRecord(
                NormalizeOutcome(GetRequiredObjectString(item, "Action", "ActionName", "PolicyAction", "Outcome")),
                GetRequiredObjectString(item, "ReasonKey", "ReasonIdentifier", "ReasonId", "Code")))
            .ToArray();
    }

    private static object GetRequiredMemberValue(object instance, params string[] memberNames)
    {
        foreach (var memberName in memberNames)
        {
            if (TryGetMemberValue(instance, memberName, out var value) && value is not null)
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Missing required member. Tried: {string.Join(", ", memberNames)}");
    }

    private static bool TryGetMemberValue(object instance, string memberName, out object? value)
    {
        var type = instance.GetType();
        var property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is not null)
        {
            value = property.GetValue(instance);
            return true;
        }

        var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (field is not null)
        {
            value = field.GetValue(instance);
            return true;
        }

        value = null;
        return false;
    }

    private static IReadOnlyList<string> GetRequiredStringArray(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Array)
            {
                return property.EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Cast<string>()
                    .ToArray();
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                return SplitReasonIdentifiers(property.GetString() ?? string.Empty);
            }
        }

        throw new InvalidOperationException($"Missing required JSON property. Tried: {string.Join(", ", propertyNames)}");
    }

    private static string NormalizeOutcome(string outcome)
    {
        return outcome.Trim().ToLowerInvariant();
    }

    private static IReadOnlyList<string> SplitReasonIdentifiers(string value)
    {
        return value.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private sealed record ConfigCase(string ScenarioId, string ConfigJson, string ExpectedOutcome);

    private sealed record PolicyActionRecord(string ActionName, string ReasonKey);

    private sealed record PipelineEvaluationResult(
        string TerminalOutcome,
        bool StructuralPassed,
        bool SemanticChecksExecuted,
        IReadOnlyList<string> SemanticViolations,
        IReadOnlyList<PolicyActionRecord> PolicyActions,
        IReadOnlyList<string> ReasonIdentifiers,
        string AuditRecordPath)
    {
        public static PipelineEvaluationResult FromObject(object rawResult)
        {
            var auditRecordPath = GetRequiredObjectString(rawResult, "AuditRecordPath", "AuditPath");
            return new PipelineEvaluationResult(
                NormalizeOutcome(GetRequiredObjectString(rawResult, "TerminalOutcome", "Outcome", "PolicyOutcome")),
                GetRequiredObjectBoolean(rawResult, "StructuralPassed", "SchemaPassed", "StructurePassed"),
                GetRequiredObjectBoolean(rawResult, "SemanticChecksExecuted", "SemanticEvaluated", "SemanticRulesExecuted"),
                GetRequiredObjectStringArray(rawResult, "SemanticViolationReasonKeys", "SemanticReasonIdentifiers", "SemanticFailures", "SemanticReasons"),
                GetRequiredPolicyActions(rawResult, "PolicyActions", "RoutingDecisions", "PolicyDecisions", "Actions"),
                GetRequiredObjectStringArray(rawResult, "ReasonIdentifiers", "ReasonIds", "Reasons"),
                auditRecordPath);
        }
    }

    private sealed record PolicyRoutingResult(
        string TerminalOutcome,
        IReadOnlyList<PolicyActionRecord> PolicyActions,
        IReadOnlyList<string> ReasonIdentifiers)
    {
        public static PolicyRoutingResult FromObject(object rawResult)
        {
            return new PolicyRoutingResult(
                NormalizeOutcome(GetRequiredObjectString(rawResult, "TerminalOutcome", "Outcome", "PolicyOutcome")),
                GetRequiredPolicyActions(rawResult, "PolicyActions", "RoutingDecisions", "PolicyDecisions", "Actions"),
                GetRequiredObjectStringArray(rawResult, "ReasonIdentifiers", "ReasonIds", "Reasons"));
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
            var rootDirectory = Path.Combine(Path.GetTempPath(), "lastking-task37-" + Guid.NewGuid().ToString("N"));
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
