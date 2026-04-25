using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class FrameTimeHotspotRankingTests
{
    // ACC:T30.5
    // ACC:T45.3
    [Fact]
    public void ShouldRankSubsystemOffendersByFrameTime_WhenProfilingSamplesAreProvided()
    {
        var samples = new[]
        {
            new FrameTimingSample("AI", "PlannerTick", 8.2, "run-1", 10),
            new FrameTimingSample("AI", "PathProbe", 6.1, "run-1", 11),
            new FrameTimingSample("spawn", "SpawnBatch", 5.5, "run-1", 12),
            new FrameTimingSample("spawn", "SpawnBatch", 6.5, "run-1", 13),
            new FrameTimingSample("render", "ShadowPass", 12.0, "run-1", 14),
            new FrameTimingSample("render", "Culling", 10.8, "run-1", 15),
            new FrameTimingSample("UI", "HudRefresh", 4.0, "run-1", 16),
            new FrameTimingSample("UI", "TooltipRefresh", 3.2, "run-1", 17),
        };

        var sut = new FrameTimeHotspotRankingService();

        var report = sut.BuildReport(samples);

        report.Rows.Select(row => row.Subsystem).Should().Equal("render", "AI", "spawn", "UI");
        report.Rows.Select(row => row.Offender).Should().Equal("ShadowPass", "PlannerTick", "SpawnBatch", "HudRefresh");
    }

    [Fact]
    public void ShouldProduceSameRanking_WhenTimingDataIsEquivalentAcrossRuns()
    {
        var runA = new[]
        {
            new FrameTimingSample("AI", "PlannerTick", 8.0, "run-a", 1),
            new FrameTimingSample("spawn", "SpawnBatch", 6.0, "run-a", 2),
            new FrameTimingSample("render", "ShadowPass", 11.0, "run-a", 3),
            new FrameTimingSample("UI", "HudRefresh", 3.0, "run-a", 4),
        };
        var runB = new[]
        {
            new FrameTimingSample("UI", "HudRefresh", 3.0, "run-b", 8),
            new FrameTimingSample("render", "ShadowPass", 11.0, "run-b", 9),
            new FrameTimingSample("spawn", "SpawnBatch", 6.0, "run-b", 10),
            new FrameTimingSample("AI", "PlannerTick", 8.0, "run-b", 11),
        };

        var sut = new FrameTimeHotspotRankingService();

        var reportA = sut.BuildReport(runA);
        var reportB = sut.BuildReport(runB);

        reportB.Rows.Should().Equal(reportA.Rows);
    }

    [Fact]
    public void ShouldRejectSamples_WhenSubsystemIsUnknown()
    {
        var samples = new[]
        {
            new FrameTimingSample("AI", "PlannerTick", 7.0, "run-2", 21),
            new FrameTimingSample("audio", "MixerTick", 2.0, "run-2", 22),
        };

        var sut = new FrameTimeHotspotRankingService();
        Action act = () => sut.BuildReport(samples);

        act.Should().Throw<InvalidOperationException>().WithMessage("*unknown subsystem*");
    }

}
