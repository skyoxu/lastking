namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.resources.changed.
/// Emitted when tracked economy resources are updated.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0021.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record ResourcesChanged(
    string RunId,
    int DayNumber,
    int Gold,
    int Iron,
    int PopulationCap,
    System.DateTimeOffset ChangedAt
)
{
    public const string EventType = EventTypes.LastkingResourcesChanged;
}
