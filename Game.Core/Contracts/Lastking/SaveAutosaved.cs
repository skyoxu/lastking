namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.save.autosaved.
/// Emitted after day-boundary autosave has been persisted.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0006, ADR-0008.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record SaveAutosaved(
    string RunId,
    int DayNumber,
    string SlotId,
    string ConfigHash,
    System.DateTimeOffset SavedAt
)
{
    public const string EventType = EventTypes.LastkingSaveAutosaved;
}
