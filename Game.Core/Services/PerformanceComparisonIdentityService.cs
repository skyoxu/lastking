using System;

namespace Game.Core.Services;

public sealed class PerformanceComparisonIdentityService
{
    public ComparisonVerdict Evaluate(PerformanceRunIdentity baseline, PerformanceRunIdentity post)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(post);

        if (!string.Equals(baseline.SceneSet, post.SceneSet, StringComparison.Ordinal))
        {
            return ComparisonVerdict.Fail;
        }

        if (baseline.FixedSeed != post.FixedSeed)
        {
            return ComparisonVerdict.Fail;
        }

        if (!string.Equals(baseline.CameraPathScript, post.CameraPathScript, StringComparison.Ordinal))
        {
            return ComparisonVerdict.Fail;
        }

        if (!string.Equals(baseline.LaunchPreset, post.LaunchPreset, StringComparison.Ordinal))
        {
            return ComparisonVerdict.Fail;
        }

        if (!string.Equals(baseline.RunMode, post.RunMode, StringComparison.Ordinal))
        {
            return ComparisonVerdict.Fail;
        }

        return ComparisonVerdict.Pass;
    }
}
