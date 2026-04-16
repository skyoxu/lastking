using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class UIFeedbackRegressionTests
{
    // ACC:T24.21
    [Fact]
    public void ShouldAggregateRepeatedInvalidPlacements_WhenSameReasonCodeIsReported()
    {
        var pipeline = new UIFeedbackPipeline();

        pipeline.ReportInvalidPlacement("tile_occupied");
        pipeline.ReportInvalidPlacement("tile_occupied");
        pipeline.ReportInvalidPlacement("tile_occupied");

        pipeline.Events.Should().HaveCount(1);
        pipeline.Events[0].Category.Should().Be("invalid_placement");
        pipeline.Events[0].ReasonCode.Should().Be("tile_occupied");
        pipeline.Events[0].MessageKey.Should().Be("ui.invalid_action.tile_occupied");
        pipeline.Events[0].RepeatCount.Should().Be(3);
    }

    [Fact]
    public void ShouldKeepFeedbackSeverityUnchanged_WhenBlockedActionRepeats()
    {
        var pipeline = new UIFeedbackPipeline();

        pipeline.ReportBlockedAction("insufficient_resources", "resource_gate");
        pipeline.ReportBlockedAction("insufficient_resources", "resource_gate");

        pipeline.Events.Should().HaveCount(1, "repeated blocked actions should not create duplicate feedback rows");
        pipeline.Events[0].Category.Should().Be("blocked_action");
        pipeline.Events[0].Severity.Should().Be("warning", "blocked actions must not escalate severity across repeated identical errors");
        pipeline.Events[0].RepeatCount.Should().Be(2);
    }

    [Fact]
    public void ShouldKeepSingleMigrationFailureFeedback_WhenMigrationFailureRepeatsAfterLoadFailure()
    {
        var pipeline = new UIFeedbackPipeline();

        pipeline.ReportLoadFailure("json_parse_error", "slot_a");
        pipeline.ReportMigrationFailure("missing_required_field", "slot_a");
        pipeline.ReportMigrationFailure("missing_required_field", "slot_a");

        pipeline.Events.Select(e => e.MessageKey).Should().Equal(
            "ui.load_failure.json_parse_error",
            "ui.migration_failure.missing_required_field");
        pipeline.Events[1].RepeatCount.Should().Be(2);
    }
}
