using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Building;

namespace Game.Core.State.Building;

public sealed class GridOccupancyTracker
{
    public GridOccupancyTracker(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public HashSet<GridPoint> Blocked { get; } = [];

    public HashSet<GridPoint> Occupied { get; } = [];

    public bool IsInBounds(GridPoint cell)
    {
        return cell.X >= 0 &&
               cell.Y >= 0 &&
               cell.X < Width &&
               cell.Y < Height;
    }

    public string? ValidateFootprint(IReadOnlyList<GridPoint> footprint)
    {
        if (footprint.Any(cell => !IsInBounds(cell)))
        {
            return BuildingPlacementReasonCodes.OutOfBounds;
        }

        if (footprint.Any(Blocked.Contains))
        {
            return BuildingPlacementReasonCodes.Blocked;
        }

        if (footprint.Any(Occupied.Contains))
        {
            return BuildingPlacementReasonCodes.Occupied;
        }

        return null;
    }

    public void Commit(IReadOnlyList<GridPoint> footprint)
    {
        foreach (var cell in footprint)
        {
            Occupied.Add(cell);
        }
    }
}
