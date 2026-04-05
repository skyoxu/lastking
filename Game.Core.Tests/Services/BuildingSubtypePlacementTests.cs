using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BuildingSubtypePlacementTests
{
    // ACC:T13.19
    [Fact]
    public void ShouldRejectSubtypePlacement_WhenAnySubtypeFootprintCellIsBlocked()
    {
        foreach (var subtype in BuildingCatalog.GetAll().Keys)
        {
            var service = new BuildingSubtypePlacementService();
            var state = new BuildingPlacementState(width: 12, height: 12, resources: 500);
            var placementService = new BuildingPlacementService();
            var preview = placementService.Preview(subtype, new GridPoint(2, 2));
            state.Grid.Blocked.Add(preview.Cells[^1]);
            var resourcesBefore = state.Resources;
            var occupiedBefore = new HashSet<GridPoint>(state.Grid.Occupied);

            var result = service.TryPlace(subtype, new GridPoint(2, 2), state);

            result.IsAccepted.Should().BeFalse();
            result.Reason.Should().Be(BuildingPlacementReasonCodes.Blocked);
            state.Resources.Should().Be(resourcesBefore);
            state.Grid.Occupied.Should().BeEquivalentTo(occupiedBefore);
            state.Placements.Should().BeEmpty();
        }
    }

    // ACC:T13.3
    [Fact]
    public void ShouldRejectSubtypePlacement_WhenResourcesAreBelowSubtypeCost()
    {
        foreach (var subtype in BuildingCatalog.GetAll().Keys)
        {
            var service = new BuildingSubtypePlacementService();
            var cost = BuildingCatalog.GetAll()[subtype].Cost;
            var state = new BuildingPlacementState(width: 12, height: 12, resources: cost - 1);
            var occupiedBefore = new HashSet<GridPoint>(state.Grid.Occupied);

            var result = service.TryPlace(subtype, new GridPoint(1, 1), state);

            result.IsAccepted.Should().BeFalse();
            result.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);
            state.Resources.Should().Be(cost - 1);
            state.Grid.Occupied.Should().BeEquivalentTo(occupiedBefore);
            state.Placements.Should().BeEmpty();
        }
    }

    [Fact]
    public void ShouldAcceptSubtypePlacementAndCommitSubtypeFootprint_WhenFootprintIsClear()
    {
        foreach (var subtype in BuildingCatalog.GetAll().Keys)
        {
            var service = new BuildingSubtypePlacementService();
            var state = new BuildingPlacementState(width: 12, height: 12, resources: 1000);
            var placementService = new BuildingPlacementService();
            var expected = placementService.Preview(subtype, new GridPoint(3, 3)).Cells;

            var result = service.TryPlace(subtype, new GridPoint(3, 3), state);

            result.IsAccepted.Should().BeTrue();
            result.Reason.Should().Be(BuildingPlacementReasonCodes.Accepted);
            result.CommittedCells.Should().BeEquivalentTo(expected);
            state.Grid.Occupied.Should().BeEquivalentTo(expected);
            state.Placements.Should().ContainSingle();
        }
    }
}
