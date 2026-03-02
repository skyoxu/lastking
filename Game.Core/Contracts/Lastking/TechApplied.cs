namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.tech.applied.
/// Emitted when one technology modifier is committed to runtime.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0021.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record TechApplied(
    string RunId,
    string TechId,
    string StatKey,
    int PreviousValue,
    int CurrentValue,
    System.DateTimeOffset AppliedAt
)
{
    public const string EventType = EventTypes.LastkingTechApplied;
}
