namespace Game.Core.Contracts.Lastking;

/// <summary>
/// DTO for cloud save sync result.
/// </summary>
public sealed record CloudSaveSyncResultDto(
    string SlotId,
    string Direction,
    bool Success,
    string ErrorCode,
    string RemoteRevision,
    System.DateTimeOffset SyncedAt
);
