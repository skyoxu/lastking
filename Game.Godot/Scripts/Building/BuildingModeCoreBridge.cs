using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Godot;
using Godot.Collections;
using GArray = Godot.Collections.Array;
using GDictionary = Godot.Collections.Dictionary;

namespace Game.Godot.Scripts.Building;

public partial class BuildingModeCoreBridge : Node
{
    private readonly BuildingPlacementService placementService = new();
    private readonly WallDragPlacementService wallDragPlacementService = new();
    private readonly GatePathingRulesEngine gatePathingRulesEngine = new();
    private readonly IReadOnlyDictionary<string, Game.Core.Domain.Building.Building> catalog = BuildingCatalog.GetAll();
    private BuildingPlacementState state = new(width: 10, height: 10, resources: 200);

    public void ResetState(int width = 10, int height = 10, int resources = 200)
    {
        state = new BuildingPlacementState(width, height, resources);
    }

    public int GetResources()
    {
        return state.Resources;
    }

    public void SetResources(int resources)
    {
        state.Resources = Math.Max(0, resources);
    }

    public int GetPlacementCount()
    {
        return state.Placements.Count;
    }

    public GArray GetPlacementsSnapshot()
    {
        var snapshot = new GArray();
        foreach (var placement in state.Placements)
        {
            var coveredCells = new GArray();
            foreach (var cell in placement.CommittedCells)
            {
                coveredCells.Add(ToVector(cell));
            }

            var origin = placement.CommittedCells.Count > 0
                ? ToVector(placement.CommittedCells[0])
                : Vector2I.Zero;
            snapshot.Add(new GDictionary
            {
                ["type"] = placement.BuildingType,
                ["origin"] = origin,
                ["covered_cells"] = coveredCells,
            });
        }

        return snapshot;
    }

    public GArray ListBuildingTypes()
    {
        var names = catalog.Keys.OrderBy(static key => key, StringComparer.Ordinal).ToArray();
        var result = new GArray();
        foreach (var name in names)
        {
            result.Add(name);
        }

        return result;
    }

    public bool HasBuildingType(string buildingType)
    {
        return catalog.ContainsKey(buildingType);
    }

    public int GetCost(string buildingType)
    {
        return catalog.TryGetValue(buildingType, out var building)
            ? building.Cost
            : 0;
    }

    public int GetFootprintSize(string buildingType)
    {
        return catalog.TryGetValue(buildingType, out var building)
            ? building.FootprintSize
            : 0;
    }

    public GArray GetFootprintOffsets(string buildingType)
    {
        var offsets = new GArray();
        if (!catalog.TryGetValue(buildingType, out var building))
        {
            return offsets;
        }

        foreach (var offset in building.FootprintOffsets)
        {
            offsets.Add(ToVector(offset));
        }

        return offsets;
    }

    public GDictionary Preview(string buildingType, Vector2I originCell)
    {
        if (!catalog.ContainsKey(buildingType))
        {
            return new GDictionary
            {
                ["is_valid"] = false,
                ["covered_cells"] = new GArray(),
            };
        }

        var preview = placementService.Preview(buildingType, ToPoint(originCell));
        var coveredCells = ToVectorArray(preview.Cells);
        var reason = state.Grid.ValidateFootprint(preview.Cells);
        var isValid = reason is null && state.Resources >= catalog[buildingType].Cost;

        return new GDictionary
        {
            ["is_valid"] = isValid,
            ["covered_cells"] = coveredCells,
            ["reject_reason"] = reason ?? string.Empty,
        };
    }

