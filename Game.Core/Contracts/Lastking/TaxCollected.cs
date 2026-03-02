namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.tax.collected.
/// Emitted when one residence tax tick is settled.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0021.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record TaxCollected(
    string RunId,
    int DayNumber,
    string ResidenceId,
    int GoldDelta,
    int TotalGold,
    System.DateTimeOffset CollectedAt
)
{
    public const string EventType = EventTypes.LastkingTaxCollected;
}
