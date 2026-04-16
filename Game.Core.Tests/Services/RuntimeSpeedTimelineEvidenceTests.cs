using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class RuntimeSpeedTimelineEvidenceTests
{
    // ACC:T23.25
    [Fact]
    public void ShouldRecordBeforeAfterEffectiveTickAndSource_WhenSpeedChanges()
    {
        var controller = new RuntimeSpeedController("run-23");

        controller.SetTwoX(source: "ui.2x", effectiveTick: 42);
        controller.Pause(source: "ui.pause", effectiveTick: 45);

        controller.Timeline.Should().HaveCount(2);
        controller.Timeline[0].EventType.Should().Be(EventTypes.LastkingTimeScaleChanged);
        controller.Timeline[0].Before.EffectiveScalePercent.Should().Be(100);
        controller.Timeline[0].After.EffectiveScalePercent.Should().Be(200);
        controller.Timeline[0].EffectiveTick.Should().Be(42);
        controller.Timeline[0].Source.Should().Be("ui.2x");
        controller.Timeline[1].Before.EffectiveScalePercent.Should().Be(200);
        controller.Timeline[1].After.EffectiveScalePercent.Should().Be(0);
        controller.Timeline[1].Source.Should().Be("ui.pause");
    }
}
