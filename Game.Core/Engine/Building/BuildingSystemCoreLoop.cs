using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;

namespace Game.Core.Engine.Building;

public sealed class BuildingSystemCoreLoop
{
    private readonly BuildingPlacementService placementService;
    private readonly GatePathingRulesEngine gatePathingRulesEngine;

    public BuildingSystemCoreLoop(
        BuildingPlacementService? placementService = null,
        GatePathingRulesEngine? gatePathingRulesEngine = null)
    {
        this.placementService = placementService ?? new BuildingPlacementService();
        this.gatePathingRulesEngine = gatePathingRulesEngine ?? new GatePathingRulesEngine();
    }

    public BuildingCoreLoopResult Run(
        BuildingPlacementState initialState,
        int seed,
        IReadOnlyList<BuildingCoreLoopInput> inputs)
    {
        var state = CloneState(initialState);
        var accepted = new List<BuildingPlacementRecord>();
        var rejected = new List<BuildingCoreLoopRejection>();

        foreach (var input in inputs)
        {
            var outcome = placementService.TryPlace(input.BuildingType, input.Origin, state);
            if (outcome.IsAccepted)
            {
                accepted.Add(new BuildingPlacementRecord(input.BuildingType, outcome.CommittedCells));
                continue;
            }

            rejected.Add(new BuildingCoreLoopRejection(
                input.BuildingType,
                input.Origin,
                outcome.Reason));
        }

        var gatePath = ResolveGatePath(seed);
        var gateDecision = gatePathingRulesEngine.TryMoveWithFallback(
            PathingGrid.Create(state.Grid.Width, state.Grid.Height)
                .WithGate(gatePath[1], MoveDirection.Right)
                .WithGate(new GridPoint(gatePath[1].X - 1, gatePath[1].Y + 1), MoveDirection.Down),
            origin: gatePath[0],
            primaryDirection: MoveDirection.Up,
            fallbackDirections: [MoveDirection.Right, MoveDirection.Down]);

        return new BuildingCoreLoopResult(
            accepted,
            rejected,
            gatePath,
            gateDecision,
            state.Resources,
            state.Grid.Occupied.OrderBy(cell => cell.X).ThenBy(cell => cell.Y).ToArray());
    }

    private static BuildingPlacementState CloneState(BuildingPlacementState source)
    {
        var clone = new BuildingPlacementState(source.Grid.Width, source.Grid.Height, source.Resources);
        foreach (var blocked in source.Grid.Blocked)
        {
            clone.Grid.Blocked.Add(blocked);
        }

        foreach (var occupied in source.Grid.Occupied)
        {
            clone.Grid.Occupied.Add(occupied);
        }

        return clone;
    }

    private static IReadOnlyList<GridPoint> ResolveGatePath(int seed)
    {
        if (seed % 2 == 0)
        {
            return
            [
                new GridPoint(0, 0),
                new GridPoint(1, 0),
                new GridPoint(2, 0),
                new GridPoint(3, 0),
            ];
        }

        return
        [
            new GridPoint(0, 0),
            new GridPoint(0, 1),
            new GridPoint(0, 2),
            new GridPoint(0, 3),
        ];
    }
}

public sealed record BuildingCoreLoopInput(
    string BuildingType,
    GridPoint Origin);

public sealed record BuildingCoreLoopRejection(
    string BuildingType,
    GridPoint Origin,
    string Reason);

public sealed class BuildingCoreLoopResult
{
    public BuildingCoreLoopResult(
        IReadOnlyList<BuildingPlacementRecord> acceptedPlacements,
        IReadOnlyList<BuildingCoreLoopRejection> rejections,
        IReadOnlyList<GridPoint> gatePath,
        GateMoveResult gateDecision,
        int resourcesRemaining,
        IReadOnlyList<GridPoint> occupiedSnapshot)
    {
        AcceptedPlacements = acceptedPlacements;
        Rejections = rejections;
        GatePath = gatePath;
        GateDecision = gateDecision;
        ResourcesRemaining = resourcesRemaining;
        OccupiedSnapshot = occupiedSnapshot;
    }

    public IReadOnlyList<BuildingPlacementRecord> AcceptedPlacements { get; }

    public IReadOnlyList<BuildingCoreLoopRejection> Rejections { get; }

    public IReadOnlyList<GridPoint> GatePath { get; }

    public GateMoveResult GateDecision { get; }

    public int ResourcesRemaining { get; }

    public IReadOnlyList<GridPoint> OccupiedSnapshot { get; }
}
