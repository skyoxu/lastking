using Game.Core.Contracts.Lastking;

namespace Game.Core.Contracts.Interfaces;

/// <summary>
/// Encapsulates one-shot cloud save synchronization behavior.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// </remarks>
public interface ICloudSaveSyncService
{
    /// <summary>
    /// Synchronize one save slot with cloud provider.
    /// </summary>
    /// <param name="runId">Run identifier.</param>
    /// <param name="slotId">Save slot identifier.</param>
    /// <param name="direction">Sync direction, for example upload/download.</param>
    /// <param name="steamAccountId">Steam account identity bound to this sync.</param>
    /// <param name="payload">Serialized save payload to sync.</param>
    /// <returns>Deterministic sync result payload.</returns>
    CloudSaveSyncResultDto Sync(string runId, string slotId, string direction, string steamAccountId, string payload);
}
