using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class CountdownSchedulerPauseTests
{
    // ACC:T23.7
    // ACC:T23.8
    // ACC:T23.12
    // ACC:T23.17
    [Fact]
    public void ShouldUseExactRemainingValue_WhenPausedCountdownResumes()
    {
        var controller = new RuntimeSpeedController("run-23");
        var scheduler = new RuntimeCountdownScheduler();
        scheduler.Enqueue("wave-1", remainingSeconds: 10d);

        scheduler.Advance(3d, controller.Current);
        controller.Pause(source: "ui.pause", effectiveTick: 3);
        var remainingAtPause = scheduler.GetRemainingSeconds("wave-1");
        var pausedAdvance = scheduler.Advance(100d, controller.Current);
        controller.Resume(source: "ui.resume", effectiveTick: 4);
        scheduler.Advance(2d, controller.Current);

        remainingAtPause.Should().BeApproximately(7d, 0.000001d);
        pausedAdvance.CompletedTimerIds.Should().BeEmpty();
        scheduler.GetRemainingSeconds("wave-1").Should().BeApproximately(5d, 0.000001d);
    }

    // ACC:T23.15
    [Fact]
    public void ShouldDecreaseCountdownFasterAtTwoXThanOneX_WhenWallClockDurationMatches()
    {
        var oneXController = new RuntimeSpeedController("one-x");
        var twoXController = new RuntimeSpeedController("two-x");
        twoXController.SetTwoX(source: "test.2x", effectiveTick: 1);
        var oneXScheduler = new RuntimeCountdownScheduler();
        var twoXScheduler = new RuntimeCountdownScheduler();
        oneXScheduler.Enqueue("wave", 10d);
        twoXScheduler.Enqueue("wave", 10d);

        oneXScheduler.Advance(2d, oneXController.Current);
        twoXScheduler.Advance(2d, twoXController.Current);

        oneXScheduler.GetRemainingSeconds("wave").Should().BeApproximately(8d, 0.000001d);
        twoXScheduler.GetRemainingSeconds("wave").Should().BeApproximately(6d, 0.000001d);
        twoXScheduler.GetRemainingSeconds("wave").Should().BeLessThan(oneXScheduler.GetRemainingSeconds("wave"));
    }

    // ACC:T23.20
    [Fact]
    public void ShouldPreserveSavedOrder_WhenQueuedCountdownsResumeAfterPause()
    {
        var controller = new RuntimeSpeedController("run-23");
        var scheduler = new RuntimeCountdownScheduler();
        scheduler.Enqueue("before-pause", remainingSeconds: 3d);
        controller.Pause(source: "ui.pause", effectiveTick: 1);
        scheduler.Enqueue("during-pause", remainingSeconds: 2d);

        scheduler.Advance(20d, controller.Current);
        controller.SetOneX(source: "ui.1x", effectiveTick: 2);
        var firstAdvance = scheduler.Advance(2d, controller.Current);
        var secondAdvance = scheduler.Advance(1d, controller.Current);

        firstAdvance.CompletedTimerIds.Should().ContainSingle().Which.Should().Be("during-pause");
        secondAdvance.CompletedTimerIds.Should().ContainSingle().Which.Should().Be("before-pause");
        scheduler.PendingTimers.Should().BeEmpty();
    }
}
