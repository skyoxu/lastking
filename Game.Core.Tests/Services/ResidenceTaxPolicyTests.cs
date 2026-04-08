using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ResidenceTaxPolicyTests
{
    // ACC:T14.16
    [Theory]
    [Trait("acceptance", "ACC:T14.16")]
    [InlineData("\"tax_tick_seconds\": 14, \"tax_per_tick\": 5, \"negative_gold_policy\": \"allow_debt\"")]
    [InlineData("\"tax_tick_seconds\": 15, \"tax_per_tick\": 5.5, \"negative_gold_policy\": \"allow_debt\"")]
    [InlineData("\"tax_tick_seconds\": 15, \"tax_per_tick\": 5, \"negative_gold_policy\": \"undefined\"")]
    public void ShouldRejectInvalidResidenceTaxPolicyAndKeepFallbackSnapshot_WhenInitialLoadContainsContractViolation(string residenceBody)
    {
        var sut = new ConfigManager();
        var baselineSnapshot = BalanceSnapshot.Default;
        var json = CreateBalanceJsonWithResidenceBody(residenceBody);

        var result = sut.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeFalse(
            "residence tax contract must reject cadence drift, non-integer tax values, and undefined negative-gold policy.");
        result.ReasonCodes.Should().NotBeEmpty(
            "rejection path must be explicit and auditable instead of silently mutating state.");
        result.Snapshot.Should().Be(
            baselineSnapshot,
            "invalid residence tax policy input must keep fallback snapshot unchanged.");
    }

    [Fact]
    public void ShouldRejectInvalidResidenceTaxPolicyAndKeepPreviouslyLoadedSnapshot_WhenReloadContainsContractViolation()
    {
        var sut = new ConfigManager();
        var initialJson = CreateBalanceJsonWithResidenceBody(
            "\"tax_tick_seconds\": 15, \"tax_per_tick\": 6, \"negative_gold_policy\": \"allow_debt\"",
            daySeconds: 240);
        var initial = sut.LoadInitialFromJson(initialJson, "res://Config/balance.json");
        var expectedSnapshot = initial.Snapshot;

        var invalidReloadJson = CreateBalanceJsonWithResidenceBody(
            "\"tax_tick_seconds\": 13, \"tax_per_tick\": 2.25, \"negative_gold_policy\": \"invalid-policy\"",
            daySeconds: 301);

        var reload = sut.ReloadFromJson(invalidReloadJson, "res://Config/balance.json");

        initial.Accepted.Should().BeTrue("sanity check: baseline load should succeed before invalid reload.");
        reload.Accepted.Should().BeFalse(
            "reload must refuse invalid residence tax policy input instead of accepting partial or silent changes.");
        reload.ReasonCodes.Should().NotBeEmpty(
            "invalid reload must return explicit rejection reasons.");
        reload.Snapshot.Should().Be(
            expectedSnapshot,
            "when residence tax policy is invalid, reload must keep the previous accepted snapshot unchanged.");
    }

    [Fact]
    public void ShouldRequireExplicitNegativeGoldPolicyBranches_WhenRuntimeTickSettlementIsExecuted()
    {
        var refusedByPolicy = ResidenceTaxRuntimePolicy.SettleTaxTick(
            tickSequence: 15,
            currentGold: 2,
            residenceCount: 1,
            taxPerResidence: -5,
            negativeGoldPolicy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyRefuseNegative);
        var invalidPolicy = ResidenceTaxRuntimePolicy.SettleTaxTick(
            tickSequence: 15,
            currentGold: 10,
            residenceCount: 1,
            taxPerResidence: 5,
            negativeGoldPolicy: "unsupported_policy");

        refusedByPolicy.Reason.Should().Be("refused_negative_policy");
        refusedByPolicy.GoldDelta.Should().Be(0);
        refusedByPolicy.TotalGold.Should().Be(2);
        refusedByPolicy.DebtState.Should().BeFalse();

        invalidPolicy.Reason.Should().Be("invalid_negative_gold_policy");
        invalidPolicy.GoldDelta.Should().Be(0);
        invalidPolicy.TotalGold.Should().Be(10);
    }

    [Fact]
    public void ShouldProduceDeterministicGoldAndDebtTransitions_WhenAllowDebtPolicyIsReplayed()
    {
        var tickSequenceSeconds = new[] { 15, 15, 15 };

        var firstRun = RunDeterministicPolicyModel(
            initialGold: -10,
            residenceCount: 1,
            taxPerResidence: 4,
            cadenceSeconds: 15,
            tickSequenceSeconds: tickSequenceSeconds,
            negativeGoldPolicy: "allow_debt");

        var secondRun = RunDeterministicPolicyModel(
            initialGold: -10,
            residenceCount: 1,
            taxPerResidence: 4,
            cadenceSeconds: 15,
            tickSequenceSeconds: tickSequenceSeconds,
            negativeGoldPolicy: "allow_debt");

        secondRun.FinalGold.Should().Be(firstRun.FinalGold);
        secondRun.FinalDebtState.Should().Be(firstRun.FinalDebtState);
        secondRun.GoldAfterTaxTicks.Should().Equal(firstRun.GoldAfterTaxTicks);
        secondRun.DebtStateAfterTaxTicks.Should().Equal(firstRun.DebtStateAfterTaxTicks);
        secondRun.PolicyOutcomes.Should().Equal(firstRun.PolicyOutcomes);

        firstRun.GoldAfterTaxTicks.Should().Equal(new[] { -6, -2, 2 });
        firstRun.DebtStateAfterTaxTicks.Should().Equal(new[] { true, true, false });
        firstRun.PolicyOutcomes.Should().OnlyContain(outcome => outcome == "applied");
    }

    private static DeterministicPolicyTrace RunDeterministicPolicyModel(
        int initialGold,
        int residenceCount,
        int taxPerResidence,
        int cadenceSeconds,
        IReadOnlyList<int> tickSequenceSeconds,
        string negativeGoldPolicy)
    {
        cadenceSeconds.Should().Be(15, "residence tax policy contract locks cadence at 15 seconds.");

        var supportedPolicies = new[] { "allow_debt", "refuse_negative" };
        supportedPolicies.Should().Contain(
            negativeGoldPolicy,
            "test model only supports explicit negative-gold policies to avoid silent behavior.");

        var gold = initialGold;
        var elapsedSeconds = 0;
        var goldAfterTaxTicks = new List<int>();
        var debtStateAfterTaxTicks = new List<bool>();
        var policyOutcomes = new List<string>();

        foreach (var tickSeconds in tickSequenceSeconds)
        {
            tickSeconds.Should().BeGreaterThan(0);

            elapsedSeconds += tickSeconds;
            if (elapsedSeconds % cadenceSeconds != 0)
            {
                continue;
            }

            var tickDelta = checked(residenceCount * taxPerResidence);
            var nextGold = checked(gold + tickDelta);

            if (negativeGoldPolicy == "refuse_negative" && nextGold < 0)
            {
                policyOutcomes.Add("refused-negative");
                goldAfterTaxTicks.Add(gold);
                debtStateAfterTaxTicks.Add(gold < 0);
                continue;
            }

            gold = nextGold;
            policyOutcomes.Add("applied");
            goldAfterTaxTicks.Add(gold);
            debtStateAfterTaxTicks.Add(gold < 0);
        }

        return new DeterministicPolicyTrace(
            FinalGold: gold,
            FinalDebtState: gold < 0,
            GoldAfterTaxTicks: goldAfterTaxTicks,
            DebtStateAfterTaxTicks: debtStateAfterTaxTicks,
            PolicyOutcomes: policyOutcomes);
    }

    private static string CreateBalanceJsonWithResidenceBody(string residenceBody, int daySeconds = 240)
    {
        return $$"""
{
  "time": {
    "day_seconds": {{daySeconds}},
    "night_seconds": 120
  },
  "waves": {
    "normal": {
      "day1_budget": 50,
      "daily_growth": 1.2
    }
  },
  "channels": {
    "elite": "elite",
    "boss": "boss"
  },
  "spawn": {
    "cadence_seconds": 10
  },
  "boss": {
    "count": 2
  },
  "economy": {
    "residence": {
      {{residenceBody}}
    }
  }
}
""";
    }

    private readonly record struct DeterministicPolicyTrace(
        int FinalGold,
        bool FinalDebtState,
        IReadOnlyList<int> GoldAfterTaxTicks,
        IReadOnlyList<bool> DebtStateAfterTaxTicks,
        IReadOnlyList<string> PolicyOutcomes);
}
