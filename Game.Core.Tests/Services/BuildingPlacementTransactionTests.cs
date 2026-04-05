using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BuildingPlacementTransactionTests
{
    // ACC:T13.9
    [Theory]
    [InlineData(BuildingPlacementReasonCodes.OutOfBounds)]
    [InlineData(BuildingPlacementReasonCodes.Blocked)]
    [InlineData(BuildingPlacementReasonCodes.Occupied)]
    public void ShouldRejectPlacement_WhenAnyTargetCellIsOutOfBoundsBlockedOrOccupied(string expectedReason)
    {
        var service = new BuildingPlacementService();
        var state = new BuildingPlacementState(width: 6, height: 6, resources: 200);
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

    // ACC:T13.9
    [Fact]
    public void ShouldRejectPlacement_WhenResourcesAreBelowBuildingCost()
    {
        var service = new BuildingPlacementService();
        var barracks = BuildingCatalog.GetAll()[BuildingTypeIds.Barracks];
        var state = new BuildingPlacementState(width: 8, height: 8, resources: barracks.Cost - 1);
        var result = service.TryPlace(BuildingTypeIds.Barracks, new GridPoint(1, 1), state);

        result.IsAccepted.Should().BeFalse();
        result.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);
        state.Resources.Should().Be(barracks.Cost - 1);
        state.Placements.Should().BeEmpty();
    }

    // ACC:T13.10
    [Fact]
    public void ShouldDeductResourcesExactlyOnceAndCommitExactFootprint_WhenPlacementSucceeds()
    {
        var service = new BuildingPlacementService();
        var wall = BuildingCatalog.GetAll()[BuildingTypeIds.Wall];
        var state = new BuildingPlacementState(width: 10, height: 10, resources: 100);

        var result = service.TryPlace(BuildingTypeIds.Wall, new GridPoint(3, 3), state);

        result.IsAccepted.Should().BeTrue();
        state.Resources.Should().Be(100 - wall.Cost);
        result.CommittedCells.Should().BeEquivalentTo(new[] { new GridPoint(3, 3) });
        state.Grid.Occupied.Should().BeEquivalentTo(result.CommittedCells);
        state.Placements.Should().ContainSingle();
    }

    // ACC:T13.11
    [Fact]
    public void ShouldAcceptCoreBuildingSubtypes_WhenValidatedAndTransacted()
    {
        var service = new BuildingPlacementService();

        foreach (var buildingType in BuildingCatalog.GetAll().Keys)
        {
            var state = new BuildingPlacementState(width: 12, height: 12, resources: 1000);
            var result = service.TryPlace(buildingType, new GridPoint(2, 2), state);

            result.IsAccepted.Should().BeTrue();
            state.Placements.Should().ContainSingle();
            state.Placements[0].BuildingType.Should().Be(buildingType);
        }
    }

    // ACC:T13.17
    [Fact]
    public void ShouldRejectAtomically_WhenAnyRequiredFootprintCellIsInvalid()
    {
        var service = new BuildingPlacementService();
        var state = new BuildingPlacementState(width: 10, height: 10, resources: 500);
        var preview = service.Preview(BuildingTypeIds.Castle, new GridPoint(2, 2));
        state.Grid.Blocked.Add(preview.Cells[3]);
        state.Grid.Occupied.Add(new GridPoint(8, 8));
        var resourcesBefore = state.Resources;
        var occupiedBefore = new HashSet<GridPoint>(state.Grid.Occupied);

        var result = service.TryPlace(BuildingTypeIds.Castle, new GridPoint(2, 2), state);

        result.IsAccepted.Should().BeFalse();
        result.Reason.Should().Be(BuildingPlacementReasonCodes.Blocked);
        state.Resources.Should().Be(resourcesBefore);
        state.Grid.Occupied.Should().BeEquivalentTo(occupiedBefore);
        state.Placements.Should().BeEmpty();
    }

    // ACC:T13.18
    [Fact]
    public void ShouldReturnDeterministicResult_WhenApplyingSameRequestOnFreshState()
    {
        var service = new BuildingPlacementService();
        var first = new BuildingPlacementState(width: 10, height: 10, resources: 300);
        var second = new BuildingPlacementState(width: 10, height: 10, resources: 300);

        var firstResult = service.TryPlace(BuildingTypeIds.Barracks, new GridPoint(2, 2), first);
        var secondResult = service.TryPlace(BuildingTypeIds.Barracks, new GridPoint(2, 2), second);

        firstResult.Should().BeEquivalentTo(secondResult);
        first.Resources.Should().Be(second.Resources);
        first.Grid.Occupied.Should().BeEquivalentTo(second.Grid.Occupied);
        first.Placements.Should().BeEquivalentTo(second.Placements);
    }
}
