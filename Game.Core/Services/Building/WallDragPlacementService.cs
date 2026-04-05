using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Building;
using Game.Core.State.Building;

namespace Game.Core.Services.Building;

public sealed class WallDragPlacementService
{
    private readonly int costPerSegment;

    public WallDragPlacementService(int? costPerSegment = null)
    {
        this.costPerSegment = costPerSegment ?? BuildingCatalog.GetAll()[BuildingTypeIds.Wall].Cost;
    }

    public WallDragPlacementResult TryPlaceDragLine(
        IReadOnlyList<GridPoint> dragPath,
        BuildingPlacementState state)
    {
        var createdSegments = new List<WallSegment>();
        var skippedCells = new List<GridPoint>();

        for (var index = 0; index < dragPath.Count; index++)
        {
            var cell = dragPath[index];
            if (createdSegments.Count > 0 && !AreNeighborCells(createdSegments[^1].Cell, cell))
            {
                skippedCells.Add(cell);
                continue;
            }

            if (!state.Grid.IsInBounds(cell) || state.Grid.Blocked.Contains(cell) || state.Grid.Occupied.Contains(cell))
            {
                skippedCells.Add(cell);
                continue;
            }

            if (state.Resources < costPerSegment)
            {
                skippedCells.Add(cell);
                continue;
            }

            state.Resources -= costPerSegment;
            state.Grid.Occupied.Add(cell);
            createdSegments.Add(new WallSegment(index, cell));
        }

        var totalCharged = createdSegments.Count * costPerSegment;
        if (createdSegments.Any())
        {
            state.Placements.Add(new BuildingPlacementRecord(
                BuildingTypeIds.Wall,
                createdSegments.Select(segment => segment.Cell).ToArray()));
        }

        return new WallDragPlacementResult(createdSegments, skippedCells, totalCharged);
    }

    private static bool AreNeighborCells(GridPoint previous, GridPoint current)
    {
        var distance = System.Math.Abs(previous.X - current.X) + System.Math.Abs(previous.Y - current.Y);
        return distance == 1;
    }
}

public sealed record WallSegment(int StepIndex, GridPoint Cell);

public sealed class WallDragPlacementResult
{
    public WallDragPlacementResult(
        IReadOnlyList<WallSegment> createdSegments,
        IReadOnlyList<GridPoint> skippedCells,
        int totalCharged)
    {
        CreatedSegments = createdSegments;
        SkippedCells = skippedCells;
        TotalCharged = totalCharged;
    }

    public IReadOnlyList<WallSegment> CreatedSegments { get; }

    public IReadOnlyList<GridPoint> SkippedCells { get; }

    public int TotalCharged { get; }
}
