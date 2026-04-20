using System.Text.Json;

namespace Game.Core.Services;

public sealed class ConfigStructuralValidationAdapter
{
    public ConfigStructuralValidationResult Validate(string schemaId, string payloadJson, string scenarioId = "")
    {
        var errors = new List<NormalizedStructuralError>();
        var normalizedSchemaId = string.IsNullOrWhiteSpace(schemaId) ? "runtime-config" : schemaId.Trim();
        var schemaRules = ResolveSchemaRules(normalizedSchemaId);
        if (schemaRules.Count == 0)
        {
            errors.Add(new NormalizedStructuralError(
                Code: "CONFIG_STRUCTURE_SCHEMA_UNKNOWN",
                Location: "$",
                Message: "unknown schema id",
                SchemaId: normalizedSchemaId));
            return new ConfigStructuralValidationResult(
                StructuralPassed: false,
                SemanticValidationStarted: false,
                AdapterId: "centralized-structural-validation",
                Errors: errors);
        }

        JsonElement root;
        try
        {
            root = ConfigJson.Parse(payloadJson);
        }
        catch (JsonException exception)
        {
            errors.Add(new NormalizedStructuralError(
                Code: "CONFIG_STRUCTURE_JSON_INVALID",
                Location: "$",
                Message: exception.Message,
                SchemaId: normalizedSchemaId));
            return new ConfigStructuralValidationResult(
                StructuralPassed: false,
                SemanticValidationStarted: false,
                AdapterId: "centralized-structural-validation",
                Errors: errors);
        }

        foreach (var rule in schemaRules)
        {
            rule(root, errors, normalizedSchemaId);
        }

        errors = errors
            .OrderBy(error => error.Code, StringComparer.Ordinal)
            .ThenBy(error => error.Location, StringComparer.Ordinal)
            .ToList();

        return new ConfigStructuralValidationResult(
            StructuralPassed: errors.Count == 0,
            SemanticValidationStarted: errors.Count == 0,
            AdapterId: "centralized-structural-validation",
            Errors: errors);
    }

    private static IReadOnlyList<Action<JsonElement, List<NormalizedStructuralError>, string>> ResolveSchemaRules(string schemaId)
    {
        if (string.Equals(schemaId, "runtime-config", StringComparison.Ordinal))
        {
            return new Action<JsonElement, List<NormalizedStructuralError>, string>[]
            {
                (root, errors, id) => ValidateRequiredString(root, errors, id, "profile"),
                (root, errors, id) => ValidateRequiredString(root, errors, id, "difficulty"),
                (root, errors, id) => ValidateRequiredInt(root, errors, id, "maxPlayers"),
            };
        }

        if (string.Equals(schemaId, "difficulty-config", StringComparison.Ordinal))
        {
            return new Action<JsonElement, List<NormalizedStructuralError>, string>[]
            {
                (root, errors, id) => ValidateRequiredInt(root, errors, id, "schema_version"),
                (root, errors, id) => ValidateRequiredArray(root, errors, id, "profiles"),
            };
        }

        return Array.Empty<Action<JsonElement, List<NormalizedStructuralError>, string>>();
    }

    private static void ValidateRequiredString(JsonElement root, List<NormalizedStructuralError> errors, string schemaId, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            errors.Add(new NormalizedStructuralError(
                Code: "CONFIG_STRUCTURE_REQUIRED_FIELD_MISSING",
                Location: "$." + propertyName,
                Message: propertyName + " is required",
                SchemaId: schemaId));
            return;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            errors.Add(new NormalizedStructuralError(
                Code: "CONFIG_STRUCTURE_TYPE_MISMATCH",
                Location: "$." + propertyName,
                Message: propertyName + " must be a string",
                SchemaId: schemaId));
        }
    }

    private static void ValidateRequiredInt(JsonElement root, List<NormalizedStructuralError> errors, string schemaId, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            errors.Add(new NormalizedStructuralError(
                Code: "CONFIG_STRUCTURE_REQUIRED_FIELD_MISSING",
                Location: "$." + propertyName,
                Message: propertyName + " is required",
                SchemaId: schemaId));
            return;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out _))
        {
            errors.Add(new NormalizedStructuralError(
                Code: "CONFIG_STRUCTURE_TYPE_MISMATCH",
                Location: "$." + propertyName,
                Message: propertyName + " must be an integer",
                SchemaId: schemaId));
        }
    }

    private static void ValidateRequiredArray(JsonElement root, List<NormalizedStructuralError> errors, string schemaId, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            errors.Add(new NormalizedStructuralError(
                Code: "CONFIG_STRUCTURE_REQUIRED_FIELD_MISSING",
                Location: "$." + propertyName,
                Message: propertyName + " is required",
                SchemaId: schemaId));
            return;
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            errors.Add(new NormalizedStructuralError(
                Code: "CONFIG_STRUCTURE_TYPE_MISMATCH",
                Location: "$." + propertyName,
                Message: propertyName + " must be an array",
                SchemaId: schemaId));
        }
    }
}
