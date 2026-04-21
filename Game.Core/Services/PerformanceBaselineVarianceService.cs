using System;

namespace Game.Core.Services;

public sealed class PerformanceBaselineVarianceService
{
    public bool IsDeterministic(BaselineCaptureProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(profile.CameraPathId) || string.IsNullOrWhiteSpace(profile.SessionScriptId))
        {
            return false;
        }

        if (profile.VarianceWindowPercent < 0)
        {
            return false;
        }

        var fps1LowBaseline = Math.Max(0.0001d, profile.RunA.Fps1Low);
        var avgBaseline = Math.Max(0.0001d, profile.RunA.AverageFps);

        var fps1LowVariancePercent = Math.Abs(profile.RunA.Fps1Low - profile.RunB.Fps1Low) / fps1LowBaseline * 100d;
        var avgVariancePercent = Math.Abs(profile.RunA.AverageFps - profile.RunB.AverageFps) / avgBaseline * 100d;

        return fps1LowVariancePercent <= profile.VarianceWindowPercent
            && avgVariancePercent <= profile.VarianceWindowPercent;
    }
}
