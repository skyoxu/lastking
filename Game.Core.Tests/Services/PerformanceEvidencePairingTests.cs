using System;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class PerformanceEvidencePairingTests
{
    // ACC:T30.11
    [Fact]
    public void ShouldRejectPairing_WhenFixedSeedSceneSetDiffers()
    {
        var baselineEvidence = new PerformanceEvidence(
            SceneSet: "city_loop_a",
            FixedSeed: 1337,
            RunMode: "headless",
            Metrics: new PerformanceMetric(Fps1Low: 45.0, AverageFps: 60.0));

        var postEvidence = new PerformanceEvidence(
            SceneSet: "city_loop_b",
            FixedSeed: 1337,
            RunMode: "headless",
            Metrics: new PerformanceMetric(Fps1Low: 50.0, AverageFps: 64.0));

        var service = new PerformanceEvidencePairingService();

        var result = service.Pair(baselineEvidence, postEvidence);

        result.IsPairable.Should().BeFalse("paired evidence must use the same fixed-seed scene set before deltas are trusted");
        result.DeltaFps1Low.Should().BeNull();
        result.DeltaAverageFps.Should().BeNull();
    }

    [Fact]
    public void ShouldExposeDirectlyVerifiableDeltas_WhenEvidenceIsPairedForSameFixedSeedSceneSet()
    {
        var baselineEvidence = new PerformanceEvidence(
            SceneSet: "city_loop_a",
            FixedSeed: 1337,
            RunMode: "headless",
            Metrics: new PerformanceMetric(Fps1Low: 45.0, AverageFps: 60.0));

        var postEvidence = new PerformanceEvidence(
            SceneSet: "city_loop_a",
            FixedSeed: 1337,
            RunMode: "headless",
            Metrics: new PerformanceMetric(Fps1Low: 48.0, AverageFps: 63.5));

        var service = new PerformanceEvidencePairingService();

        var result = service.Pair(baselineEvidence, postEvidence);

        result.IsPairable.Should().BeTrue();
        result.DeltaFps1Low.Should().Be(3.0);
        result.DeltaAverageFps.Should().Be(3.5);
    }

    [Fact]
    public void ShouldRejectPairing_WhenRunModeDiffers()
    {
        var baselineEvidence = new PerformanceEvidence(
            SceneSet: "city_loop_a",
            FixedSeed: 1337,
            RunMode: "headless",
            Metrics: new PerformanceMetric(Fps1Low: 45.0, AverageFps: 60.0));

        var postEvidence = new PerformanceEvidence(
            SceneSet: "city_loop_a",
            FixedSeed: 1337,
            RunMode: "playable",
            Metrics: new PerformanceMetric(Fps1Low: 46.0, AverageFps: 61.0));

        var service = new PerformanceEvidencePairingService();

        var result = service.Pair(baselineEvidence, postEvidence);

        result.IsPairable.Should().BeFalse();
        result.DeltaFps1Low.Should().BeNull();
        result.DeltaAverageFps.Should().BeNull();
    }

    [Fact]
    public void ShouldRejectPairing_WhenAnySideHasMissingMetrics()
    {
        var baselineEvidence = new PerformanceEvidence(
            SceneSet: "city_loop_a",
            FixedSeed: 1337,
            RunMode: "headless",
            Metrics: null);

        var postEvidence = new PerformanceEvidence(
            SceneSet: "city_loop_a",
            FixedSeed: 1337,
            RunMode: "headless",
            Metrics: new PerformanceMetric(Fps1Low: 46.0, AverageFps: 61.0));

        var service = new PerformanceEvidencePairingService();

        var result = service.Pair(baselineEvidence, postEvidence);

        result.IsPairable.Should().BeFalse();
        result.DeltaFps1Low.Should().BeNull();
        result.DeltaAverageFps.Should().BeNull();
    }
}
