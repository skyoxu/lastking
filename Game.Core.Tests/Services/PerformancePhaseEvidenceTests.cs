using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class PerformancePhaseEvidenceTests
{
    // ACC:T30.12
    [Fact]
    public void ShouldBeBlocked_WhenHotspotIsolationEvidenceIsMissing()
    {
        var evidence = new HashSet<PerformanceEvidencePhase>
        {
            PerformanceEvidencePhase.BaselineCapture,
            PerformanceEvidencePhase.FrameBudgetRemediation,
            PerformanceEvidencePhase.StressValidation,
            PerformanceEvidencePhase.GateArtifactOutput
        };

        var sut = new PerformancePhaseEvidenceService();

        var isBlocked = sut.IsBlocked(evidence);

        isBlocked.Should().BeTrue("a missing required phase must block acceptance");
    }

    [Fact]
    public void ShouldBeBlocked_WhenBaselineCaptureEvidenceIsMissing()
    {
        var evidence = new HashSet<PerformanceEvidencePhase>
        {
            PerformanceEvidencePhase.HotspotIsolation,
            PerformanceEvidencePhase.FrameBudgetRemediation,
            PerformanceEvidencePhase.StressValidation,
            PerformanceEvidencePhase.GateArtifactOutput
        };

        var sut = new PerformancePhaseEvidenceService();

        var isBlocked = sut.IsBlocked(evidence);

        isBlocked.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeBlocked_WhenGateArtifactOutputEvidenceIsMissing()
    {
        var evidence = new HashSet<PerformanceEvidencePhase>
        {
            PerformanceEvidencePhase.BaselineCapture,
            PerformanceEvidencePhase.HotspotIsolation,
            PerformanceEvidencePhase.FrameBudgetRemediation,
            PerformanceEvidencePhase.StressValidation
        };

        var sut = new PerformancePhaseEvidenceService();

        var isBlocked = sut.IsBlocked(evidence);

        isBlocked.Should().BeTrue();
    }

    [Fact]
    public void ShouldNotBeBlocked_WhenAllRequiredEvidenceIsPresent()
    {
        var evidence = Enum.GetValues<PerformanceEvidencePhase>().ToHashSet();

        var sut = new PerformancePhaseEvidenceService();

        var isBlocked = sut.IsBlocked(evidence);

        isBlocked.Should().BeFalse();
    }
}
