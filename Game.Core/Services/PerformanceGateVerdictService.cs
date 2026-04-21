using System;

namespace Game.Core.Services;

public sealed class PerformanceGateVerdictService
{
    private const double MinFps1PctlLow = 45.0;
    private const double MinAverageFps = 60.0;

    public PerformanceVerdict EvaluateWindowsBaseline(double averageFps, double onePercentLowFps)
    {
        return averageFps >= MinAverageFps && onePercentLowFps >= MinFps1PctlLow
            ? PerformanceVerdict.Pass
            : PerformanceVerdict.Fail;
    }

    public PerformanceVerdict EvaluateFixedSeedGate(
        string platform,
        string seedMode,
        PerformanceGateRunMetrics? headlessMetrics,
        PerformanceGateRunMetrics? playableMetrics)
    {
        if (!string.Equals(platform, "windows", StringComparison.OrdinalIgnoreCase))
        {
            return PerformanceVerdict.Fail;
        }

        if (!string.Equals(seedMode, "fixed", StringComparison.OrdinalIgnoreCase))
        {
            return PerformanceVerdict.Fail;
        }

        if (headlessMetrics is null || playableMetrics is null)
        {
            return PerformanceVerdict.Fail;
        }

        var headlessPass = EvaluateWindowsBaseline(headlessMetrics.AverageFps, headlessMetrics.Fps1Low);
        var playablePass = EvaluateWindowsBaseline(playableMetrics.AverageFps, playableMetrics.Fps1Low);
        return headlessPass == PerformanceVerdict.Pass && playablePass == PerformanceVerdict.Pass
            ? PerformanceVerdict.Pass
            : PerformanceVerdict.Fail;
    }
}
