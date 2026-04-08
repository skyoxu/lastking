using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ResidenceTaxDeterminismTests
{
    // ACC:T14.17
    [Fact]
    [Trait("acceptance", "ACC:T14.17")]
    public void ShouldEnforceDeterministicIntegerSettlementOrder_WhenResidenceTaxTickRuntimeIsReplayed()
    {
        var ticks = new[] { 15, 30, 45 };
        var firstRun = ReplayTickSequence(
            initialGold: -20,
            residenceCount: 1,
            taxPerResidence: 10,
            tickSequenceSeconds: ticks,
            policy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt);
        var secondRun = ReplayTickSequence(
            initialGold: -20,
            residenceCount: 1,
            taxPerResidence: 10,
            tickSequenceSeconds: ticks,
            policy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt);

        secondRun.FinalGold.Should().Be(firstRun.FinalGold);
        secondRun.FinalDebtState.Should().Be(firstRun.FinalDebtState);
        secondRun.GoldAfterTaxTicks.Should().Equal(firstRun.GoldAfterTaxTicks);
        secondRun.DebtStateAfterTaxTicks.Should().Equal(firstRun.DebtStateAfterTaxTicks);
        secondRun.Reasons.Should().Equal(firstRun.Reasons);

        firstRun.Reasons.Should().Equal(new[] { "tax_applied", "tax_applied", "tax_applied" });
        firstRun.GoldAfterTaxTicks.Should().Equal(new[] { -10, 0, 10 });
        firstRun.DebtStateAfterTaxTicks.Should().Equal(new[] { true, false, false });
    }

    [Fact]
    public void ShouldKeepGoldAndDebtUnchanged_WhenTickSequenceNeverReachesCadenceBoundary()
    {
        var result = ReplayTickSequence(
            initialGold: -3,
            residenceCount: 2,
            taxPerResidence: 5,
            tickSequenceSeconds: new[] { 1, 2, 3, 4 },
            policy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt);

        result.FinalGold.Should().Be(-3);
        result.FinalDebtState.Should().BeTrue();
        result.GoldAfterTaxTicks.Should().BeEmpty();
        result.DebtStateAfterTaxTicks.Should().BeEmpty();
        result.Reasons.Should().OnlyContain(reason => reason == "no_tax_tick");
    }

    [Fact]
    public void ShouldRejectUnknownPolicyWithoutMutatingState_WhenSettlementHitsCadenceBoundary()
    {
        var result = ReplayTickSequence(
            initialGold: 10,
            residenceCount: 1,
            taxPerResidence: 5,
            tickSequenceSeconds: new[] { 15, 30 },
            policy: "unsupported_policy");

        result.FinalGold.Should().Be(10);
        result.FinalDebtState.Should().BeFalse();
        result.GoldAfterTaxTicks.Should().BeEmpty();
        result.Reasons.Should().OnlyContain(reason => reason == "invalid_negative_gold_policy");
    }

    private static DeterministicTaxTrace ReplayTickSequence(
        int initialGold,
        int residenceCount,
        int taxPerResidence,
        IReadOnlyList<int> tickSequenceSeconds,
        string policy)
    {
        var currentGold = initialGold;
        var goldAfterTaxTicks = new List<int>();
        var debtStateAfterTaxTicks = new List<bool>();
        var reasons = new List<string>();

        foreach (var tickSequence in tickSequenceSeconds)
        {
            var trace = ResidenceTaxRuntimePolicy.SettleTaxTick(
                tickSequence: tickSequence,
                currentGold: currentGold,
                residenceCount: residenceCount,
                taxPerResidence: taxPerResidence,
                negativeGoldPolicy: policy);

            reasons.Add(trace.Reason);
            currentGold = trace.TotalGold;
            if (trace.Reason == "tax_applied")
            {
                goldAfterTaxTicks.Add(trace.TotalGold);
                debtStateAfterTaxTicks.Add(trace.DebtState);
            }
        }

        return new DeterministicTaxTrace(
            FinalGold: currentGold,
            FinalDebtState: currentGold < 0,
            GoldAfterTaxTicks: goldAfterTaxTicks,
            DebtStateAfterTaxTicks: debtStateAfterTaxTicks,
            Reasons: reasons);
    }

    private readonly record struct DeterministicTaxTrace(
        int FinalGold,
        bool FinalDebtState,
        IReadOnlyList<int> GoldAfterTaxTicks,
        IReadOnlyList<bool> DebtStateAfterTaxTicks,
        IReadOnlyList<string> Reasons);
}
