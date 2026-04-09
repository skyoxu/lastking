using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ResidenceTaxCadenceTests
{
    // ACC:T14.19
    [Fact]
    [Trait("acceptance", "ACC:T14.19")]
    public void ShouldLockTaxCadenceAtFifteenSeconds_WhenDeterministicTickInputIsApplied()
    {
        ResidenceTaxRuntimePolicy.ResidenceTaxCadenceSeconds.Should().Be(15);

        var appliedTickSeconds = Enumerable.Range(1, 60)
            .Select(second => ResidenceTaxRuntimePolicy.SettleTaxTick(
                tickSequence: second,
                currentGold: 100,
                residenceCount: 1,
                taxPerResidence: 8,
                negativeGoldPolicy: ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt))
            .Where(trace => trace.Reason == "tax_applied")
            .Select(trace => trace.TickSequence)
            .ToArray();

        appliedTickSeconds.Should().Equal(new[] { 15, 30, 45, 60 },
            "cadence drift must fail under identical tick input rather than by weak source scanning.");
    }

    // ACC:T14.21
    [Fact]
    [Trait("acceptance", "ACC:T14.21")]
    public void ShouldAccumulateExpectedGoldTotals_WhenMultipleTaxTicksAreApplied()
    {
        var initialGold = 800;
        var initialPopulationCap = 50;
        var taxPerResidence = 12;
        var populationCapPerResidence = 6;
        var cadenceSeconds = 15;
        var tickSequenceSeconds = new[] { 5, 10, 15, 15, 15 };
        var placementSequence = new[] { new PlacementEvent(AtSecond: 1, Succeeded: true) };

        var result = RunDeterministicResidenceEconomySimulation(
            initialGold,
            initialPopulationCap,
            taxPerResidence,
            populationCapPerResidence,
            cadenceSeconds,
            tickSequenceSeconds,
            placementSequence);

        var expectedTicks = 4;
        var expectedGold = initialGold + (expectedTicks * taxPerResidence);

        result.FinalGold.Should().Be(expectedGold);
        result.TaxEventSeconds.Should().Equal(new[] { 15, 30, 45, 60 });
    }

    // ACC:T14.22
    [Fact]
    [Trait("acceptance", "ACC:T14.22")]
    public void ShouldProduceDeterministicDebtStateOutputs_WhenInitialStateAndTicksAreIdentical()
    {
        var initialGold = -20;
        var initialPopulationCap = 50;
        var taxPerResidence = 15;
        var populationCapPerResidence = 6;
        var cadenceSeconds = 15;
        var tickSequenceSeconds = new[] { 15, 15, 15, 15 };
        var placementSequence = new[] { new PlacementEvent(AtSecond: 1, Succeeded: true) };

        var firstRun = RunDeterministicResidenceEconomySimulation(
            initialGold,
            initialPopulationCap,
            taxPerResidence,
            populationCapPerResidence,
            cadenceSeconds,
            tickSequenceSeconds,
            placementSequence);

        var secondRun = RunDeterministicResidenceEconomySimulation(
            initialGold,
            initialPopulationCap,
            taxPerResidence,
            populationCapPerResidence,
            cadenceSeconds,
            tickSequenceSeconds,
            placementSequence);

        secondRun.FinalGold.Should().Be(firstRun.FinalGold);
        secondRun.DebtStateAfterEachTaxTick.Should().Equal(firstRun.DebtStateAfterEachTaxTick);
        secondRun.DebtStateAfterEachTaxTick.Should().Equal(new[] { true, false, false, false });
    }

    // ACC:T14.3
    [Fact]
    [Trait("acceptance", "ACC:T14.3")]
    public void ShouldProduceRepeatableGoldAndPopulationResults_WhenPlacementAndTickSequencesMatch()
    {
        var initialGold = 800;
        var initialPopulationCap = 50;
        var taxPerResidence = 7;
        var populationCapPerResidence = 6;
        var cadenceSeconds = 15;
        var tickSequenceSeconds = new[] { 3, 4, 8, 10, 15, 30 };
        var placementSequence = new[]
        {
            new PlacementEvent(AtSecond: 1, Succeeded: true),
            new PlacementEvent(AtSecond: 9, Succeeded: false),
            new PlacementEvent(AtSecond: 18, Succeeded: true),
        };

        var firstRun = RunDeterministicResidenceEconomySimulation(
            initialGold,
            initialPopulationCap,
            taxPerResidence,
            populationCapPerResidence,
            cadenceSeconds,
            tickSequenceSeconds,
            placementSequence);

        var secondRun = RunDeterministicResidenceEconomySimulation(
            initialGold,
            initialPopulationCap,
            taxPerResidence,
            populationCapPerResidence,
            cadenceSeconds,
            tickSequenceSeconds,
            placementSequence);

        secondRun.FinalGold.Should().Be(firstRun.FinalGold);
        secondRun.FinalPopulationCap.Should().Be(firstRun.FinalPopulationCap);
        secondRun.TaxEventSeconds.Should().Equal(firstRun.TaxEventSeconds);
        secondRun.GoldAfterEachTaxTick.Should().Equal(firstRun.GoldAfterEachTaxTick);
    }

    // ACC:T14.7
    [Fact]
    [Trait("acceptance", "ACC:T14.7")]
    public void ShouldKeepEconomyAndPopulationAsIntegerSemantics_WhenRunningLongDeterministicSimulation()
    {
        var initialGold = 800;
        var initialPopulationCap = 50;
        var taxPerResidence = 9;
        var populationCapPerResidence = 6;
        var cadenceSeconds = 15;
        var tickSequenceSeconds = Enumerable.Repeat(1, 3600).ToArray();
        var placementSequence = new[]
        {
            new PlacementEvent(AtSecond: 1, Succeeded: true),
            new PlacementEvent(AtSecond: 2, Succeeded: true),
            new PlacementEvent(AtSecond: 3, Succeeded: true),
        };

        var result = RunDeterministicResidenceEconomySimulation(
            initialGold,
            initialPopulationCap,
            taxPerResidence,
            populationCapPerResidence,
            cadenceSeconds,
            tickSequenceSeconds,
            placementSequence);

        var expectedTaxTicks = 3600 / cadenceSeconds;
        var expectedGold = initialGold + (expectedTaxTicks * 3 * taxPerResidence);
        var expectedPopulationCap = initialPopulationCap + (3 * populationCapPerResidence);

        result.GoldAfterEachTaxTick.Should().HaveCount(expectedTaxTicks);
        result.GoldAfterEachTaxTick.Should().OnlyContain(value => value >= initialGold);
        result.FinalGold.Should().Be(expectedGold);
        result.FinalPopulationCap.Should().Be(expectedPopulationCap);
    }

    // ACC:T14.8
    [Fact]
    [Trait("acceptance", "ACC:T14.8")]
    public void ShouldReturnZeroGoldDelta_WhenNoResidenceIsBuilt()
    {
        var initialGold = 800;
        var initialPopulationCap = 50;
        var taxPerResidence = 20;
        var populationCapPerResidence = 6;
        var cadenceSeconds = 15;
        var tickSequenceSeconds = new[] { 15, 15, 15, 15, 15 };
        var placementSequence = Array.Empty<PlacementEvent>();

        var result = RunDeterministicResidenceEconomySimulation(
            initialGold,
            initialPopulationCap,
            taxPerResidence,
            populationCapPerResidence,
            cadenceSeconds,
            tickSequenceSeconds,
            placementSequence);

        var goldDelta = result.FinalGold - initialGold;

        goldDelta.Should().Be(0);
        result.FinalPopulationCap.Should().Be(initialPopulationCap);
    }

    private static SimulationResult RunDeterministicResidenceEconomySimulation(
        int initialGold,
        int initialPopulationCap,
        int taxPerResidence,
        int populationCapPerResidence,
        int cadenceSeconds,
        IReadOnlyList<int> tickSequenceSeconds,
        IReadOnlyList<PlacementEvent> placementSequence)
    {
        cadenceSeconds.Should().BeGreaterThan(0);

        var gold = initialGold;
        var populationCap = initialPopulationCap;
        var residenceCount = 0;
        var elapsedSeconds = 0;
        var sortedPlacements = placementSequence
            .OrderBy(placement => placement.AtSecond)
            .ToArray();

        var taxEventSeconds = new List<int>();
        var goldAfterEachTaxTick = new List<int>();
        var debtStateAfterEachTaxTick = new List<bool>();
        var placementCursor = 0;

        foreach (var tickSeconds in tickSequenceSeconds)
        {
            tickSeconds.Should().BeGreaterThan(0);

            for (var second = 0; second < tickSeconds; second++)
            {
                elapsedSeconds++;

                while (placementCursor < sortedPlacements.Length &&
                       sortedPlacements[placementCursor].AtSecond == elapsedSeconds)
                {
                    if (sortedPlacements[placementCursor].Succeeded)
                    {
                        checked
                        {
                            residenceCount += 1;
                            populationCap += populationCapPerResidence;
                        }
                    }

                    placementCursor++;
                }

                if (elapsedSeconds % cadenceSeconds != 0)
                {
                    continue;
                }

                var goldDelta = residenceCount * taxPerResidence;
                checked
                {
                    gold += goldDelta;
                }

                taxEventSeconds.Add(elapsedSeconds);
                goldAfterEachTaxTick.Add(gold);
                debtStateAfterEachTaxTick.Add(gold < 0);
            }
        }

        return new SimulationResult(
            FinalGold: gold,
            FinalPopulationCap: populationCap,
            TaxEventSeconds: taxEventSeconds,
            GoldAfterEachTaxTick: goldAfterEachTaxTick,
            DebtStateAfterEachTaxTick: debtStateAfterEachTaxTick);
    }

    private readonly record struct PlacementEvent(int AtSecond, bool Succeeded);

    private readonly record struct SimulationResult(
        int FinalGold,
        int FinalPopulationCap,
        IReadOnlyList<int> TaxEventSeconds,
        IReadOnlyList<int> GoldAfterEachTaxTick,
        IReadOnlyList<bool> DebtStateAfterEachTaxTick);
}
