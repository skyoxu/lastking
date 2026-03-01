namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.time_scale.changed.
/// Emitted when runtime speed mode is switched (pause/1x/2x).
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0021, ADR-0022.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record TimeScaleChanged(
    string RunId,
    int PreviousScalePercent,
    int CurrentScalePercent,
    bool IsPaused,
    System.DateTimeOffset ChangedAt
)
{
    public const string EventType = EventTypes.LastkingTimeScaleChanged;
}
