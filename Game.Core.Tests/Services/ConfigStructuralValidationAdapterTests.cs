using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ConfigStructuralValidationAdapterTests
{
    // ACC:T37.4
    [Fact]
    public void ShouldReturnNormalizedStructuralErrors_WhenPayloadTypesAndRequiredFieldsAreInvalid()
    {
        const string invalidPayload = """
                                      {
                                        "profile": 42,
                                        "difficulty": "normal",
                                        "fallbackProfile": null
                                      }
                                      """;

        var result = ValidateWithStructuralAdapter("runtime-config", invalidPayload, "structural-invalid-runtime");
        var orderedKeys = result.Errors.Select(error => error.Code + "|" + error.Location).ToArray();

        result.StructuralPassed.Should().BeFalse();
        result.SemanticValidationStarted.Should().BeFalse("semantic validation must not run after structural rejection");
        result.Errors.Should().NotBeEmpty();
        orderedKeys.Should().Equal(orderedKeys.OrderBy(key => key, StringComparer.Ordinal), "normalized structural errors must have deterministic ordering");
        result.Errors.Should().Contain(error => error.Code == "CONFIG_STRUCTURE_TYPE_MISMATCH" && error.Location == "$.profile");
        result.Errors.Should().Contain(error => error.Code == "CONFIG_STRUCTURE_REQUIRED_FIELD_MISSING" && error.Location == "$.maxPlayers");
        result.Errors.Should().OnlyContain(error =>
            error.Code.StartsWith("CONFIG_STRUCTURE_", StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(error.Location) &&
            !string.IsNullOrWhiteSpace(error.Message));
    }

    // ACC:T37.18
    [Fact]
    public void ShouldProceedToSemanticValidation_WhenStructuralInputIsValid()
    {
        const string validPayload = """
                                    {
                                      "profile": "unknown-profile",
                                      "difficulty": "normal",
                                      "maxPlayers": 4,
                                      "fallbackProfile": null
                                    }
                                    """;

        var result = ValidateWithStructuralAdapter("runtime-config", validPayload, "structural-valid-semantic-next");

        result.StructuralPassed.Should().BeTrue();
        result.Errors.Should().BeEmpty("structurally valid payloads should not emit structural error payloads");
        result.SemanticValidationStarted.Should().BeTrue("structurally valid inputs must proceed to semantic validation");
    }

    // ACC:T37.14
    [Fact]
    public void ShouldCollectNormalizedErrorsThroughCentralizedAdapter_WhenMultipleSchemasAreValidated()
    {
        var cases = new[]
        {
            new StructuralValidationCase(
                "runtime-config",
                """
                {
                  "profile": 42,
                  "difficulty": "normal",
                  "fallbackProfile": null
                }
                """),
            new StructuralValidationCase(
                "difficulty-config",
                """
                {
                  "schema_version": "2",
                  "profiles": "invalid"
                }
                """)
        };

        var results = cases
            .Select(caseDefinition => ValidateWithStructuralAdapter(caseDefinition.SchemaId, caseDefinition.PayloadJson, "centralized-" + caseDefinition.SchemaId))
            .ToArray();
        var allErrors = results.SelectMany(result => result.Errors).ToArray();
        var adapterIds = results.Select(result => result.AdapterId).Distinct(StringComparer.Ordinal).ToArray();

        results.Should().OnlyContain(result => !result.StructuralPassed);
        adapterIds.Should().ContainSingle().Which.Should().Be("centralized-structural-validation");
        allErrors.Should().NotBeEmpty();
        allErrors.Should().OnlyContain(error => error.Code.StartsWith("CONFIG_STRUCTURE_", StringComparison.Ordinal));
        allErrors.Should().OnlyContain(error => !string.IsNullOrWhiteSpace(error.Location));
        allErrors.Select(error => error.SchemaId)
            .Where(schemaId => !string.IsNullOrWhiteSpace(schemaId))
            .Distinct(StringComparer.Ordinal)
            .Should()
            .BeEquivalentTo(cases.Select(caseDefinition => caseDefinition.SchemaId));
        allErrors.Should().Contain(error => error.SchemaId == "difficulty-config" && error.Location == "$.schema_version");
        allErrors.Should().Contain(error => error.SchemaId == "difficulty-config" && error.Location == "$.profiles");
    }

    private static StructuralValidationResult ValidateWithStructuralAdapter(string schemaId, string payloadJson, string scenarioId)
    {
        LoadCoreAssembly();
        var adapterType = FindType(
            "Game.Core.Services.ConfigStructuralValidationAdapter",
            "Game.Core.Services.StructuralConfigValidationAdapter",
            "Game.Core.Services.ConfigSchemaValidationAdapter",
            "Game.Core.Configuration.ConfigStructuralValidationAdapter");
        adapterType.Should().NotBeNull("Task 37 requires a centralized structural validation adapter for schema-level config checks");

        var method = FindValidationMethod(adapterType!);
        method.Should().NotBeNull("the structural validation adapter must expose a public validation method");

        var instance = method!.IsStatic ? null : CreateInstance(adapterType!);
        var arguments = BuildArguments(method.GetParameters(), schemaId, payloadJson, scenarioId);
        var rawResult = method.Invoke(instance, arguments);

        return StructuralValidationResult.FromObject(ResolveTaskResult(rawResult));
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

    private static MethodInfo? FindValidationMethod(Type adapterType)
    {
        var methodNames = new[]
        {
            "Validate",
            "ValidateConfig",
            "ValidatePayload",
            "ValidateStructure",
            "ValidateStructural",
            "ValidateSchema"
        };

        return adapterType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => methodNames.Contains(method.Name, StringComparer.Ordinal))
            .Where(method => method.ReturnType != typeof(void))
            .FirstOrDefault(method => CanBuildArguments(method.GetParameters()));
    }

    private static bool CanBuildArguments(ParameterInfo[] parameters)
    {
        return parameters.All(parameter =>
            parameter.HasDefaultValue ||
            parameter.ParameterType == typeof(string) ||
            parameter.ParameterType == typeof(JsonDocument) ||
            parameter.ParameterType == typeof(JsonElement) ||
            parameter.ParameterType == typeof(CancellationToken) ||
            (parameter.ParameterType.IsClass && parameter.ParameterType != typeof(string)));
    }

    private static object CreateInstance(Type adapterType)
    {
        var defaultConstructor = adapterType.GetConstructor(Type.EmptyTypes);
        defaultConstructor.Should().NotBeNull("the structural validation adapter must be constructible without external infrastructure");
        return defaultConstructor!.Invoke(Array.Empty<object>());
    }

    private static object?[] BuildArguments(ParameterInfo[] parameters, string schemaId, string payloadJson, string scenarioId)
    {
        return parameters.Select(parameter => BuildArgument(parameter, schemaId, payloadJson, scenarioId)).ToArray();
    }

    private static object? BuildArgument(ParameterInfo parameter, string schemaId, string payloadJson, string scenarioId)
    {
        if (parameter.HasDefaultValue)
        {
            return parameter.DefaultValue;
        }

        var parameterName = parameter.Name ?? string.Empty;
        var parameterType = parameter.ParameterType;

        if (parameterType == typeof(string))
        {
            if (ContainsAny(parameterName, "json", "payload", "document", "content", "text"))
            {
                return payloadJson;
            }

            if (ContainsAny(parameterName, "scenario", "case", "correlation"))
            {
                return scenarioId;
            }

            if (ContainsAny(parameterName, "schema", "kind", "type", "name", "config"))
            {
                return schemaId;
            }

            return payloadJson;
        }

        if (parameterType == typeof(JsonDocument))
        {
            return JsonDocument.Parse(payloadJson);
        }

        if (parameterType == typeof(JsonElement))
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.Clone();
        }

        if (parameterType == typeof(CancellationToken))
        {
            return CancellationToken.None;
        }

        var request = TryDeserialize(payloadJson, parameterType) ?? Activator.CreateInstance(parameterType);
        request.Should().NotBeNull($"request type {parameterType.FullName} must be constructible for structural validation");

        SetWritableStringProperty(request!, schemaId, "SchemaId", "SchemaName", "ConfigKind", "ConfigType", "Kind");
        SetWritableStringProperty(request!, payloadJson, "PayloadJson", "ConfigJson", "Json", "Payload", "Content");
        SetWritableStringProperty(request!, scenarioId, "ScenarioId", "CaseId", "CorrelationId");
        SetWritableJsonProperty(request!, payloadJson, "Payload", "Config", "Document", "Root");

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

    private static void SetWritableJsonProperty(object instance, string json, params string[] propertyNames)
    {
        var property = FindWritableProperty(instance.GetType(), propertyNames);
        if (property is null)
        {
            return;
        }

        if (property.PropertyType == typeof(JsonDocument))
        {
            property.SetValue(instance, JsonDocument.Parse(json));
            return;
        }

        if (property.PropertyType == typeof(JsonElement))
        {
            using var document = JsonDocument.Parse(json);
            property.SetValue(instance, document.RootElement.Clone());
        }
    }

    private static PropertyInfo? FindWritableProperty(Type type, params string[] propertyNames)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property => property.CanWrite && propertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase));
    }

    private static object ResolveTaskResult(object? rawResult)
    {
        rawResult.Should().NotBeNull("the structural validation adapter must return a result object");

        if (rawResult is Task task)
        {
            task.GetAwaiter().GetResult();
            rawResult = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(task);
        }

        rawResult.Should().NotBeNull("the awaited structural validation result must not be null");
        return rawResult!;
    }

    private static bool GetRequiredBoolean(object instance, params string[] memberNames)
    {
        var value = GetRequiredMemberValue(instance, memberNames);
        if (value is bool booleanValue)
        {
            return booleanValue;
        }

        bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed)
            .Should().BeTrue($"one of [{string.Join(", ", memberNames)}] must contain a boolean value");
        return parsed;
    }

    private static bool GetOptionalBoolean(object instance, bool defaultValue, params string[] memberNames)
    {
        return TryGetMemberValue(instance, memberNames, out var value) && value is not null
            ? Convert.ToBoolean(value, CultureInfo.InvariantCulture)
            : defaultValue;
    }

    private static string GetOptionalString(object instance, string defaultValue, params string[] memberNames)
    {
        return TryGetMemberValue(instance, memberNames, out var value) && value is not null
            ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? defaultValue
            : defaultValue;
    }

    private static IReadOnlyList<NormalizedStructuralError> GetRequiredErrors(object instance, params string[] memberNames)
    {
        var value = GetRequiredMemberValue(instance, memberNames);
        if (value is JsonElement jsonElement)
        {
            jsonElement.ValueKind.Should().Be(JsonValueKind.Array);
            return jsonElement.EnumerateArray().Select(NormalizedStructuralError.FromJson).ToArray();
        }

        if (value is IEnumerable enumerable and not string)
        {
            return enumerable.Cast<object?>()
                .Where(item => item is not null)
                .Select(item => NormalizedStructuralError.FromObject(item!))
                .ToArray();
        }

        throw new InvalidOperationException($"Member [{string.Join(", ", memberNames)}] is not an error collection.");
    }

    private static object GetRequiredMemberValue(object instance, params string[] memberNames)
    {
        if (TryGetMemberValue(instance, memberNames, out var value) && value is not null)
        {
            return value;
        }

        throw new InvalidOperationException($"Missing required member. Tried: {string.Join(", ", memberNames)}");
    }

    private static bool TryGetMemberValue(object instance, IReadOnlyList<string> memberNames, out object? value)
    {
        if (instance is IDictionary<string, object?> dictionary)
        {
            var key = dictionary.Keys.FirstOrDefault(candidate => memberNames.Contains(candidate, StringComparer.OrdinalIgnoreCase));
            value = key is null ? null : dictionary[key];
            return key is not null;
        }

        var type = instance.GetType();
        foreach (var memberName in memberNames)
        {
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
        }

        value = null;
        return false;
    }

    private static string GetRequiredJsonString(JsonElement element, params string[] propertyNames)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (propertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                property.Value.ValueKind.Should().Be(JsonValueKind.String);
                return property.Value.GetString() ?? string.Empty;
            }
        }

        throw new InvalidOperationException($"Missing required JSON property. Tried: {string.Join(", ", propertyNames)}");
    }

    private static string GetOptionalJsonString(JsonElement element, string defaultValue, params string[] propertyNames)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (propertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? defaultValue
                    : property.Value.ToString();
            }
        }

        return defaultValue;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record StructuralValidationCase(string SchemaId, string PayloadJson);

    private sealed record StructuralValidationResult(
        bool StructuralPassed,
        bool SemanticValidationStarted,
        string AdapterId,
        IReadOnlyList<NormalizedStructuralError> Errors)
    {
        public static StructuralValidationResult FromObject(object rawResult)
        {
            return new StructuralValidationResult(
                GetRequiredBoolean(rawResult, "StructuralPassed", "StructurePassed", "SchemaPassed", "IsValid", "Valid", "Accepted"),
                GetOptionalBoolean(rawResult, defaultValue: false, "SemanticValidationStarted", "SemanticChecksExecuted", "SemanticEvaluated", "SemanticRulesExecuted"),
                GetOptionalString(rawResult, string.Empty, "AdapterId", "AdapterName", "ValidatorId", "ValidatorName"),
                GetRequiredErrors(rawResult, "Errors", "StructuralErrors", "ValidationErrors", "ErrorPayloads", "NormalizedErrors"));
        }
    }

    private sealed record NormalizedStructuralError(string Code, string Location, string Message, string SchemaId)
    {
        public static NormalizedStructuralError FromObject(object rawError)
        {
            if (rawError is JsonElement jsonElement)
            {
                return FromJson(jsonElement);
            }

            return new NormalizedStructuralError(
                GetOptionalString(rawError, string.Empty, "Code", "ErrorCode", "ReasonCode", "ReasonIdentifier"),
                GetOptionalString(rawError, string.Empty, "Location", "JsonPath", "Path", "Pointer"),
                GetOptionalString(rawError, string.Empty, "Message", "Detail", "Description"),
                GetOptionalString(rawError, string.Empty, "SchemaId", "SchemaName", "ConfigKind", "ConfigType"));
        }

        public static NormalizedStructuralError FromJson(JsonElement jsonElement)
        {
            return new NormalizedStructuralError(
                GetRequiredJsonString(jsonElement, "code", "errorCode", "reasonCode", "reasonIdentifier"),
                GetRequiredJsonString(jsonElement, "location", "jsonPath", "path", "pointer"),
                GetOptionalJsonString(jsonElement, string.Empty, "message", "detail", "description"),
                GetOptionalJsonString(jsonElement, string.Empty, "schemaId", "schemaName", "configKind", "configType"));
        }
    }
}
