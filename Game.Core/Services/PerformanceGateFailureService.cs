namespace Game.Core.Services;

public sealed class PerformanceGateFailureService
{
    public PerformanceVerdict Evaluate(PerformanceEvaluationInput input)
    {
        if (!input.HasSummaryJson || !input.HasTelemetryCsv || !input.HasBaselineField || !input.HasCurrentField)
        {
            return PerformanceVerdict.Fail;
        }

        var averageGain = input.CurrentAverageFps - input.BaselineAverageFps;
        var onePercentLowGain = input.CurrentOnePercentLowFps - input.BaselineOnePercentLowFps;
        var varianceWindow = input.DeclaredVarianceWindow;
        var hasMeasurableAverageGain = averageGain > varianceWindow;
        var hasMeasurableOnePercentLowGain = onePercentLowGain > varianceWindow;

        return hasMeasurableAverageGain && hasMeasurableOnePercentLowGain
            ? PerformanceVerdict.Pass
            : PerformanceVerdict.Fail;
    }
}
