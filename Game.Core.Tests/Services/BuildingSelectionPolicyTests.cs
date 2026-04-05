using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class BuildingSelectionPolicyTests
{
    // ACC:T13.16
    [Fact]
    public void ShouldRejectPlacement_WhenSelectedTypeFootprintTouchesBlockedCell()
    {
        var blocked = new List<GridPoint> { new GridPoint(10, 10) };
        foreach (var buildingType in BuildingCatalog.GetAll().Keys)
        {
            var policy = new BuildingSelectionPolicy();
            var result = policy.Validate(buildingType, new GridPoint(10, 10), availableResources: 999, blocked);

            result.IsPlacementAllowed.Should().BeFalse();
            result.Reason.Should().Be(BuildingPlacementReasonCodes.InvalidFootprint);
        }
    }

    [Fact]
    public void ShouldRejectSelection_WhenResourcesAreBelowSelectedTypeCost()
    {
        foreach (var buildingType in BuildingCatalog.GetAll().Keys)
        {
            var policy = new BuildingSelectionPolicy();
            var cost = BuildingCatalog.GetAll()[buildingType].Cost;
            var result = policy.Validate(buildingType, new GridPoint(0, 0), cost - 1, blockedCells: []);

            result.IsCostAllowed.Should().BeFalse();
            result.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);
        }
    }

    [Fact]
    public void ShouldRejectSelection_WhenResourcesMatchDifferentTypeButNotSelectedTypeCost()
    {
        var policy = new BuildingSelectionPolicy();
        var result = policy.Validate(BuildingTypeIds.Castle, new GridPoint(0, 0), availableResources: 40, blockedCells: []);

        result.IsCostAllowed.Should().BeFalse();
        result.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);
    }

    [Fact]
    public void ShouldAllowSelection_WhenSelectedTypeFootprintIsClearAndResourcesMeetSelectedTypeCost()
    {
        var policy = new BuildingSelectionPolicy();
        var trap = BuildingCatalog.GetAll()[BuildingTypeIds.MineTrap];
        var result = policy.Validate(BuildingTypeIds.MineTrap, new GridPoint(3, 3), trap.Cost, blockedCells: []);

        result.IsPlacementAllowed.Should().BeTrue();
        result.IsCostAllowed.Should().BeTrue();
        result.Reason.Should().Be(BuildingPlacementReasonCodes.Accepted);
    }
}
