using System;
using System.Linq;

namespace Game.Core.Services;

public sealed class PerformanceRemediationEvaluator
{
    public PerformanceRemediationResult Evaluate(
        FrameMetrics baseline,
        FrameMetrics optimized,
        GameplaySnapshot baselineSemantics,
        GameplaySnapshot optimizedSemantics)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(optimized);
        ArgumentNullException.ThrowIfNull(baselineSemantics);
        ArgumentNullException.ThrowIfNull(optimizedSemantics);

        var measurableImprovement =
            optimized.AverageFrameMs < baseline.AverageFrameMs &&
            optimized.OnePercentLowFrameMs < baseline.OnePercentLowFrameMs;
        var semanticsUnchanged = baselineSemantics.Events.SequenceEqual(optimizedSemantics.Events);

        return new PerformanceRemediationResult(measurableImprovement, semanticsUnchanged, baseline, optimized);
    }

    public bool IsConfigSafe(OptimizationCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return candidate.UpdateLoopBatchingEnabled
            && candidate.ObjectPoolingEnabled
            && candidate.ExpensiveQueryCachingEnabled
            && !candidate.ReordersGameplayEvents;
    }
}
