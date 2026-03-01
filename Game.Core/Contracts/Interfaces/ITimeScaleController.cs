using Game.Core.Contracts.Lastking;

namespace Game.Core.Contracts.Interfaces;

/// <summary>
/// Controls and snapshots runtime speed state transitions.
/// </summary>
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
