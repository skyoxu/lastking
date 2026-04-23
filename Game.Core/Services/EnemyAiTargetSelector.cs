using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public enum EnemyTargetClass
{
    Unit,
    Castle,
    ArmedDefense,
    WallGate,
    BlockingStructure,
    Decoration
}

public sealed record EnemyAiTargetCandidate(
    string Id,
    EnemyTargetClass Class,
    bool IsReachable,
    int Distance,
    bool BlocksRouteToHigherPriority)
{
    public static EnemyAiTargetCandidate Reachable(string id, EnemyTargetClass targetClass, int distance) =>
        new(id, targetClass, true, distance, false);

    public static EnemyAiTargetCandidate Unreachable(string id, EnemyTargetClass targetClass, int distance) =>
        new(id, targetClass, false, distance, false);

    public static EnemyAiTargetCandidate Blocker(string id, int distance) =>
        new(id, EnemyTargetClass.BlockingStructure, false, distance, true);
}

public sealed record EnemyAiTargetDecision(
    string? TargetId,
    bool IsFallbackAttack,
    string? AttackEventTargetId,
    EnemyTargetClass? TargetClass)
{
    public static EnemyAiTargetDecision Idle { get; } = new(null, false, null, null);
}

public sealed class EnemyAiTargetSelector
{
    private static int ReadConfigManagerCadence(ConfigManager manager) => manager.Snapshot.SpawnCadenceSeconds;

    private static readonly EnemyTargetClass[] PriorityOrder =
    {
        EnemyTargetClass.Unit,
        EnemyTargetClass.Castle,
        EnemyTargetClass.ArmedDefense,
        EnemyTargetClass.WallGate
    };

    public EnemyAiTargetDecision SelectTarget(IReadOnlyCollection<EnemyAiTargetCandidate> candidates)
    {
        var reachable = candidates.Where(c => c.IsReachable).ToList();
        foreach (var targetClass in PriorityOrder)
        {
            var bestInClass = reachable
                .Where(c => c.Class == targetClass)
                .OrderBy(c => c.Distance)
                .FirstOrDefault();
            if (bestInClass is not null)
            {
                return new EnemyAiTargetDecision(
                    bestInClass.Id,
                    false,
                    bestInClass.Id,
                    bestInClass.Class);
            }
        }

        var hasBlockedHigherPriority = candidates.Any(c =>
            !c.IsReachable &&
            Array.IndexOf(PriorityOrder, c.Class) >= 0);
        var nearestFallbackBlocker = candidates
            .Where(c => c.BlocksRouteToHigherPriority)
            .OrderBy(c => c.Distance)
            .FirstOrDefault();

        if (hasBlockedHigherPriority && nearestFallbackBlocker is not null)
        {
            return new EnemyAiTargetDecision(
                nearestFallbackBlocker.Id,
                true,
                nearestFallbackBlocker.Id,
                nearestFallbackBlocker.Class);
        }

        var nearestReachable = reachable.OrderBy(c => c.Distance).FirstOrDefault();
        if (nearestReachable is not null)
        {
            return new EnemyAiTargetDecision(
                nearestReachable.Id,
                false,
                nearestReachable.Id,
                nearestReachable.Class);
        }

        return EnemyAiTargetDecision.Idle;
    }
}
