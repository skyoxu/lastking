using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed record EnemyAiPriorityCandidate(string Id, int Priority, int PathCost);

public sealed class EnemyAiTargetingService
{
    public string SelectTargetId(IEnumerable<EnemyAiPriorityCandidate> candidates, int seed)
    {
        var reachable = candidates.Where(c => c.PathCost >= 0).ToList();
        if (reachable.Count == 0)
        {
            return string.Empty;
        }

        var topPriority = reachable.Max(c => c.Priority);
        var samePriority = reachable.Where(c => c.Priority == topPriority).ToList();

        var bestCost = samePriority.Min(c => c.PathCost);
        var tied = samePriority
            .Where(c => c.PathCost == bestCost)
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .ToList();

        if (tied.Count == 1)
        {
            return tied[0].Id;
        }

        var rng = new Random(seed);
        return tied[rng.Next(tied.Count)].Id;
    }
}

