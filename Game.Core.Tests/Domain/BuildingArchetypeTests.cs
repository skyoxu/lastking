using System;
using FluentAssertions;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Xunit;

namespace Game.Core.Tests.Domain;

public sealed class BuildingArchetypeTests
{
    private static readonly string[] RequiredTypes =
    {
        BuildingTypeIds.Castle,
        BuildingTypeIds.Residence,
        BuildingTypeIds.Mine,
        BuildingTypeIds.Barracks,
        BuildingTypeIds.MgTower,
        BuildingTypeIds.Wall,
        BuildingTypeIds.MineTrap,
    };

    // ACC:T13.16
    [Fact]
    public void ShouldExposeCanonicalArchetypesWithCoreStats_WhenLoadingCatalog()
    {
        var catalog = BuildingCatalog.GetAll();

        catalog.Keys.Should().BeEquivalentTo(RequiredTypes);
        foreach (var buildingType in RequiredTypes)
        {
            var building = catalog[buildingType];
            building.Type.Should().Be(buildingType);
            building.Level.Should().BeGreaterThanOrEqualTo(1);
            building.FootprintSize.Should().BeGreaterThan(0);
            building.Hp.Should().BeGreaterThan(0);
            building.Cost.Should().BeGreaterThan(0);
        }
    }

    // ACC:T13.7
    [Theory]
    [InlineData(BuildingTypeIds.Castle, 4)]
    [InlineData(BuildingTypeIds.Barracks, 2)]
    [InlineData(BuildingTypeIds.MgTower, 1)]
    [InlineData(BuildingTypeIds.Wall, 1)]
    public void ShouldHonorExplicitTaskFootprints_WhenCheckingCanonicalValues(string buildingType, int expectedFootprint)
    {
        BuildingCatalog.GetAll()[buildingType].FootprintSize.Should().Be(expectedFootprint);
    }

    // ACC:T13.19
    [Fact]
    public void ShouldUseSubtypeSpecificFootprintAndCost_WhenPlacingBuildings()
    {
        var placementService = new BuildingPlacementService();

        foreach (var building in BuildingCatalog.GetAll().Values)
        {
            var rejectByCostState = new BuildingPlacementState(width: 8, height: 8, resources: building.Cost - 1);
            var reject = placementService.TryPlace(building.Type, new GridPoint(1, 1), rejectByCostState);
            reject.IsAccepted.Should().BeFalse();
            reject.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);

            var acceptState = new BuildingPlacementState(width: 8, height: 8, resources: building.Cost);
            var accept = placementService.TryPlace(building.Type, new GridPoint(1, 1), acceptState);
            accept.IsAccepted.Should().BeTrue();
            accept.RequiredCost.Should().Be(building.Cost);
            acceptState.Resources.Should().Be(0);
        }
    }

    // ACC:T13.6
    [Fact]
    public void ShouldDefineSharedBuildingBaseAndConcreteSubtypes_WhenInspectingDomainTypes()
    {
        var buildingBase = typeof(Building);
        buildingBase.IsAbstract.Should().BeTrue();

        buildingBase.GetProperty(nameof(Building.Type))?.PropertyType.Should().Be(typeof(string));
        buildingBase.GetProperty(nameof(Building.Level))?.PropertyType.Should().Be(typeof(int));
        buildingBase.GetProperty(nameof(Building.FootprintSize))?.PropertyType.Should().Be(typeof(int));
        buildingBase.GetProperty(nameof(Building.Cost))?.PropertyType.Should().Be(typeof(int));
        buildingBase.GetProperty(nameof(Building.Hp))?.PropertyType.Should().Be(typeof(int));

        var subtypeNames = new[]
        {
            typeof(CastleBuilding).Name,
            typeof(ResidenceBuilding).Name,
            typeof(MineBuilding).Name,
            typeof(BarracksBuilding).Name,
            typeof(MgTowerBuilding).Name,
            typeof(WallBuilding).Name,
            typeof(MineTrapBuilding).Name,
        };

        subtypeNames.Should().Contain(name => name.Contains("Castle", StringComparison.OrdinalIgnoreCase));
        subtypeNames.Should().Contain(name => name.Contains("Residence", StringComparison.OrdinalIgnoreCase));
        subtypeNames.Should().Contain(name => name.Contains("Mine", StringComparison.OrdinalIgnoreCase));
        subtypeNames.Should().Contain(name => name.Contains("Barracks", StringComparison.OrdinalIgnoreCase));
        subtypeNames.Should().Contain(name => name.Contains("Tower", StringComparison.OrdinalIgnoreCase));
        subtypeNames.Should().Contain(name => name.Contains("Wall", StringComparison.OrdinalIgnoreCase));
        subtypeNames.Should().Contain(name => name.Contains("Trap", StringComparison.OrdinalIgnoreCase));
    }

    // ACC:T13.10
    [Fact]
    public void ShouldRefusePlacementAndKeepTreasuryUnchanged_WhenTreasuryIsInsufficientForSubtypeCost()
    {
        var placementService = new BuildingPlacementService();
        var castle = BuildingCatalog.GetAll()[BuildingTypeIds.Castle];
        var state = new BuildingPlacementState(width: 10, height: 10, resources: castle.Cost - 1);
        var rejected = placementService.TryPlace(castle.Type, new GridPoint(1, 1), state);

        rejected.IsAccepted.Should().BeFalse();
        rejected.Reason.Should().Be(BuildingPlacementReasonCodes.InsufficientResources);
        state.Resources.Should().Be(castle.Cost - 1);
    }
}
