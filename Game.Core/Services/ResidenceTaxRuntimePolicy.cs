using System.Collections.Generic;
using System.Text.Json;

namespace Game.Core.Services;

public static class ResidenceTaxRuntimePolicy
{
    public const int ResidenceTaxCadenceSeconds = 15;
    public const string NegativeGoldPolicyAllowDebt = "allow_debt";
    public const string NegativeGoldPolicyRefuseNegative = "refuse_negative";

    public static ResidenceTaxSettings ReadSettings(
        JsonElement root,
        ISet<string> reasons,
        string invalidTypeReason,
        string outOfRangeReason)
    {
        var taxTickSeconds = ResidenceTaxCadenceSeconds;
        var taxPerTick = 0;
        var taxPerTickByLevel = new Dictionary<int, int>();
        var negativeGoldPolicy = NegativeGoldPolicyAllowDebt;

        if (!TryGetPath(root, out var residenceElement, "economy", "residence") || residenceElement.ValueKind != JsonValueKind.Object)
        {
            return new ResidenceTaxSettings(taxTickSeconds, taxPerTick, taxPerTickByLevel, negativeGoldPolicy);
        }

        if (TryGetPropertyIgnoreCase(residenceElement, "tax_tick_seconds", out var tickElement))
        {
            if (tickElement.ValueKind != JsonValueKind.Number || !tickElement.TryGetInt32(out taxTickSeconds))
            {
                reasons.Add(invalidTypeReason);
                taxTickSeconds = ResidenceTaxCadenceSeconds;
            }
            else if (taxTickSeconds != ResidenceTaxCadenceSeconds)
            {
                reasons.Add(outOfRangeReason);
            }
        }

        if (TryGetPropertyIgnoreCase(residenceElement, "tax_per_tick", out var taxElement))
        {
            if (taxElement.ValueKind != JsonValueKind.Number || !taxElement.TryGetInt32(out taxPerTick))
            {
                reasons.Add(invalidTypeReason);
                taxPerTick = 0;
            }
            else if (taxPerTick < 0)
            {
                reasons.Add(outOfRangeReason);
                taxPerTick = 0;
            }
        }

        if (TryGetPropertyIgnoreCase(residenceElement, "tax_per_tick_by_level", out var byLevelElement))
        {
            if (byLevelElement.ValueKind != JsonValueKind.Object)
            {
                reasons.Add(invalidTypeReason);
            }
            else
            {
                foreach (var property in byLevelElement.EnumerateObject())
                {
                    if (!int.TryParse(property.Name, out var level))
                    {
                        reasons.Add(invalidTypeReason);
                        continue;
                    }

                    if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out var levelTax))
                    {
                        reasons.Add(invalidTypeReason);
                        continue;
                    }

                    if (levelTax < 0)
                    {
                        reasons.Add(outOfRangeReason);
                        continue;
                    }

                    taxPerTickByLevel[level] = levelTax;
                }
            }
        }

        if (TryGetPropertyIgnoreCase(residenceElement, "negative_gold_policy", out var policyElement))
        {
            if (policyElement.ValueKind != JsonValueKind.String)
            {
                reasons.Add(invalidTypeReason);
            }
            else
            {
                negativeGoldPolicy = NormalizeNegativeGoldPolicy(policyElement.GetString() ?? string.Empty);
                if (negativeGoldPolicy.Length == 0)
                {
                    reasons.Add(outOfRangeReason);
                    negativeGoldPolicy = NegativeGoldPolicyAllowDebt;
                }
            }
        }

        return new ResidenceTaxSettings(
            taxTickSeconds,
            taxPerTick,
            taxPerTickByLevel,
            negativeGoldPolicy);
    }

    public static ResidenceTaxTraceEntry SettleTaxTick(
        int tickSequence,
        int currentGold,
        int residenceCount,
        int taxPerResidence,
        string negativeGoldPolicy)
    {
        var intervalSeconds = ResidenceTaxCadenceSeconds;
        if (tickSequence <= 0 || tickSequence % intervalSeconds != 0)
        {
            return new ResidenceTaxTraceEntry(
                TickSequence: tickSequence,
                Reason: "no_tax_tick",
                GoldDelta: 0,
                TotalGold: currentGold,
                DebtState: currentGold < 0);
        }

        var taxDelta = checked(residenceCount * taxPerResidence);
        var nextGold = checked(currentGold + taxDelta);
        var normalizedPolicy = NormalizeNegativeGoldPolicy(negativeGoldPolicy);
        if (normalizedPolicy.Length == 0)
        {
            return new ResidenceTaxTraceEntry(
                TickSequence: tickSequence,
                Reason: "invalid_negative_gold_policy",
                GoldDelta: 0,
                TotalGold: currentGold,
                DebtState: currentGold < 0);
        }

        if (normalizedPolicy == NegativeGoldPolicyRefuseNegative && nextGold < 0)
        {
            return new ResidenceTaxTraceEntry(
                TickSequence: tickSequence,
                Reason: "refused_negative_policy",
                GoldDelta: 0,
                TotalGold: currentGold,
                DebtState: currentGold < 0);
        }

        return new ResidenceTaxTraceEntry(
            TickSequence: tickSequence,
            Reason: "tax_applied",
            GoldDelta: taxDelta,
            TotalGold: nextGold,
            DebtState: nextGold < 0);
    }

    private static bool TryGetPath(JsonElement root, out JsonElement value, params string[] segments)
    {
        value = root;
        foreach (var segment in segments)
        {
            if (!TryGetPropertyIgnoreCase(value, segment, out value))
            {
                value = default;
                return false;
            }
        }

        return true;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, System.StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string NormalizeNegativeGoldPolicy(string rawPolicy)
    {
        var normalized = rawPolicy.Trim().ToLowerInvariant();
        return normalized switch
        {
            NegativeGoldPolicyAllowDebt => NegativeGoldPolicyAllowDebt,
            NegativeGoldPolicyRefuseNegative => NegativeGoldPolicyRefuseNegative,
            _ => string.Empty,
        };
    }
}

public static class EconomyRulesReader
{
    public static ResidenceTaxSettings ReadEconomyRules(
        JsonElement root,
        ISet<string> reasons,
        string invalidTypeReason,
        string outOfRangeReason)
    {
        return ResidenceTaxRuntimePolicy.ReadSettings(root, reasons, invalidTypeReason, outOfRangeReason);
    }
}

public readonly record struct ResidenceTaxSettings(
    int TaxTickSeconds,
    int TaxPerTick,
    IReadOnlyDictionary<int, int> TaxPerTickByLevel,
    string NegativeGoldPolicy)
{
    public int ResolveTaxPerTick(int residenceLevel)
    {
        if (TaxPerTickByLevel.TryGetValue(residenceLevel, out var levelValue))
        {
            return levelValue;
        }

        return TaxPerTick;
    }
}

public readonly record struct ResidenceTaxTraceEntry(
    int TickSequence,
    string Reason,
    int GoldDelta,
    int TotalGold,
    bool DebtState);
