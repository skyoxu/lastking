namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.camera.scrolled.
/// Emitted when camera position is updated by edge or keyboard scrolling.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0022.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record CameraScrolled(
    string RunId,
    int PositionX,
    int PositionY,
    string InputMode,
    System.DateTimeOffset ScrolledAt
)
{
    public const string EventType = EventTypes.LastkingCameraScrolled;
}
