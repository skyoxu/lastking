using System;
using System.Collections.Generic;
using Game.Core.Contracts.Lastking;

namespace Game.Core.Services.Reward;

public sealed record NightlyRewardResult(
    bool Triggered,
    NightType ActiveNightType,
    IReadOnlyList<string> Choices,
    RewardOffered? OfferedEvent,
    int GrantedFallbackGold)
{
    public static NightlyRewardResult NotTriggered(NightType activeNightType)
    {
        return new NightlyRewardResult(
            Triggered: false,
            ActiveNightType: activeNightType,
            Choices: Array.Empty<string>(),
            OfferedEvent: null,
            GrantedFallbackGold: 0);
    }
}
