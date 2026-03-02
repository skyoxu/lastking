namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.bootstrap.ready.
/// Emitted when the baseline project bootstrap is validated and ready.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0011.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record BootstrapReady(
    string RunId,
    string ProjectRoot,
    bool ExportProfileReady,
    System.DateTimeOffset ReadyAt
)
{
    public const string EventType = EventTypes.LastkingBootstrapReady;
}
