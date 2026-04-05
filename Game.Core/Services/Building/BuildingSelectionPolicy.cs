using System;
using System.Collections.Generic;
using Game.Core.Domain.Building;

namespace Game.Core.Services.Building;

public sealed class BuildingSelectionPolicy
{
    private readonly IReadOnlyDictionary<string, Game.Core.Domain.Building.Building> catalog;

    public BuildingSelectionPolicy(IReadOnlyDictionary<string, Game.Core.Domain.Building.Building>? catalog = null)
    {
        this.catalog = catalog ?? BuildingCatalog.GetAll();
    }

    public BuildingSelectionResult Validate(
        string buildingType,
        GridPoint origin,
        int availableResources,
        IEnumerable<GridPoint> blockedCells)
    {
        ArgumentNullException.ThrowIfNull(blockedCells);

        if (!catalog.TryGetValue(buildingType, out var building))
        {
            return new BuildingSelectionResult(
                IsPlacementAllowed: false,
                IsCostAllowed: false,
                BuildingPlacementReasonCodes.UnknownType);
        }

        var blocked = new HashSet<GridPoint>(blockedCells);
        var footprint = building.ResolveFootprint(origin);

        foreach (var cell in footprint)
        {
            if (blocked.Contains(cell))
            {
                return new BuildingSelectionResult(
                    IsPlacementAllowed: false,
                    IsCostAllowed: true,
                    BuildingPlacementReasonCodes.InvalidFootprint);
            }
        }

        if (availableResources < building.Cost)
        {
            return new BuildingSelectionResult(
                IsPlacementAllowed: true,
                IsCostAllowed: false,
                BuildingPlacementReasonCodes.InsufficientResources);
        }

        return new BuildingSelectionResult(
            IsPlacementAllowed: true,
            IsCostAllowed: true,
            BuildingPlacementReasonCodes.Accepted);
    }
}
