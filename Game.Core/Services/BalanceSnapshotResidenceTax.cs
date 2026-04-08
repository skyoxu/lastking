using System.Collections.Generic;

namespace Game.Core.Services;

public sealed partial record BalanceSnapshot
{
    private static readonly IReadOnlyDictionary<int, int> EmptyResidenceTaxByLevel = new Dictionary<int, int>();

    public int ResidenceTaxTickSeconds { get; init; } = 15;
    public int ResidenceTaxPerTick { get; init; }
    public IReadOnlyDictionary<int, int> ResidenceTaxPerTickByLevel { get; init; } = EmptyResidenceTaxByLevel;
    public string NegativeGoldPolicy { get; init; } = ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt;

    public int GetResidenceTaxPerTickForLevel(int residenceLevel)
    {
        if (ResidenceTaxPerTickByLevel.TryGetValue(residenceLevel, out var levelOverride))
        {
            return levelOverride;
        }

        return ResidenceTaxPerTick;
    }

    public BalanceSnapshot WithEconomyRules(ResidenceTaxSettings settings)
    {
        return this with
        {
            ResidenceTaxTickSeconds = settings.TaxTickSeconds,
            ResidenceTaxPerTick = settings.TaxPerTick,
            ResidenceTaxPerTickByLevel = settings.TaxPerTickByLevel,
            NegativeGoldPolicy = settings.NegativeGoldPolicy,
        };
    }
}
