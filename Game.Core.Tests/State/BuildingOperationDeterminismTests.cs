using System.Collections.Generic;
using FluentAssertions;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.State;

public sealed class BuildingOperationDeterminismTests
{
    // ACC:T15.24
    [Fact]
    [Trait("acceptance", "ACC:T15.24")]
    public void ShouldProduceIdenticalTimelineAndCompletionOrder_WhenInitialStateAndProgressStepsAreFixed()
    {
        var initialPlan = new[]
        {
            new BuildingOperationPlan("barracks-1", "upgrade", 2),
            new BuildingOperationPlan("wall-1", "repair", 2),
        };
        var progressSteps = new[] { 1, 1 };

        var firstReplay = ReplayScenario(initialPlan, progressSteps);
        var secondReplay = ReplayScenario(initialPlan, progressSteps);

        secondReplay.TickTimeline.Should().Equal(
            firstReplay.TickTimeline,
            "deterministic replays must keep timing and event order identical for identical inputs.");
        secondReplay.CompletedOperations.Should().Equal(
            firstReplay.CompletedOperations,
            "completion order must remain stable across repeated runs.");
    }

    [Fact]
    public void ShouldKeepPendingOperationsUnchanged_WhenProgressStepsContainOnlyZeroes()
    {
        var initialPlan = new[]
        {
            new BuildingOperationPlan("barracks-1", "upgrade", 2),
            new BuildingOperationPlan("wall-1", "repair", 2),
        };
        var progressSteps = new[] { 0, 0, 0 };

        var replay = ReplayScenario(initialPlan, progressSteps);

        replay.CompletedOperations.Should().BeEmpty();
        replay.PendingOperations.Should().Equal("barracks-1:upgrade", "wall-1:repair");
        replay.TickTimeline.Should().ContainInOrder("tick:0:step:0", "tick:1:step:0", "tick:2:step:0");
    }

    private static BuildingOperationReplayResult ReplayScenario(
        IReadOnlyList<BuildingOperationPlan> initialPlan,
        IReadOnlyList<int> progressSteps)
    {
        var sut = new BuildingOperationDeterminismReplay(initialPlan);
        return sut.Replay(progressSteps);
    }
}
