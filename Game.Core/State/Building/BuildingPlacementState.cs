using System.Collections.Generic;
using Game.Core.Domain.Building;

namespace Game.Core.State.Building;

public sealed class BuildingPlacementState
{
    public BuildingPlacementState(int width, int height, int resources)
    {
        Resources = resources;
        Grid = new GridOccupancyTracker(width, height);
    }

    public int Resources { get; set; }

    public GridOccupancyTracker Grid { get; }

    public List<BuildingPlacementRecord> Placements { get; } = [];
}

public sealed record BuildingPlacementRecord(
    string BuildingType,
    IReadOnlyList<GridPoint> CommittedCells);