    public GDictionary Confirm(string buildingType, Vector2I originCell)
    {
        var resourcesBefore = state.Resources;
        var placementsBefore = state.Placements.Count;

        if (!catalog.ContainsKey(buildingType))
        {
            return Rejected(resourcesBefore, placementsBefore);
        }

        var outcome = placementService.TryPlace(buildingType, ToPoint(originCell), state);
        if (!outcome.IsAccepted)
        {
            return Rejected(resourcesBefore, placementsBefore);
        }

        var last = state.Placements[^1];
        return new GDictionary
        {
            ["accepted"] = true,
            ["resources_before"] = resourcesBefore,
            ["resources_after"] = state.Resources,
            ["placements_before"] = placementsBefore,
            ["placements_after"] = state.Placements.Count,
            ["placed"] = new GDictionary
            {
                ["type"] = last.BuildingType,
                ["origin"] = originCell,
                ["covered_cells"] = ToVectorArray(last.CommittedCells),
            },
        };
    }

    public void SetBlocked(Vector2I cell)
    {
        state.Grid.Blocked.Add(ToPoint(cell));
    }

    public void SetOccupied(Vector2I cell)
    {
        state.Grid.Occupied.Add(ToPoint(cell));
    }

    public GDictionary DragPlaceWall(GArray path)
    {
        var dragPath = path.Select(item => ToPoint((Vector2I)item)).ToArray();
        var result = wallDragPlacementService.TryPlaceDragLine(dragPath, state);
        var createdSegments = new GArray();
        foreach (var segment in result.CreatedSegments)
        {
            createdSegments.Add(ToVector(segment.Cell));
        }

        var skippedCells = new GArray();
        foreach (var cell in result.SkippedCells)
        {
            skippedCells.Add(ToVector(cell));
        }

        return new GDictionary
        {
            ["created_segments"] = createdSegments,
            ["skipped_cells"] = skippedCells,
            ["resources_spent"] = result.TotalCharged,
        };
    }

    public string ResolveGateFallback(int seed, GArray forcedGateOrder)
    {
        if (seed < 0)
        {
            return "InvalidSeed";
        }

        var fallbackDirections = new List<MoveDirection>();
        foreach (var raw in forcedGateOrder)
        {
            var name = raw.AsString();
            if (string.Equals(name, "GateEast", StringComparison.Ordinal))
            {
                fallbackDirections.Add(MoveDirection.Right);
            }
            else if (string.Equals(name, "GateNorth", StringComparison.Ordinal))
            {
                fallbackDirections.Add(MoveDirection.Down);
            }
        }

        if (fallbackDirections.Count == 0)
        {
            fallbackDirections.Add(MoveDirection.Right);
            fallbackDirections.Add(MoveDirection.Down);
        }

        var grid = PathingGrid.Create(8, 8)
            .WithWall(new GridPoint(4, 3))
            .WithGate(new GridPoint(3, 4), MoveDirection.Down)
            .WithGate(new GridPoint(4, 4), MoveDirection.Right);
        var result = gatePathingRulesEngine.TryMoveWithFallback(
            grid,
            origin: new GridPoint(3, 3),
            primaryDirection: MoveDirection.Up,
            fallbackDirections);

        if (!result.IsAllowed)
        {
            return "NoPath";
        }

        if (result.NewCell == new GridPoint(4, 4))
        {
            return "GateEast";
        }

        if (result.NewCell == new GridPoint(3, 4))
        {
            return "GateNorth";
        }

        return $"{result.NewCell.X},{result.NewCell.Y}";
    }

    private GDictionary Rejected(int resourcesBefore, int placementsBefore)
    {
        return new GDictionary
        {
            ["accepted"] = false,
            ["resources_before"] = resourcesBefore,
            ["resources_after"] = state.Resources,
            ["placements_before"] = placementsBefore,
            ["placements_after"] = state.Placements.Count,
        };
    }

    private static GridPoint ToPoint(Vector2I cell)
    {
        return new GridPoint(cell.X, cell.Y);
    }

    private static Vector2I ToVector(GridPoint cell)
    {
        return new Vector2I(cell.X, cell.Y);
    }

    private static GArray ToVectorArray(IReadOnlyList<GridPoint> cells)
    {
        var result = new GArray();
        foreach (var cell in cells)
        {
            result.Add(ToVector(cell));
        }

        return result;
    }
}
