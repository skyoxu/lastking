using System;
using System.Collections.Generic;

namespace Game.Core.Services;

public sealed record ChannelRule(int Day1Budget, decimal DailyGrowth, int ChannelLimit, int CostPerEnemy);

public sealed record ChannelBudgetConfiguration(ChannelRule Normal, ChannelRule Elite, ChannelRule Boss)
{
    public static ChannelBudgetConfiguration FromConfigManager(
        ConfigManager configManager,
        ChannelRule? eliteRule = null,
        ChannelRule? bossRule = null)
    {
        ArgumentNullException.ThrowIfNull(configManager);

        var snapshot = configManager.Snapshot;
        var normalRule = new ChannelRule(
            snapshot.Day1Budget,
            snapshot.DailyGrowth,
            20,
            10);

        var snapshotEliteRule = snapshot.EliteRule ?? new ChannelRule(120, 1.2m, 8, 20);
        var snapshotBossRule = snapshot.BossRule ?? new ChannelRule(300, 1.2m, 3, 100);

        return new ChannelBudgetConfiguration(
            Normal: normalRule,
            Elite: eliteRule ?? snapshotEliteRule,
            Boss: bossRule ?? snapshotBossRule);
    }

    public ChannelRule GetRule(string channelName)
    {
        return channelName switch
        {
            WaveManager.NormalChannel => Normal,
            WaveManager.EliteChannel => Elite,
            WaveManager.BossChannel => Boss,
            _ => throw new ArgumentOutOfRangeException(nameof(channelName), channelName, "Unknown channel.")
        };
    }
}

public sealed record ChannelAudit(int InputBudget, int Allocated, int Spent, int Remaining);

public sealed record ChannelWaveResult(string ChannelName, ChannelAudit Audit, IReadOnlyList<int> SpawnOrder);

public sealed record WaveResult(int DayIndex, int Seed, IReadOnlyDictionary<string, ChannelWaveResult> ChannelResults);

public sealed record ChannelAccounting(int InputBudget, int Allocated, int Spent, int Remaining);

public sealed record WaveAccountingState(
    ChannelAccounting Normal,
    ChannelAccounting Elite,
    ChannelAccounting Boss,
    int AccountingVersion = 0);

public sealed record AccountingTransitionAttempt(string SourceChannel, string ChargeChannel, int Spend);

public sealed record TransitionOutcome(bool Accepted, string Reason, WaveAccountingState StateAfter, string Trace);

public sealed record WaveMutationAttempt(
    ChannelAccounting? Normal,
    ChannelAccounting? Elite,
    ChannelAccounting? Boss,
    int? AccountingVersion);
