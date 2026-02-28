namespace Game.Core.Contracts.Lastking;

/// <summary>
/// DTO for deterministic wave budget calculation output.
/// </summary>
/// <remarks>
/// Overlay ref: docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// </remarks>
public sealed record WaveBudgetDto(
    int DayNumber,
    int NightNumber,
    int NormalBudget,
    int EliteBudget,
    int BossBudget,
    System.DateTimeOffset ComputedAt
);
