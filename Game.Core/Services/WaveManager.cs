using System;
using System.Collections.Generic;

namespace Game.Core.Services;

public sealed partial class WaveManager
{
    public const string NormalChannel = "normal";
    public const string EliteChannel = "elite";
    public const string BossChannel = "boss";

    public WaveResult GenerateFromConfig(
        int dayIndex,
        ConfigManager configManager,
        int seed,
        ChannelRule? eliteRule = null,
        ChannelRule? bossRule = null)
    {
        var channelBudgetConfiguration = ChannelBudgetConfiguration.FromConfigManager(
            configManager,
            eliteRule,
            bossRule);

        return Generate(dayIndex, channelBudgetConfiguration, seed);
    }

    public WaveResult Generate(int dayIndex, ChannelBudgetConfiguration channelBudgetConfiguration, int seed)
    {
        if (dayIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dayIndex), dayIndex, "Day index must be at least 1.");
        }

        ArgumentNullException.ThrowIfNull(channelBudgetConfiguration);

        var normalInput = ComputeBudget(channelBudgetConfiguration.Normal.Day1Budget, channelBudgetConfiguration.Normal.DailyGrowth, dayIndex);
        var eliteInput = ComputeBudget(channelBudgetConfiguration.Elite.Day1Budget, channelBudgetConfiguration.Elite.DailyGrowth, dayIndex);
        var bossInput = ComputeBudget(channelBudgetConfiguration.Boss.Day1Budget, channelBudgetConfiguration.Boss.DailyGrowth, dayIndex);

        var channelResults = new Dictionary<string, ChannelWaveResult>(StringComparer.Ordinal)
        {
            [NormalChannel] = BuildChannelResult(NormalChannel, channelBudgetConfiguration.Normal, dayIndex, seed, normalInput),
            [EliteChannel] = BuildChannelResult(EliteChannel, channelBudgetConfiguration.Elite, dayIndex, seed, eliteInput),
            [BossChannel] = BuildChannelResult(BossChannel, channelBudgetConfiguration.Boss, dayIndex, seed, bossInput)
        };

        return new WaveResult(dayIndex, seed, channelResults);
    }

    public TransitionOutcome TryApplyAccountingTransition(
        WaveAccountingState accountingState,
        AccountingTransitionAttempt attempt,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(accountingState);
        ArgumentNullException.ThrowIfNull(attempt);

        if (attempt.Spend < 0)
        {
            return Reject("negative-spend", accountingState, attempt, seed);
        }

        if (!string.Equals(attempt.SourceChannel, attempt.ChargeChannel, StringComparison.Ordinal))
        {
            return Reject("cross-channel-charge", accountingState, attempt, seed);
        }

        var chargedChannel = GetChannel(accountingState, attempt.ChargeChannel);
        if (attempt.Spend > chargedChannel.Remaining)
        {
            return Reject("over-budget-spend", accountingState, attempt, seed);
        }

        var updatedState = ApplySpend(accountingState, attempt.ChargeChannel, attempt.Spend);
        var trace = $"{seed}|accepted|{attempt.SourceChannel}->{attempt.ChargeChannel}|{attempt.Spend}";
        return new TransitionOutcome(Accepted: true, Reason: "accepted", StateAfter: updatedState, Trace: trace);
    }

    private static TransitionOutcome Reject(
        string reason,
        WaveAccountingState accountingState,
        AccountingTransitionAttempt attempt,
        int seed)
    {
        var trace = $"{seed}|reject|{attempt.SourceChannel}->{attempt.ChargeChannel}|{attempt.Spend}|{reason}|0";
        return new TransitionOutcome(Accepted: false, Reason: reason, StateAfter: accountingState, Trace: trace);
    }

    private static WaveAccountingState ApplySpend(WaveAccountingState state, string channelName, int spend)
    {
        return channelName switch
        {
            NormalChannel => state with { Normal = Spend(state.Normal, spend) },
            EliteChannel => state with { Elite = Spend(state.Elite, spend) },
            BossChannel => state with { Boss = Spend(state.Boss, spend) },
            _ => throw new ArgumentOutOfRangeException(nameof(channelName), channelName, "Unknown channel.")
        };
    }

    private static ChannelAccounting GetChannel(WaveAccountingState state, string channelName)
    {
        return channelName switch
        {
            NormalChannel => state.Normal,
            EliteChannel => state.Elite,
            BossChannel => state.Boss,
            _ => throw new ArgumentOutOfRangeException(nameof(channelName), channelName, "Unknown channel.")
        };
    }

    private static ChannelAccounting Spend(ChannelAccounting channelAccounting, int spend)
    {
        return channelAccounting with
        {
            Spent = channelAccounting.Spent + spend,
            Remaining = channelAccounting.Remaining - spend
        };
    }

    private static ChannelWaveResult BuildChannelResult(
        string channelName,
        ChannelRule channelRule,
        int dayIndex,
        int seed,
        int inputBudget)
    {
        var safeCostPerEnemy = Math.Max(1, channelRule.CostPerEnemy);
        var allocationCap = checked(channelRule.ChannelLimit * safeCostPerEnemy);
        var allocated = Math.Min(inputBudget, allocationCap);

        var maxSpawnable = allocated / safeCostPerEnemy;
        var spendUnits = maxSpawnable == 0
            ? 0
            : (Math.Abs(Hash(seed, dayIndex, channelName, inputBudget)) % maxSpawnable) + 1;

        var spent = spendUnits * safeCostPerEnemy;
        var remaining = allocated - spent;

        var spawnOrder = new List<int>(spendUnits);
        var rolling = Hash(seed, dayIndex, channelName, inputBudget);
        for (var index = 0; index < spendUnits; index++)
        {
            rolling = unchecked((rolling * 1103515245) + 12345);
            var spawnId = (int)(Math.Abs((long)rolling % 997L) + 1L);
            spawnOrder.Add(spawnId);
        }

        var audit = new ChannelAudit(
            InputBudget: inputBudget,
            Allocated: allocated,
            Spent: spent,
            Remaining: remaining);

        return new ChannelWaveResult(channelName, audit, spawnOrder);
    }

    private static int ComputeBudget(int day1Budget, decimal dailyGrowth, int dayIndex)
    {
        var growthFactor = Math.Pow((double)dailyGrowth, dayIndex - 1);
        return (int)Math.Round(day1Budget * growthFactor, MidpointRounding.AwayFromZero);
    }

    private static int Hash(int seed, int dayIndex, string channelName, int budgetSalt)
    {
        unchecked
        {
            var hash = seed;
            hash = (hash * 31) + dayIndex;
            hash = (hash * 31) + budgetSalt;
            foreach (var ch in channelName)
            {
                hash = (hash * 31) + ch;
            }

            return hash;
        }
    }
}
