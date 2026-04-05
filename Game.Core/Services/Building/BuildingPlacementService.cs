using System;
using System.Collections.Generic;
using Game.Core.Domain.Building;
using Game.Core.State.Building;

namespace Game.Core.Services.Building;

public sealed class BuildingPlacementService
{
    private readonly IReadOnlyDictionary<string, Game.Core.Domain.Building.Building> catalog;

    public BuildingPlacementService(IReadOnlyDictionary<string, Game.Core.Domain.Building.Building>? catalog = null)
    {
        this.catalog = catalog ?? BuildingCatalog.GetAll();
    }

    public BuildingPlacementPreview Preview(string buildingType, GridPoint origin)
    {
        var building = ResolveBuilding(buildingType);
        return new BuildingPlacementPreview(building.Type, building.ResolveFootprint(origin));
    }

    public BuildingPlacementOutcome TryPlace(string buildingType, GridPoint origin, BuildingPlacementState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var building = ResolveBuilding(buildingType);
        return TryPlace(building, origin, state);
    }

    public BuildingPlacementOutcome TryPlace(Game.Core.Domain.Building.Building building, GridPoint origin, BuildingPlacementState state)
    {
        ArgumentNullException.ThrowIfNull(building);
        ArgumentNullException.ThrowIfNull(state);

        var footprint = building.ResolveFootprint(origin);
        var invalidReason = state.Grid.ValidateFootprint(footprint);
        if (invalidReason is not null)
        {
            return BuildingPlacementOutcome.Rejected(
                invalidReason,
                footprint,
                requiredCost: building.Cost);
        }

        if (state.Resources < building.Cost)
        {
            return BuildingPlacementOutcome.Rejected(
                BuildingPlacementReasonCodes.InsufficientResources,
                footprint,
                requiredCost: building.Cost);
        }

        state.Resources -= building.Cost;
        state.Grid.Commit(footprint);
        state.Placements.Add(new BuildingPlacementRecord(building.Type, footprint));
        return BuildingPlacementOutcome.Accepted(footprint, building.Cost);
    }

    private Game.Core.Domain.Building.Building ResolveBuilding(string buildingType)
    {
        if (!catalog.TryGetValue(buildingType, out var building))
        {
            throw new InvalidOperationException($"Unknown building type: {buildingType}");
        }

        return building;
    }
}
