namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.night.started.
/// Emitted when day phase transitions into night defense phase.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0018, ADR-0021, ADR-0022.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record NightStarted(
    string RunId,
    int DayNumber,
    int NightNumber,
    System.DateTimeOffset StartedAt
)
{
    public const string EventType = EventTypes.LastkingNightStarted;
}
