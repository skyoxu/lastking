using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ResidenceTaxTraceTests
{
    // ACC:T14.18
    [Fact]
    [Trait("acceptance", "ACC:T14.18")]
    public void ShouldExposeStructuredResidenceTaxTraceFields_WhenRuntimeSettlementExecutes()
    {
        var trace = ResidenceTaxRuntimePolicy.SettleTaxTick(
            tickSequence: 15,
            currentGold: 120,
            residenceCount: 2,
            taxPerResidence: 5,
            negativeGoldPolicy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt);

        trace.TickSequence.Should().Be(15);
        trace.Reason.Should().Be("tax_applied");
        trace.GoldDelta.Should().Be(10);
        trace.TotalGold.Should().Be(130);
        trace.DebtState.Should().BeFalse();
    }

    [Fact]
    public void ShouldReplayIdenticalTraceOrderAndAggregate_WhenInputsAreFixed()
    {
        var ticks = new[] { 15, 30, 45, 60 };
        var firstRun = ReplayTrace(
            initialGold: 100,
            residenceCount: 2,
            taxPerResidence: 5,
            tickSequenceSeconds: ticks,
            policy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt);
        var secondRun = ReplayTrace(
            initialGold: 100,
            residenceCount: 2,
            taxPerResidence: 5,
            tickSequenceSeconds: ticks,
            policy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt);

        secondRun.Entries.Should().Equal(firstRun.Entries);
        secondRun.AggregatedGoldDelta.Should().Be(firstRun.AggregatedGoldDelta);
        secondRun.FinalGold.Should().Be(firstRun.FinalGold);

        firstRun.AggregatedGoldDelta.Should().Be(firstRun.Entries.Sum(entry => entry.GoldDelta));
        firstRun.FinalGold.Should().Be(100 + firstRun.AggregatedGoldDelta);
    }

    [Fact]
    public void ShouldChangeTraceAggregate_WhenTickSequenceChanges()
    {
        var firstRun = ReplayTrace(
            initialGold: 100,
            residenceCount: 2,
            taxPerResidence: 5,
            tickSequenceSeconds: new[] { 15, 30, 45, 60 },
            policy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt);
        var secondRun = ReplayTrace(
            initialGold: 100,
            residenceCount: 2,
            taxPerResidence: 5,
            tickSequenceSeconds: new[] { 15, 45, 61, 105 },
            policy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt);

        secondRun.Entries.Should().NotEqual(firstRun.Entries);
        secondRun.AggregatedGoldDelta.Should().NotBe(firstRun.AggregatedGoldDelta);
    }

    private static DeterministicTraceReplay ReplayTrace(
        int initialGold,
        int residenceCount,
        int taxPerResidence,
        IReadOnlyList<int> tickSequenceSeconds,
        string policy)
    {
        var currentGold = initialGold;
        var entries = new List<ResidenceTaxTraceEntry>(tickSequenceSeconds.Count);

        foreach (var tickSequence in tickSequenceSeconds)
        {
            var trace = ResidenceTaxRuntimePolicy.SettleTaxTick(
                tickSequence: tickSequence,
                currentGold: currentGold,
                residenceCount: residenceCount,
                taxPerResidence: taxPerResidence,
                negativeGoldPolicy: policy);

            entries.Add(trace);
            currentGold = trace.TotalGold;
        }

        var aggregatedGoldDelta = entries
            .Where(entry => entry.Reason == "tax_applied")
            .Sum(entry => entry.GoldDelta);
        return new DeterministicTraceReplay(entries, aggregatedGoldDelta, currentGold);
    }

    private readonly record struct DeterministicTraceReplay(
        IReadOnlyList<ResidenceTaxTraceEntry> Entries,
        int AggregatedGoldDelta,
        int FinalGold);
}
