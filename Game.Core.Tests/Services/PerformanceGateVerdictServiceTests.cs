using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class PerformanceGateVerdictServiceTests
{
    [Fact]
    public void EvaluateWindowsBaseline_ShouldPass_WhenMetricsMeetThresholds()
    {
        var sut = new PerformanceGateVerdictService();

        var verdict = sut.EvaluateWindowsBaseline(60.0, 45.0);

        verdict.Should().Be(PerformanceVerdict.Pass);
    }

    [Fact]
    public void EvaluateWindowsBaseline_ShouldFail_WhenMetricsAreBelowThresholds()
    {
        var sut = new PerformanceGateVerdictService();

        var verdict = sut.EvaluateWindowsBaseline(59.9, 44.9);

        verdict.Should().Be(PerformanceVerdict.Fail);
    }

    [Fact]
    public void EvaluateFixedSeedGate_ShouldPass_WhenPlatformAndRunsMeetRequirements()
    {
        var sut = new PerformanceGateVerdictService();

        var verdict = sut.EvaluateFixedSeedGate(
            "windows",
            "fixed",
            new PerformanceGateRunMetrics(45.0, 60.0),
            new PerformanceGateRunMetrics(45.0, 60.0));

        verdict.Should().Be(PerformanceVerdict.Pass);
    }

    [Fact]
    public void EvaluateFixedSeedGate_ShouldFail_WhenPlatformIsNotWindows()
    {
        var sut = new PerformanceGateVerdictService();

        var verdict = sut.EvaluateFixedSeedGate(
            "linux",
            "fixed",
            new PerformanceGateRunMetrics(47.0, 62.0),
            new PerformanceGateRunMetrics(47.0, 62.0));

        verdict.Should().Be(PerformanceVerdict.Fail);
    }

    [Fact]
    public void EvaluateFixedSeedGate_ShouldFail_WhenAnyRunFallsBelowThreshold()
    {
        var sut = new PerformanceGateVerdictService();

        var verdict = sut.EvaluateFixedSeedGate(
            "windows",
            "fixed",
            new PerformanceGateRunMetrics(44.9, 60.0),
            new PerformanceGateRunMetrics(47.0, 62.0));

        verdict.Should().Be(PerformanceVerdict.Fail);
    }
}
