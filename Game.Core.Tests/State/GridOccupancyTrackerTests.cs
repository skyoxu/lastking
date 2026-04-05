using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.State;

public sealed class GridOccupancyTrackerTests
{
    // ACC:T13.8
    [Theory]
    [InlineData("out-of-bounds", BuildingPlacementReasonCodes.OutOfBounds)]
    [InlineData("occupied", BuildingPlacementReasonCodes.Occupied)]
    public void ShouldRejectWithDeterministicReasonCode_WhenAnyFootprintCellIsInvalid(string invalidKind, string expectedReason)
    {
        var tracker = new GridOccupancyTracker(width: 4, height: 4);
        var footprint = new[] { new GridPoint(1, 1), new GridPoint(2, 1) };

        if (invalidKind == "out-of-bounds")
        {
            footprint = new[] { new GridPoint(3, 3), new GridPoint(4, 3) };
        }
        else
        {
            tracker.Occupied.Add(new GridPoint(2, 1));
        }

        var result = tracker.ValidateFootprint(footprint);

        result.Should().Be(expectedReason);
    }

    // ACC:T13.11
    [Fact]
    public void ShouldCommitOnlyInBoundsDiscreteCells_WhenPlacementIsAccepted()
    {
        var tracker = new GridOccupancyTracker(width: 6, height: 6);
        var footprint = new[] { new GridPoint(2, 2), new GridPoint(3, 2), new GridPoint(3, 3) };

        tracker.ValidateFootprint(footprint).Should().BeNull();
        tracker.Commit(footprint);

        tracker.Occupied.Should().BeEquivalentTo(footprint);
    }

    // ACC:T13.15
    [Fact]
    public void ShouldKeepDeterministicValidationOrder_WhenOutOfBoundsAndBlockedExistTogether()
    {
        var tracker = new GridOccupancyTracker(width: 4, height: 4);
        tracker.Blocked.Add(new GridPoint(3, 3));
        var footprint = new[] { new GridPoint(3, 3), new GridPoint(4, 3) };

        var reason = tracker.ValidateFootprint(footprint);

        reason.Should().Be(BuildingPlacementReasonCodes.OutOfBounds);
    }
}
