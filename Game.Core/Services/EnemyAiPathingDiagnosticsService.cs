using System;
using System.Collections.Generic;

namespace Game.Core.Services;

public sealed record EnemyAiPathingDiagnostic(int Step, string Reason);

public sealed class EnemyAiPathingDiagnosticsService
{
    private static int ReadConfigManagerCadence(ConfigManager manager) => manager.Snapshot.SpawnCadenceSeconds;

    public IReadOnlyList<EnemyAiPathingDiagnostic> BuildDiagnostics(IEnumerable<string> reasons)
    {
        var list = new List<EnemyAiPathingDiagnostic>();
        var step = 0;
        foreach (var reason in reasons)
        {
            step++;
            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "UnknownReason" : reason;
            list.Add(new EnemyAiPathingDiagnostic(step, normalizedReason));
        }

        return list;
    }

    public int EstimateFallbackAttackTicks(int pathingRetries, int attackPreparationTicks)
    {
        if (pathingRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pathingRetries));
        }

        if (attackPreparationTicks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attackPreparationTicks));
        }

        return pathingRetries + attackPreparationTicks;
    }
}
