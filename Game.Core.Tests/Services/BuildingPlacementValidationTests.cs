using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BuildingPlacementValidationTests
{
    // ACC:T13.3
    [Fact]
    public void ShouldAcceptAllRequiredBuildingSubtypes_WhenFootprintsAreValid()
    {
        var service = new BuildingPlacementService();

        foreach (var buildingType in BuildingCatalog.GetAll().Keys)
        {
            var state = new BuildingPlacementState(width: 12, height: 12, resources: 1000);
            var result = service.TryPlace(buildingType, new GridPoint(2, 2), state);

            result.IsAccepted.Should().BeTrue();
            result.Reason.Should().Be(BuildingPlacementReasonCodes.Accepted);
            state.Placements.Should().ContainSingle();
            state.Placements[0].BuildingType.Should().Be(buildingType);
        }
    }

    // ACC:T13.4
    // ACC:T13.9
    [Theory]
    [InlineData(BuildingPlacementReasonCodes.OutOfBounds)]
    [InlineData(BuildingPlacementReasonCodes.Blocked)]
    [InlineData(BuildingPlacementReasonCodes.Occupied)]
    public void ShouldRejectPlacement_WhenAnyTargetCellIsOutOfBoundsBlockedOrOccupied(string expectedReason)
    {
        var service = new BuildingPlacementService();
        var state = new BuildingPlacementState(width: 6, height: 6, resources: 300);
        var origin = new GridPoint(1, 1);
        var preview = service.Preview(BuildingTypeIds.Barracks, origin);

        if (expectedReason == BuildingPlacementReasonCodes.OutOfBounds)
        {
            origin = new GridPoint(5, 5);
        }
        else if (expectedReason == BuildingPlacementReasonCodes.Blocked)
        {
            state.Grid.Blocked.Add(preview.Cells[1]);
        }
        else
        {
            state.Grid.Occupied.Add(preview.Cells[1]);
        }

        var resourcesBefore = state.Resources;
        var occupiedBefore = new HashSet<GridPoint>(state.Grid.Occupied);

        var result = service.TryPlace(BuildingTypeIds.Barracks, origin, state);

        result.IsAccepted.Should().BeFalse();
        result.Reason.Should().Be(expectedReason);
        state.Resources.Should().Be(resourcesBefore);
        state.Grid.Occupied.Should().BeEquivalentTo(occupiedBefore);
        state.Placements.Should().BeEmpty();
    }

    // ACC:T13.8
    [Fact]
    public void ShouldCommitOnlyInBoundsGridCells_WhenPlacementSucceeds()
    {
        var service = new BuildingPlacementService();
        var state = new BuildingPlacementState(width: 10, height: 10, resources: 500);

        var result = service.TryPlace(BuildingTypeIds.Castle, new GridPoint(2, 2), state);

        result.IsAccepted.Should().BeTrue();
        result.CommittedCells.Should().HaveCount(4);
        result.CommittedCells.Should().OnlyContain(cell =>
            cell.X >= 0 &&
            cell.Y >= 0 &&
            cell.X < state.Grid.Width &&
            cell.Y < state.Grid.Height);
        state.Grid.Occupied.Should().BeEquivalentTo(result.CommittedCells);
    }

    // ACC:T13.9
    [Fact]
    public void ShouldRejectPlacement_WhenResourcesAreBelowBuildingCost()
    {
        var service = new BuildingPlacementService();
        var castle = BuildingCatalog.GetAll()[BuildingTypeIds.Castle];
        var state = new BuildingPlacementState(width: 8, height: 8, resources: castle.Cost - 1);
        var occupiedBefore = new HashSet<GridPoint>(state.Grid.Occupied);

        var result = service.TryPlace(BuildingTypeIds.Castle, new GridPoint(1, 1), state);

        result.IsAccepted.Should().BeFalse();
        result.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);
        state.Resources.Should().Be(castle.Cost - 1);
        state.Grid.Occupied.Should().BeEquivalentTo(occupiedBefore);
        state.Placements.Should().BeEmpty();
    }

    // ACC:T13.15
    [Theory]
    [InlineData(BuildingTypeIds.Castle, 4)]
    [InlineData(BuildingTypeIds.Barracks, 2)]
    public void ShouldPreviewAllCoveredCells_WhenBuildingHasMultiTileFootprint(string buildingType, int expectedCellCount)
    {
        var service = new BuildingPlacementService();
        var preview = service.Preview(buildingType, new GridPoint(2, 2));

        preview.BuildingType.Should().Be(buildingType);
        preview.Cells.Should().HaveCount(expectedCellCount);
        preview.Cells.Should().OnlyHaveUniqueItems();
    }

    // ACC:T13.17
    [Fact]
    public void ShouldRejectPlacementAtomically_WhenAnyRequiredFootprintCellIsInvalid()
    {
        var service = new BuildingPlacementService();
        var state = new BuildingPlacementState(width: 10, height: 10, resources: 600);
        var preview = service.Preview(BuildingTypeIds.Castle, new GridPoint(3, 3));
        state.Grid.Blocked.Add(preview.Cells[2]);
        state.Grid.Occupied.Add(new GridPoint(0, 0));
        var resourcesBefore = state.Resources;
        var occupiedBefore = new HashSet<GridPoint>(state.Grid.Occupied);

        var result = service.TryPlace(BuildingTypeIds.Castle, new GridPoint(3, 3), state);

        result.IsAccepted.Should().BeFalse();
        result.Reason.Should().Be(BuildingPlacementReasonCodes.Blocked);
        state.Resources.Should().Be(resourcesBefore);
        state.Grid.Occupied.Should().BeEquivalentTo(occupiedBefore);
        state.Placements.Should().BeEmpty();
    }

    // ACC:T13.18
    [Fact]
    public void ShouldReturnDeterministicReason_WhenRequestContainsOutOfBoundsAndBlockedCells()
    {
        var service = new BuildingPlacementService();
        var first = new BuildingPlacementState(width: 5, height: 5, resources: 400);
        var second = new BuildingPlacementState(width: 5, height: 5, resources: 400);
        first.Grid.Blocked.Add(new GridPoint(4, 4));
        second.Grid.Blocked.Add(new GridPoint(4, 4));

        var firstResult = service.TryPlace(BuildingTypeIds.Castle, new GridPoint(4, 4), first);
        var secondResult = service.TryPlace(BuildingTypeIds.Castle, new GridPoint(4, 4), second);

        firstResult.Reason.Should().Be(BuildingPlacementReasonCodes.OutOfBounds);
        secondResult.Reason.Should().Be(BuildingPlacementReasonCodes.OutOfBounds);
        firstResult.Should().BeEquivalentTo(secondResult);
    }
}
