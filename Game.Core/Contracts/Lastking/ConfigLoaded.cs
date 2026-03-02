namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.config.loaded.
/// Emitted when gameplay config set is loaded and bound to runtime.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0023.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record ConfigLoaded(
    string RunId,
    string ConfigVersion,
    string ConfigHash,
    string SourcePath,
    System.DateTimeOffset LoadedAt
)
{
    public const string EventType = EventTypes.LastkingConfigLoaded;
}
