using System;
using System.Collections.Generic;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class PerformanceRemediationImpactTests
{
    private readonly PerformanceRemediationEvaluator evaluator = new();

    // ACC:T30.6
    [Fact]
    public void ShouldReachPerformanceTargets_WhenApplyingConfigSafeOptimizations()
    {
        var baseline = new FrameMetrics(22.8, 26.4);
        var optimized = new FrameMetrics(16.0, 20.0);
        var baselineSemantics = new GameplaySnapshot(new[] { "TurnStarted", "DiceRolled", "MovementResolved", "TurnEnded" });
        var optimizedSemantics = new GameplaySnapshot(new[] { "TurnStarted", "DiceRolled", "MovementResolved", "TurnEnded" });

        var result = evaluator.Evaluate(baseline, optimized, baselineSemantics, optimizedSemantics);

        result.MeasurableImprovement.Should().BeTrue("optimizations must improve frame-time compared to baseline");
        result.SemanticsUnchanged.Should().BeTrue("config-safe optimizations must preserve gameplay semantics");
        result.Optimized.OnePercentLowFps.Should().BeGreaterOrEqualTo(45.0);
        result.Optimized.AverageFps.Should().BeGreaterOrEqualTo(60.0);
    }

    [Fact]
    public void ShouldKeepGameplaySemanticsUnchanged_WhenApplyingConfigSafeOptimizations()
    {
        var baseline = new FrameMetrics(23.0, 27.0);
        var optimized = new FrameMetrics(20.0, 24.0);
        var baselineSemantics = new GameplaySnapshot(new[] { "TurnStarted", "TaxCalculated", "TreasuryUpdated", "TurnEnded" });
        var optimizedSemantics = new GameplaySnapshot(new[] { "TurnStarted", "TaxCalculated", "TreasuryUpdated", "TurnEnded" });

        var result = evaluator.Evaluate(baseline, optimized, baselineSemantics, optimizedSemantics);

        result.SemanticsUnchanged.Should().BeTrue();
        result.MeasurableImprovement.Should().BeTrue();
    }

    [Fact]
    public void ShouldRefuseOptimizationPlan_WhenGameplaySemanticsWouldChange()
    {
        var candidate = new OptimizationCandidate(
            UpdateLoopBatchingEnabled: true,
            ObjectPoolingEnabled: true,
            ExpensiveQueryCachingEnabled: true,
            ReordersGameplayEvents: true);

        var isConfigSafe = evaluator.IsConfigSafe(candidate);

        isConfigSafe.Should().BeFalse("unsafe plans must be rejected when gameplay semantics can change");
    }

}
