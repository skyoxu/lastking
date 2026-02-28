namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.castle.hp_changed.
/// Emitted after castle durability value changes.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0018.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record CastleHpChanged(
    string RunId,
    int DayNumber,
    int PreviousHp,
    int CurrentHp,
    System.DateTimeOffset ChangedAt
)
{
    public const string EventType = EventTypes.LastkingCastleHpChanged;
}
