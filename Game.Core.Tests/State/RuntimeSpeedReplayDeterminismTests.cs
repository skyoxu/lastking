using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.State;

public sealed class RuntimeSpeedReplayDeterminismTests
{
    // ACC:T23.26
    [Fact]
    public void ShouldReplayIdenticalRuntimeSpeedTimeline_WhenInputsAreIdentical()
    {
        var inputs = new[]
        {
            new RuntimeSpeedInput(RuntimeSpeedMode.TwoX, "ui.2x", 10),
            new RuntimeSpeedInput(RuntimeSpeedMode.Pause, "ui.pause", 20),
            new RuntimeSpeedInput(RuntimeSpeedMode.OneX, "ui.1x", 30),
            new RuntimeSpeedInput(RuntimeSpeedMode.TwoX, "ui.2x", 40),
        };

        var first = RuntimeSpeedReplay.Run("run-23", inputs);
        var second = RuntimeSpeedReplay.Run("run-23", inputs);

        second.TimelineAnchors.Should().Equal(first.TimelineAnchors);
        second.CompletedCountdownOrder.Should().Equal(first.CompletedCountdownOrder);
        second.ProgressMilestones.Should().Equal(first.ProgressMilestones);
        second.FinalState.Should().Be(first.FinalState);

        first.CompletedCountdownOrder.Should().Equal("alpha", "beta");
        first.ProgressMilestones.Should().Equal("10:2", "20:2", "30:3", "40:5");
    }
}
