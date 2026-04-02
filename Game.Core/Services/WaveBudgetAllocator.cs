using System;

namespace Game.Core.Services;

public sealed class WaveBudgetAllocator
{
    private readonly WaveAccountingState state;

    public WaveBudgetAllocator(WaveAccountingState initialState)
    {
        state = initialState ?? throw new ArgumentNullException(nameof(initialState));
    }

    public WaveAccountingState Current => state;

    public static WaveBudgetAllocator CreateForRuntime(
        ConfigManager configManager,
        WaveAccountingState initialState)
    {
        ArgumentNullException.ThrowIfNull(configManager);
        _ = configManager.Snapshot;
        return new WaveBudgetAllocator(initialState);
    }

    public bool TryApplyMutationDuringWaveExecution(WaveMutationAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        var modifiesLockedFields =
            attempt.Normal is not null ||
            attempt.Elite is not null ||
            attempt.Boss is not null ||
            attempt.AccountingVersion is not null;

        if (modifiesLockedFields)
        {
            return false;
        }

        return true;
    }
}
