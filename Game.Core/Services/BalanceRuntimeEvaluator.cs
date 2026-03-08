using System;

namespace Game.Core.Services;

/// <summary>
/// Deterministic runtime metrics evaluator consuming typed balance snapshot.
/// </summary>
public static class BalanceRuntimeEvaluator
{
    public static RuntimeBalanceMetrics Evaluate(BalanceSnapshot snapshot, int dayIndex)
    {
        var clampedDay = Math.Max(1, dayIndex);
        var cycleSeconds = snapshot.DaySeconds + snapshot.NightSeconds;
        var growthFactor = Math.Pow((double)snapshot.DailyGrowth, clampedDay - 1);
        var waveBudget = (int)Math.Round(snapshot.Day1Budget * growthFactor, MidpointRounding.AwayFromZero);
        var spawnsPerMinute = 60d / snapshot.SpawnCadenceSeconds;

        return new RuntimeBalanceMetrics(
            DayNightCycleSeconds: cycleSeconds,
            WaveBudget: waveBudget,
            SpawnCadenceSeconds: snapshot.SpawnCadenceSeconds,
            SpawnsPerMinute: spawnsPerMinute,
            BossCount: snapshot.BossCount);
    }
}

public sealed record RuntimeBalanceMetrics(
    int DayNightCycleSeconds,
    int WaveBudget,
    int SpawnCadenceSeconds,
    double SpawnsPerMinute,
    int BossCount);
