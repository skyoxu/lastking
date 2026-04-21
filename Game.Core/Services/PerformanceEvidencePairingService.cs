using System;

namespace Game.Core.Services;

public sealed class PerformanceEvidencePairingService
{
    public PerformancePairingResult Pair(PerformanceEvidence baselineEvidence, PerformanceEvidence postEvidence)
    {
        ArgumentNullException.ThrowIfNull(baselineEvidence);
        ArgumentNullException.ThrowIfNull(postEvidence);

        if (baselineEvidence.Metrics is null || postEvidence.Metrics is null)
        {
            return new PerformancePairingResult(false, null, null);
        }

        if (!string.Equals(baselineEvidence.RunMode, postEvidence.RunMode, StringComparison.Ordinal))
        {
            return new PerformancePairingResult(false, null, null);
        }

        if (!string.Equals(baselineEvidence.SceneSet, postEvidence.SceneSet, StringComparison.Ordinal))
        {
            return new PerformancePairingResult(false, null, null);
        }

        if (baselineEvidence.FixedSeed != postEvidence.FixedSeed)
        {
            return new PerformancePairingResult(false, null, null);
        }

        var deltaFps1Low = postEvidence.Metrics.Fps1Low - baselineEvidence.Metrics.Fps1Low;
        var deltaAverageFps = postEvidence.Metrics.AverageFps - baselineEvidence.Metrics.AverageFps;
        return new PerformancePairingResult(true, deltaFps1Low, deltaAverageFps);
    }
}
