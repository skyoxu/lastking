using Game.Core.Contracts.Lastking;

namespace Game.Core.Contracts.Interfaces;

/// <summary>
/// Provides deterministic nightly budget computation for spawn planning.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0032.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public interface IWaveBudgetPolicy
{
    /// <summary>
    /// Compute a wave budget snapshot for the current night.
    /// </summary>
    /// <param name="dayNumber">Current in-run day number, 1-based.</param>
    /// <param name="nightNumber">Current in-run night number, 1-based.</param>
    /// <param name="isEliteNight">Whether this night is marked as elite.</param>
    /// <param name="isBossNight">Whether this night is marked as boss.</param>
    /// <returns>Wave budget output for runtime scheduling.</returns>
    WaveBudgetDto Compute(
        int dayNumber,
        int nightNumber,
        bool isEliteNight,
        bool isBossNight
    );
}
