using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Engine;

public sealed class CoreLoopDeterminismRegressionTests
{
    // ACC:T27.3
    [Fact]
    [Trait("acceptance", "ACC:T27.3")]
    public void ShouldRemainBitwiseEqual_WhenReplayedWithSameSeedAndInputs()
    {
        var sut = new DeterministicSimulationLoopService();
        var initialEnemyPositions = new[] { 3, 7, 11 };
        var targetThreatPriority = new[] { 2, 5, 1 };

        var firstRun = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);
        var secondRun = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);

        secondRun.Should().Equal(firstRun);
    }

    [Fact]
    public void ShouldChangeOutputTrace_WhenSeedChangesUnderSameInputs()
    {
        var sut = new DeterministicSimulationLoopService();
        var initialEnemyPositions = new[] { 3, 7, 11 };
        var targetThreatPriority = new[] { 2, 5, 1 };

        var baselineRun = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);
        var changedSeedRun = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1338u, ticks: 6);

        changedSeedRun.Should().NotEqual(baselineRun);
    }

    [Fact]
    public void ShouldMatchFrozenPreAchievementsBaseline_WhenCoreLoopIsExecutedWithApprovedSeed()
    {
        var sut = new DeterministicSimulationLoopService();
        var initialEnemyPositions = new[] { 3, 7, 11 };
        var targetThreatPriority = new[] { 2, 5, 1 };

        var run = sut.RunLoop(initialEnemyPositions, targetThreatPriority, seed: 1337u, ticks: 6);
        var frozenPreAchievementsBaseline = new[] { 17, 15, 15, 11, 9, 9 };

        run.Should().Equal(
            frozenPreAchievementsBaseline,
            "Task 27 must preserve the pre-achievements deterministic core-loop baseline without regression.");
    }
}
