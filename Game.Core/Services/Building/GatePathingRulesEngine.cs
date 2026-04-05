using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Building;

namespace Game.Core.Services.Building;

public sealed class GatePathingRulesEngine
{
    public GateMoveResult TryMove(PathingGrid grid, GridPoint origin, MoveDirection direction)
    {
        var target = origin + ToDelta(direction);
        if (!grid.IsInBounds(target))
        {
            return new GateMoveResult(false, origin, "OutOfBounds");
        }

        if (!grid.IsWall(target))
        {
            return new GateMoveResult(true, target, "Open");
        }

        if (!grid.TryGetGateAllowedEntry(target, out var allowed))
        {
            return new GateMoveResult(false, origin, "WallBlocked");
        }

        if (allowed != direction)
        {
            return new GateMoveResult(false, origin, "GateDirectionBlocked");
        }

        return new GateMoveResult(true, target, "GateAllowed");
    }

    public GateMoveResult TryMoveWithFallback(
        PathingGrid grid,
        GridPoint origin,
        MoveDirection primaryDirection,
        IReadOnlyList<MoveDirection> fallbackDirections)
    {
        var primary = TryMove(grid, origin, primaryDirection);
        if (primary.IsAllowed)
        {
            return primary;
        }

        var candidates = fallbackDirections
            .Select(direction => TryMove(grid, origin, direction))
            .Where(result => result.IsAllowed)
            .OrderBy(result => result.NewCell.X)
            .ThenBy(result => result.NewCell.Y)
            .ToArray();

        if (candidates.Length == 0)
        {
            return new GateMoveResult(false, origin, "NoPath");
        }

        return candidates[0];
    }

    private static GridPoint ToDelta(MoveDirection direction)
    {
        return direction switch
        {
            MoveDirection.Up => new GridPoint(0, -1),
            MoveDirection.Right => new GridPoint(1, 0),
            MoveDirection.Down => new GridPoint(0, 1),
            MoveDirection.Left => new GridPoint(-1, 0),
            _ => new GridPoint(0, 0),
        };
    }
}

public enum MoveDirection
{
    Up,
    Right,
    Down,
    Left,
}

public sealed class PathingGrid
{
    private readonly HashSet<GridPoint> walls = [];
    private readonly Dictionary<GridPoint, MoveDirection> gateEntryDirections = [];

    private PathingGrid(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    public static PathingGrid Create(int width, int height)
    {
        return new PathingGrid(width, height);
    }

    public PathingGrid WithWall(GridPoint cell)
    {
        walls.Add(cell);
        return this;
    }

    public PathingGrid WithGate(GridPoint cell, MoveDirection allowedEntryDirection)
    {
        walls.Add(cell);
        gateEntryDirections[cell] = allowedEntryDirection;
        return this;
    }

    public bool IsInBounds(GridPoint cell)
    {
        return cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;
    }

    public bool IsWall(GridPoint cell)
    {
        return walls.Contains(cell);
    }

    public bool TryGetGateAllowedEntry(GridPoint cell, out MoveDirection allowedEntryDirection)
    {
        return gateEntryDirections.TryGetValue(cell, out allowedEntryDirection);
    }
}

public readonly record struct GateMoveResult(
    bool IsAllowed,
    GridPoint NewCell,
    string Reason);
