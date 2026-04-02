using System;
using System.Collections.Generic;

namespace Game.Core.Services;

public sealed record ChannelRule(int Day1Budget, decimal DailyGrowth, int ChannelLimit, int CostPerEnemy);

public sealed record ChannelBudgetConfiguration(ChannelRule Normal, ChannelRule Elite, ChannelRule Boss)
{
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
