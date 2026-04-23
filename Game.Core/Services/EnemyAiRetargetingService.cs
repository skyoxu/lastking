using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed record EnemyAiRetargetCandidate(string TargetId, int Priority, bool IsReachable);

public sealed class EnemyAiRetargetingService
{
    private static int ReadConfigManagerCadence(ConfigManager manager) => manager.Snapshot.SpawnCadenceSeconds;

    public EnemyAiRetargetCandidate? SelectNextReachableTarget(
        IEnumerable<EnemyAiRetargetCandidate> candidates,
        string? currentTargetId)
    {
        if (candidates is null)
        {
            throw new ArgumentNullException(nameof(candidates));
        }

        var reachableByPriority = candidates
            .Where(static c => c.IsReachable)
            .OrderByDescending(static c => c.Priority)
            .ThenBy(static c => c.TargetId, StringComparer.Ordinal)
            .ToList();

        if (reachableByPriority.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(currentTargetId))
        {
            var currentStillReachable = reachableByPriority.FirstOrDefault(
                c => string.Equals(c.TargetId, currentTargetId, StringComparison.Ordinal));
            if (currentStillReachable is not null)
            {
                return currentStillReachable;
            }
        }

        return reachableByPriority[0];
    }
}

