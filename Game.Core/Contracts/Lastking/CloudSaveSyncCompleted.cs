namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.cloud_save.sync.completed.
/// Emitted when one cloud-save sync operation is completed.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0008, ADR-0011.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record CloudSaveSyncCompleted(
    string RunId,
    string SlotId,
    string Direction,
    string SteamAccountId,
    bool Success,
    string ErrorCode,
    string RemoteRevision,
    System.DateTimeOffset SyncedAt
)
{
    public const string EventType = EventTypes.LastkingCloudSaveSyncCompleted;
}
