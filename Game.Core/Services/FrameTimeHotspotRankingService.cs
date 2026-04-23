using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Services;

public sealed class FrameTimeHotspotRankingService
{
    private static readonly HashSet<string> KnownSubsystems = new(StringComparer.Ordinal)
    {
        "AI",
        "spawn",
        "render",
        "UI",
    };

    public FrameTimeHotspotReport BuildReport(IEnumerable<FrameTimingSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var hotspotBySubsystem = new Dictionary<string, FrameTimeHotspotRow>(StringComparer.Ordinal);

        foreach (var sample in samples)
        {
            if (!KnownSubsystems.Contains(sample.Subsystem))
            {
                throw new InvalidOperationException($"unknown subsystem: {sample.Subsystem}");
            }

            if (!hotspotBySubsystem.TryGetValue(sample.Subsystem, out var existing))
            {
                hotspotBySubsystem[sample.Subsystem] = new FrameTimeHotspotRow(
                    sample.Subsystem,
                    sample.Operation,
                    sample.FrameTimeMs,
                    sample.FrameTimeMs,
                    string.Empty);
                continue;
            }

            var worstFrameTime = Math.Max(existing.WorstFrameTimeMs, sample.FrameTimeMs);
            var totalFrameTime = existing.AverageFrameTimeMs * existing.sampleCount + sample.FrameTimeMs;
            var sampleCount = existing.sampleCount + 1;
            var averageFrameTime = totalFrameTime / sampleCount;
            var offender = sample.FrameTimeMs >= existing.WorstFrameTimeMs
                ? sample.Operation
                : existing.Offender;

            hotspotBySubsystem[sample.Subsystem] = existing with
            {
                Offender = offender,
                WorstFrameTimeMs = worstFrameTime,
                AverageFrameTimeMs = averageFrameTime,
                sampleCount = sampleCount,
                SampleRunId = string.Empty
            };
        }

        var rows = hotspotBySubsystem.Values
            .OrderByDescending(row => row.WorstFrameTimeMs)
            .ThenBy(row => row.Subsystem, StringComparer.Ordinal)
            .ToArray();
        return new FrameTimeHotspotReport(rows);
    }
}
