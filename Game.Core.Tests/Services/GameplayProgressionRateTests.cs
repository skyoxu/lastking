using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class GameplayProgressionRateTests
{
    // ACC:T23.3
    // ACC:T23.6
    // ACC:T23.19
    [Fact]
    public void ShouldOrderGameplayProgressionBySpeed_WhenWallClockDurationMatches()
    {
        var controller = new RuntimeSpeedController("run-23");

        controller.Pause(source: "test.pause", effectiveTick: 1);
        var pausedProgress = controller.AdvanceGameplayProgress(10d);
        controller.SetOneX(source: "test.1x", effectiveTick: 2);
        var oneXProgress = controller.AdvanceGameplayProgress(10d);
        controller.SetTwoX(source: "test.2x", effectiveTick: 3);
        var twoXProgress = controller.AdvanceGameplayProgress(10d);

        pausedProgress.Should().Be(0d);
        oneXProgress.Should().Be(10d);
        twoXProgress.Should().Be(20d);
        twoXProgress.Should().BeGreaterThan(oneXProgress);
        oneXProgress.Should().BeGreaterThan(pausedProgress);
    }
}
