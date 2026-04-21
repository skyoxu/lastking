using System;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class PerformanceComparisonIdentityTests
{
    // ACC:T30.13
    [Fact]
    public void ShouldFail_WhenRunModeDiffers()
    {
        var baseline = CreateBaseline();
        var post = baseline with { RunMode = "playable" };
        var evaluator = new PerformanceComparisonIdentityService();

        var verdict = evaluator.Evaluate(baseline, post);

        verdict.Should().Be(ComparisonVerdict.Fail);
    }

    [Fact]
    public void ShouldFail_WhenSceneSetDiffers()
    {
        var baseline = CreateBaseline();
        var post = baseline with { SceneSet = "benchmark_scene_set_b" };
        var evaluator = new PerformanceComparisonIdentityService();

        var verdict = evaluator.Evaluate(baseline, post);

        verdict.Should().Be(ComparisonVerdict.Fail);
    }

    [Fact]
    public void ShouldFail_WhenFixedSeedDiffers()
    {
        var baseline = CreateBaseline();
        var post = baseline with { FixedSeed = 2026 };
        var evaluator = new PerformanceComparisonIdentityService();

        var verdict = evaluator.Evaluate(baseline, post);

        verdict.Should().Be(ComparisonVerdict.Fail);
    }

    [Fact]
    public void ShouldFail_WhenCameraPathScriptDiffers()
    {
        var baseline = CreateBaseline();
        var post = baseline with { CameraPathScript = "camera_paths/benchmark_alt.path" };
        var evaluator = new PerformanceComparisonIdentityService();

        var verdict = evaluator.Evaluate(baseline, post);

        verdict.Should().Be(ComparisonVerdict.Fail);
    }

    [Fact]
    public void ShouldFail_WhenLaunchPresetDiffers()
    {
        var baseline = CreateBaseline();
        var post = baseline with { LaunchPreset = "ultra_quality" };
        var evaluator = new PerformanceComparisonIdentityService();

        var verdict = evaluator.Evaluate(baseline, post);

        verdict.Should().Be(ComparisonVerdict.Fail);
    }

    [Fact]
    public void ShouldPass_WhenAllIdentityDimensionsMatch()
    {
        var baseline = CreateBaseline();
        var post = CreateBaseline();
        var evaluator = new PerformanceComparisonIdentityService();

        var verdict = evaluator.Evaluate(baseline, post);

        verdict.Should().Be(ComparisonVerdict.Pass);
    }

    private static PerformanceRunIdentity CreateBaseline()
    {
        return new PerformanceRunIdentity(
            SceneSet: "benchmark_scene_set_a",
            FixedSeed: 1337,
            CameraPathScript: "camera_paths/benchmark_main.path",
            LaunchPreset: "performance_medium",
            RunMode: "headless");
    }

}
