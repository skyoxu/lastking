using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed record EnemyAiDeterminismCandidate(string Id, int Priority, bool IsPathBlocked);

public sealed record EnemyAiDeterminismState(IReadOnlyList<EnemyAiDeterminismCandidate> Candidates)
{
    public static EnemyAiDeterminismState Create(params EnemyAiDeterminismCandidate[] candidates)
    {
        return new EnemyAiDeterminismState(candidates);
    }
}

public sealed class EnemyAiDeterminismService
{
    private static int ReadConfigManagerCadence(ConfigManager manager) => manager.Snapshot.SpawnCadenceSeconds;

    public string[] RunLoop(EnemyAiDeterminismState state, int seed, int steps)
    {
        var decisions = new string[steps];
        for (var tick = 0; tick < steps; tick++)
        {
            decisions[tick] = DecideTarget(state, seed, tick);
        }

        return decisions;
    }

    public string DecideTarget(EnemyAiDeterminismState state, int seed, int tick)
    {
        var topPriority = state.Candidates.Max(candidate => candidate.Priority);
        var topCandidates = state.Candidates
            .Where(candidate => candidate.Priority == topPriority)
            .OrderBy(candidate => candidate.Id, StringComparer.Ordinal)
            .ToArray();

        var reachable = topCandidates.Where(candidate => !candidate.IsPathBlocked).ToArray();

        if (reachable.Length > 0)
        {
            var index = NextIndex(seed, tick, reachable.Length);
            return reachable[index].Id;
        }

        var fallbackIndex = NextIndex(seed, tick, topCandidates.Length);
        return "fallback:" + topCandidates[fallbackIndex].Id;
    }

    private static int NextIndex(int seed, int tick, int count)
    {
        var value = seed + (tick * 17);
        var normalized = value < 0 ? -value : value;
        return normalized % count;
    }
}

public sealed class DeterministicSimulationLoopService
{
    public int[] RunLoop(int[] initialEnemyPositions, int[] targetThreatPriority, uint seed, int ticks)
    {
        if (initialEnemyPositions is null)
        {
            throw new ArgumentNullException(nameof(initialEnemyPositions));
        }

        if (targetThreatPriority is null)
        {
            throw new ArgumentNullException(nameof(targetThreatPriority));
        }

        if (initialEnemyPositions.Length == 0)
        {
            throw new ArgumentException("At least one enemy is required.", nameof(initialEnemyPositions));
        }

        if (initialEnemyPositions.Length != targetThreatPriority.Length)
        {
            throw new ArgumentException("Enemy and priority arrays must have the same length.");
        }

        if (ticks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        var enemyPositions = new int[initialEnemyPositions.Length];
        Array.Copy(initialEnemyPositions, enemyPositions, initialEnemyPositions.Length);

        var outputs = new int[ticks];
        var highestPriorityTargetIndex = GetHighestPriorityTargetIndex(targetThreatPriority);

        for (var tick = 0; tick < ticks; tick++)
        {
            for (var enemyIndex = 0; enemyIndex < enemyPositions.Length; enemyIndex++)
            {
                var step = (int)((seed + (uint)tick + (uint)enemyIndex) % 3u) + 1;
                var direction = enemyIndex == highestPriorityTargetIndex ? 1 : -1;
                enemyPositions[enemyIndex] += direction * step;
            }

            outputs[tick] = Sum(enemyPositions);
        }

        return outputs;
    }

    private static int GetHighestPriorityTargetIndex(int[] targetThreatPriority)
    {
        var index = 0;
        var bestPriority = targetThreatPriority[0];
        for (var i = 1; i < targetThreatPriority.Length; i++)
        {
            if (targetThreatPriority[i] > bestPriority)
            {
                bestPriority = targetThreatPriority[i];
                index = i;
            }
        }

        return index;
    }

    private static int Sum(int[] values)
    {
        var total = 0;
        for (var i = 0; i < values.Length; i++)
        {
            total += values[i];
        }

        return total;
    }
}

