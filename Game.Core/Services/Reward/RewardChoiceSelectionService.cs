using System;
using System.Collections.Generic;

namespace Game.Core.Services.Reward;

public sealed class RewardChoiceSelectionService
{
    private readonly IReadOnlyDictionary<RewardChoice, Action<RewardState>> rewardEffects;

    public RewardChoiceSelectionService(IReadOnlyDictionary<RewardChoice, Action<RewardState>> rewardEffects)
    {
        ArgumentNullException.ThrowIfNull(rewardEffects);
        this.rewardEffects = rewardEffects;
    }

    public RewardState ApplySelection(RewardState state, RewardChoice selectedChoice)
    {
        ArgumentNullException.ThrowIfNull(state);
        var nextState = state.Clone();

        if (rewardEffects.TryGetValue(selectedChoice, out var selectedEffect))
        {
            selectedEffect(nextState);
        }

        return nextState;
    }
}
