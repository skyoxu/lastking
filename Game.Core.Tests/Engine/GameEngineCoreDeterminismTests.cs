using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Engine;

public sealed class GameEngineCoreDeterminismTests
{
    [Fact]
    public void ShouldMatchApprovedRegressionBaseline_WhenCoreLoopInputsAreFixed()
    {
        var sut = new DeterministicSimulationLoopService();
        var initialEnemyPositions = new[] { 3, 7, 11 };
        var targetThreatPriority = new[] { 2, 5, 1 };

        var outputs = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);

        outputs.Should().Equal(17, 15, 15, 11, 9, 9);
    }

    [Fact]
    public void ShouldRemainBitwiseEqualAcrossRuns_WhenSeedAndInputsAreUnchanged()
    {
        var sut = new DeterministicSimulationLoopService();
        var initialEnemyPositions = new[] { 3, 7, 11 };
        var targetThreatPriority = new[] { 2, 5, 1 };

        var firstRun = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);
        var secondRun = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);

        firstRun.Should().Equal(secondRun);
    }

    [Fact]
    public void ShouldNotKeepCoreLoopOutputsUnchanged_WhenSeedChangesUnderSameInputs()
    {
        var sut = new DeterministicSimulationLoopService();
        var initialEnemyPositions = new[] { 3, 7, 11 };
        var targetThreatPriority = new[] { 2, 5, 1 };

        var baselineOutputs = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);
        var changedSeedOutputs = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1338u, ticks: 6);

        changedSeedOutputs.Should().NotEqual(baselineOutputs);
    }
}
