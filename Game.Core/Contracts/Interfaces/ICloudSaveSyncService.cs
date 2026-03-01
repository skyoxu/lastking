using Game.Core.Contracts.Lastking;

namespace Game.Core.Contracts.Interfaces;

/// <summary>
/// Encapsulates one-shot cloud save synchronization behavior.
/// </summary>
public interface ICloudSaveSyncService
{
    /// <summary>
    /// Synchronize one save slot with cloud provider.
    /// </summary>
    /// <param name="runId">Run identifier.</param>
    /// <param name="slotId">Save slot identifier.</param>
    /// <param name="direction">Sync direction, for example upload/download.</param>
    /// <param name="steamAccountId">Steam account identity bound to this sync.</param>
    /// <returns>Deterministic sync result payload.</returns>
    CloudSaveSyncResultDto Sync(string runId, string slotId, string direction, string steamAccountId);
}
