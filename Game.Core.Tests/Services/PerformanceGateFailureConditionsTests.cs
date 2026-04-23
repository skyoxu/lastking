using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class PerformanceGateFailureConditionsTests
{
    // ACC:T30.10
    [Fact]
    public void ShouldReturnFail_WhenImprovementIsNotMeasurableWithinDeclaredVarianceWindow()
    {
        var sut = new PerformanceGateFailureService();
        var input = new PerformanceEvaluationInput(
            BaselineAverageFps: 60.0,
            CurrentAverageFps: 60.1,
            BaselineOnePercentLowFps: 45.0,
            CurrentOnePercentLowFps: 45.05,
            DeclaredVarianceWindow: 0.5,
            HasSummaryJson: true,
            HasTelemetryCsv: true,
            HasBaselineField: true,
            HasCurrentField: true);

        var verdict = sut.Evaluate(input);

        verdict.Should().Be(
            PerformanceVerdict.Fail,
            "the gate must fail when observed gains stay inside the declared variance window and are not measurable");
    }

    [Fact]
    public void ShouldReturnFail_WhenAnyRequiredArtifactFieldIsMissing()
    {
        var sut = new PerformanceGateFailureService();
        var input = new PerformanceEvaluationInput(
            BaselineAverageFps: 60.0,
            CurrentAverageFps: 63.0,
            BaselineOnePercentLowFps: 45.0,
            CurrentOnePercentLowFps: 48.0,
            DeclaredVarianceWindow: 0.5,
            HasSummaryJson: true,
            HasTelemetryCsv: true,
            HasBaselineField: true,
            HasCurrentField: false);

        var verdict = sut.Evaluate(input);

        verdict.Should().Be(
            PerformanceVerdict.Fail,
            "the gate must fail when a mandatory artifact field is missing even if fps appears improved");
    }

    [Fact]
    public void ShouldReturnFail_WhenAnyRequiredArtifactFileIsMissing()
    {
        var sut = new PerformanceGateFailureService();
        var input = new PerformanceEvaluationInput(
            BaselineAverageFps: 60.0,
            CurrentAverageFps: 64.0,
            BaselineOnePercentLowFps: 45.0,
            CurrentOnePercentLowFps: 49.0,
            DeclaredVarianceWindow: 0.5,
            HasSummaryJson: false,
            HasTelemetryCsv: true,
            HasBaselineField: true,
            HasCurrentField: true);

        var verdict = sut.Evaluate(input);

        verdict.Should().Be(
            PerformanceVerdict.Fail,
            "the gate must fail when any required artifact file is missing");
    }

    [Fact]
    public void ShouldReturnPass_WhenImprovementIsMeasurableAndArtifactsAreComplete()
    {
        var sut = new PerformanceGateFailureService();
        var input = new PerformanceEvaluationInput(
            BaselineAverageFps: 60.0,
            CurrentAverageFps: 62.0,
            BaselineOnePercentLowFps: 45.0,
            CurrentOnePercentLowFps: 46.0,
            DeclaredVarianceWindow: 0.5,
            HasSummaryJson: true,
            HasTelemetryCsv: true,
            HasBaselineField: true,
            HasCurrentField: true);

        var verdict = sut.Evaluate(input);

        verdict.Should().Be(PerformanceVerdict.Pass);
    }

    [Fact]
    public void ShouldReturnFail_WhenImprovementEqualsDeclaredVarianceWindow()
    {
        var sut = new PerformanceGateFailureService();
        var input = new PerformanceEvaluationInput(
            BaselineAverageFps: 60.0,
            CurrentAverageFps: 60.5,
            BaselineOnePercentLowFps: 45.0,
            CurrentOnePercentLowFps: 45.5,
            DeclaredVarianceWindow: 0.5,
            HasSummaryJson: true,
            HasTelemetryCsv: true,
            HasBaselineField: true,
            HasCurrentField: true);

        var verdict = sut.Evaluate(input);

        verdict.Should().Be(PerformanceVerdict.Fail);
    }
}
