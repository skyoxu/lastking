using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class RuntimeSpeedTimelineGateValidationTests
{
    // ACC:T23.27
    [Fact]
    public void ShouldAcceptTimelineGate_WhenEntriesUseAllowedModesAndMonotonicTicks()
    {
        var controller = new RuntimeSpeedController("run-23");
        controller.SetTwoX(source: "ui.2x", effectiveTick: 10);
        controller.Pause(source: "ui.pause", effectiveTick: 11);
        controller.Resume(source: "ui.resume", effectiveTick: 12);

        var result = RuntimeSpeedTimelineGate.Validate(controller.Timeline);

        result.Accepted.Should().BeTrue();
        result.Reason.Should().Be("accepted");
    }

    [Fact]
    public void ShouldRejectTimelineGate_WhenTicksMoveBackward()
    {
        var controller = new RuntimeSpeedController("run-23");
        controller.SetTwoX(source: "ui.2x", effectiveTick: 10);
        controller.SetOneX(source: "ui.1x", effectiveTick: 9);

        var result = RuntimeSpeedTimelineGate.Validate(controller.Timeline);

        result.Accepted.Should().BeFalse();
        result.Reason.Should().Be("non_monotonic_tick");
    }

    [Fact]
    public void ShouldRejectTimelineGate_WhenTimelineEvidenceIsMissing()
    {
        var result = RuntimeSpeedTimelineGate.Validate([]);

        result.Accepted.Should().BeFalse();
        result.Reason.Should().Be("missing_evidence");
    }

    [Fact]
    public void ShouldRejectTimelineGate_WhenSourceIsMissing()
    {
        var before = new RuntimeSpeedState("run-23", RuntimeSpeedMode.OneX, 100, IsPaused: false, EffectiveTick: 0, Source: "initial");
        var after = new RuntimeSpeedState("run-23", RuntimeSpeedMode.TwoX, 200, IsPaused: false, EffectiveTick: 1, Source: "");
        var timeline = new[]
        {
            new RuntimeSpeedTimelineEntry("run-23", before, after, 1, "", EventTypes.LastkingTimeScaleChanged),
        };

        var result = RuntimeSpeedTimelineGate.Validate(timeline);

        result.Accepted.Should().BeFalse();
        result.Reason.Should().Be("invalid_source");
    }

    [Fact]
    public void ShouldRejectTimelineGate_WhenAfterStateDoesNotMatchEventEnvelope()
    {
        var before = new RuntimeSpeedState("run-23", RuntimeSpeedMode.OneX, 100, IsPaused: false, EffectiveTick: 0, Source: "initial");
        var after = new RuntimeSpeedState("run-23", RuntimeSpeedMode.Pause, 0, IsPaused: true, EffectiveTick: 99, Source: "ui.pause");
        var timeline = new[]
        {
            new RuntimeSpeedTimelineEntry("run-23", before, after, 1, "ui.pause", EventTypes.LastkingTimeScaleChanged),
        };

        var result = RuntimeSpeedTimelineGate.Validate(timeline);

        result.Accepted.Should().BeFalse();
        result.Reason.Should().Be("state_mismatch");
    }

    [Fact]
    public void ShouldRejectTimelineGate_WhenEventTypeIsNotRecognized()
    {
        var before = new RuntimeSpeedState("run-23", RuntimeSpeedMode.OneX, 100, IsPaused: false, EffectiveTick: 0, Source: "initial");
        var after = new RuntimeSpeedState("run-23", RuntimeSpeedMode.TwoX, 200, IsPaused: false, EffectiveTick: 1, Source: "ui.2x");
        var timeline = new[]
        {
            new RuntimeSpeedTimelineEntry("run-23", before, after, 1, "ui.2x", "invalid"),
        };

        var result = RuntimeSpeedTimelineGate.Validate(timeline);

        result.Accepted.Should().BeFalse();
        result.Reason.Should().Be("invalid_event_type");
    }
}
