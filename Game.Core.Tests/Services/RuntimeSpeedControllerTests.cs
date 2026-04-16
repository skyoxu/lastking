using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class RuntimeSpeedControllerTests
{
    // ACC:T23.1
    // ACC:T23.4
    // ACC:T23.5
    // ACC:T23.11
    // ACC:T23.13
    // ACC:T23.18
    [Fact]
    public void ShouldAllowOnlyPauseOneXAndTwoXStates_WhenRuntimeSpeedChanges()
    {
        var controller = new RuntimeSpeedController("run-23");

        var paused = controller.Pause(source: "ui.pause", effectiveTick: 10);
        var resumed = controller.Resume(source: "ui.resume", effectiveTick: 20);
        var twoX = controller.SetTwoX(source: "ui.2x", effectiveTick: 30);
        var oneX = controller.SetOneX(source: "ui.1x", effectiveTick: 40);

        paused.Mode.Should().Be(RuntimeSpeedMode.Pause);
        paused.EffectiveScalePercent.Should().Be(0);
        paused.IsPaused.Should().BeTrue();
        resumed.Mode.Should().Be(RuntimeSpeedMode.OneX);
        resumed.EffectiveScalePercent.Should().Be(100);
        twoX.Mode.Should().Be(RuntimeSpeedMode.TwoX);
        twoX.EffectiveScalePercent.Should().Be(200);
        oneX.Mode.Should().Be(RuntimeSpeedMode.OneX);
        oneX.EffectiveScalePercent.Should().Be(100);
        controller.SetScale("run-23", 150, isPaused: false).CurrentScalePercent.Should().Be(100);
    }

    // ACC:T23.16
    [Fact]
    public void ShouldApplyImmediateSpeedSwitchesWithoutResettingRuntimeState_WhenToggledRapidly()
    {
        var controller = new RuntimeSpeedController("run-23");
        var scheduler = new RuntimeCountdownScheduler();
        scheduler.Enqueue("wave-1", remainingSeconds: 10d);

        scheduler.Advance(2d, controller.Current);
        controller.SetTwoX(source: "ui.2x", effectiveTick: 2);
        scheduler.Advance(1d, controller.Current);
        controller.SetOneX(source: "ui.1x", effectiveTick: 3);
        scheduler.Advance(1d, controller.Current);

        controller.Current.EffectiveScalePercent.Should().Be(100);
        scheduler.GetRemainingSeconds("wave-1").Should().BeApproximately(5d, 0.000001d);
        controller.Timeline.Select(entry => entry.After.EffectiveScalePercent).Should().Equal(200, 100);
    }

    // ACC:T23.21
    [Fact]
    public void ShouldExposeSingleRuntimeSpeedOwnerEvidence_WhenControllerIsInstantiated()
    {
        var controller = new RuntimeSpeedController("run-23");

        controller.OwnerKey.Should().Be("RuntimeSpeedController:run-23");
        RuntimeSpeedController.AllowedScalePercents.Should().Equal(0, 100, 200);
    }
}
