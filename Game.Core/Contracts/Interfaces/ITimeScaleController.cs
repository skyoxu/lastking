using Game.Core.Contracts.Lastking;

namespace Game.Core.Contracts.Interfaces;

/// <summary>
/// Controls and snapshots runtime speed state transitions.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0030.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public interface ITimeScaleController
{
    /// <summary>
    /// Apply one runtime speed state transition.
    /// </summary>
    /// <param name="runId">Run identifier.</param>
    /// <param name="currentScalePercent">Target speed in percent (0/100/200).</param>
    /// <param name="isPaused">Whether runtime should be paused.</param>
    /// <returns>Updated runtime speed state snapshot.</returns>
    TimeScaleStateDto SetScale(string runId, int currentScalePercent, bool isPaused);
}
