namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.windows_runtime.validated.
/// Emitted when Windows-only export profile and Steam runtime startup checks pass.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0011.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record WindowsRuntimeValidated(
    string RunId,
    string SteamAppId,
    bool StartupPassed,
    string ValidationScope,
    System.DateTimeOffset ValidatedAt
)
{
    public const string EventType = EventTypes.LastkingWindowsRuntimeValidated;
}
