using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Contracts.Lastking;

namespace Game.Core.Services.Reward;

public sealed class RewardManager
{
    private readonly IReadOnlyDictionary<NightType, IReadOnlyList<string>> rewardPools;
    private readonly int fallbackGold;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly HashSet<string> processedTransitionTokens = new(StringComparer.Ordinal);

    private int progressionStepCount;
    private int triggerCount;
    private int fallbackGoldGranted;

    public RewardManager(
        IReadOnlyDictionary<NightType, IReadOnlyList<string>> rewardPools,
        int fallbackGold,
        Func<DateTimeOffset> utcNow)
    {
        ArgumentNullException.ThrowIfNull(rewardPools);
        ArgumentNullException.ThrowIfNull(utcNow);
        ArgumentOutOfRangeException.ThrowIfNegative(fallbackGold);

        this.rewardPools = rewardPools;
        this.fallbackGold = fallbackGold;
        this.utcNow = utcNow;
    }

    public int ProgressionStepCount => progressionStepCount;

    public int TriggerCount => triggerCount;

    public int FallbackGoldGranted => fallbackGoldGranted;

    public NightlyRewardResult ProcessPhaseTransition(
        string runId,
        int dayNumber,
        bool enteredNight,
        bool isEliteNight,
        bool isBossNight,
        string transitionToken)
    {
        progressionStepCount++;
        var activeNightType = ResolveNightType(isEliteNight, isBossNight);

        if (!enteredNight)
        {
            return NightlyRewardResult.NotTriggered(activeNightType);
        }

        if (!processedTransitionTokens.Add(transitionToken))
        {
            return NightlyRewardResult.NotTriggered(activeNightType);
        }

        triggerCount++;

        if (!rewardPools.TryGetValue(activeNightType, out var pool) || pool.Count == 0)
        {
            fallbackGoldGranted += fallbackGold;
            return new NightlyRewardResult(
                Triggered: true,
                ActiveNightType: activeNightType,
                Choices: Array.Empty<string>(),
                OfferedEvent: null,
                GrantedFallbackGold: fallbackGold);
        }

        var choices = pool.Take(3).ToArray();
        var optionA = choices.Length > 0 ? choices[0] : string.Empty;
        var optionB = choices.Length > 1 ? choices[1] : string.Empty;
        var optionC = choices.Length > 2 ? choices[2] : string.Empty;
        var offeredEvent = new RewardOffered(
            runId,
            dayNumber,
            activeNightType == NightType.Elite,
            activeNightType == NightType.Boss,
            optionA,
            optionB,
            optionC,
            utcNow());

        return new NightlyRewardResult(
            Triggered: true,
            ActiveNightType: activeNightType,
            Choices: choices,
            OfferedEvent: offeredEvent,
            GrantedFallbackGold: 0);
    }

    private static NightType ResolveNightType(bool isEliteNight, bool isBossNight)
    {
        if (isBossNight)
        {
            return NightType.Boss;
        }

        if (isEliteNight)
        {
            return NightType.Elite;
        }

        return NightType.Normal;
    }
}
