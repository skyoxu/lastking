using FluentAssertions;
using Game.Core.Services;
using System.Linq;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class EnemyAiPathingDiagnosticsTests
{
    // ACC:T6.19
    [Fact]
    public void ShouldRecordOneDiagnosticPerFallbackDecision_WhenBlockedMapRegressionIsSimulated()
    {
        var sut = new EnemyAiPathingDiagnosticsService();
        var fallbackDecisions = new[]
        {
            "NoPathToPrimaryTarget",
            "PrimaryBlockedByObstacle",
            "FallbackToNearestReachableTile",
            "FallbackAttackWithinTimeout"
        };

        var diagnostics = sut.BuildDiagnostics(fallbackDecisions);

        diagnostics.Should().HaveCount(fallbackDecisions.Length);
        diagnostics.Should().OnlyContain(d => !string.IsNullOrWhiteSpace(d.Reason));
        diagnostics.Select(d => d.Reason).Should().Equal(fallbackDecisions);
        diagnostics.Select(d => d.Step).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void ShouldReachFallbackAttackBeforeTimeout_WhenFallbackLoopAdvancesDeterministically()
    {
        var sut = new EnemyAiPathingDiagnosticsService();
        const int timeoutTicks = 10;
        var ticksUsed = sut.EstimateFallbackAttackTicks(pathingRetries: 3, attackPreparationTicks: 2);

        ticksUsed.Should().BeGreaterThan(0);
        ticksUsed.Should().BeLessOrEqualTo(timeoutTicks);
    }

    [Fact]
    public void ShouldNormalizeEmptyReason_WhenFallbackReasonIsMissing()
    {
        var sut = new EnemyAiPathingDiagnosticsService();
        var reasons = new[] { "FallbackToNearestReachableTile", "", "FallbackAttackWithinTimeout" };

        var diagnostics = sut.BuildDiagnostics(reasons);

        diagnostics.Select(d => d.Reason).Should().Equal(
            "FallbackToNearestReachableTile",
            "UnknownReason",
            "FallbackAttackWithinTimeout");
    }
}
