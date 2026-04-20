using System.Text.Json;

namespace Game.Core.Services;

public static class PressureNormalizationConfigContractValidator
{
    public const string RangeContractKeyword = "x-range-check";

    public static bool TryValidate(JsonElement root, out string reason)
    {
        reason = string.Empty;
        if (root.ValueKind != JsonValueKind.Object)
        {
            reason = "payload must be an object";
            return false;
        }

        if (!TryGetRequiredNumber(root, "baseline", out var baseline) ||
            !TryGetRequiredNumber(root, "min_pressure", out var minPressure) ||
            !TryGetRequiredNumber(root, "max_pressure", out var maxPressure))
        {
            reason = "baseline, min_pressure, and max_pressure are required numbers";
            return false;
        }

        if (baseline < 0d)
        {
            reason = "baseline must be greater than or equal to zero";
            return false;
        }

        if (!root.TryGetProperty("normalization_factors", out var normalizationFactors) ||
            normalizationFactors.ValueKind != JsonValueKind.Array ||
            normalizationFactors.GetArrayLength() == 0)
        {
            reason = "normalization_factors must be a non-empty array";
            return false;
        }

        foreach (var item in normalizationFactors.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetDouble(out _))
            {
                reason = "normalization_factors items must be numeric";
                return false;
            }
        }

        if (root.TryGetProperty("constraints", out var constraints))
        {
            if (constraints.ValueKind != JsonValueKind.Object)
            {
                reason = "constraints must be an object when present";
                return false;
            }

            foreach (var property in constraints.EnumerateObject())
            {
                if (!string.Equals(property.Name, "range_check", StringComparison.Ordinal))
                {
                    reason = $"unsupported constraints key '{property.Name}'";
                    return false;
                }
            }

            if (constraints.TryGetProperty("range_check", out var rangeCheckValue) &&
                rangeCheckValue.ValueKind != JsonValueKind.True &&
                rangeCheckValue.ValueKind != JsonValueKind.False)
            {
                reason = "constraints.range_check must be boolean";
                return false;
            }
        }

        if (minPressure >= maxPressure)
        {
            reason = "min_pressure must be less than max_pressure";
            return false;
        }

        return true;
    }

    private static bool TryGetRequiredNumber(JsonElement root, string propertyName, out double value)
    {
        value = 0d;
        if (!root.TryGetProperty(propertyName, out var node))
        {
            return false;
        }

        return node.ValueKind == JsonValueKind.Number && node.TryGetDouble(out value);
    }
}
