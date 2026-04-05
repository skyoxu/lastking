using System.Collections.Generic;
using Game.Core.Domain.Building;

namespace Game.Core.Services.Building;

public sealed record BuildingPlacementPreview(
    string BuildingType,
    IReadOnlyList<GridPoint> Cells);

public sealed class BuildingPlacementOutcome
{
    private BuildingPlacementOutcome(
        bool isAccepted,
        string reason,
        IReadOnlyList<GridPoint> evaluatedFootprint,
        IReadOnlyList<GridPoint> committedCells,
        int requiredCost)
    {
        IsAccepted = isAccepted;
        Reason = reason;
        EvaluatedFootprint = evaluatedFootprint;
        CommittedCells = committedCells;
        RequiredCost = requiredCost;
    }

    public bool IsAccepted { get; }

    public string Reason { get; }

    public IReadOnlyList<GridPoint> EvaluatedFootprint { get; }

    public IReadOnlyList<GridPoint> CommittedCells { get; }

    public int RequiredCost { get; }

    public static BuildingPlacementOutcome Accepted(IReadOnlyList<GridPoint> committedCells, int requiredCost)
    {
        return new BuildingPlacementOutcome(
            isAccepted: true,
            reason: BuildingPlacementReasonCodes.Accepted,
            evaluatedFootprint: committedCells,
            committedCells: committedCells,
            requiredCost: requiredCost);
    }

    public static BuildingPlacementOutcome Rejected(string reason, IReadOnlyList<GridPoint> evaluatedFootprint, int requiredCost)
    {
        return new BuildingPlacementOutcome(
            isAccepted: false,
            reason: reason,
            evaluatedFootprint: evaluatedFootprint,
            committedCells: [],
            requiredCost: requiredCost);
    }
}

public sealed record BuildingSelectionResult(
    bool IsPlacementAllowed,
    bool IsCostAllowed,
    string Reason);
