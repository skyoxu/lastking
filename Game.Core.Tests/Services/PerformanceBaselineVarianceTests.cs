using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class PerformanceBaselineVarianceTests
{
    // ACC:T30.4
    [Fact]
    public void ShouldRejectBaseline_WhenRepeatRunsExceedDeclaredVarianceWindow()
    {
        var profile = new BaselineCaptureProfile(
            CameraPathId: "locked_camera_path_city_loop",
            SessionScriptId: "session_script_perf_smoke",
            VarianceWindowPercent: 2.0,
            RunA: new PerformanceMetric(Fps1Low: 45.0, AverageFps: 60.0),
            RunB: new PerformanceMetric(Fps1Low: 40.0, AverageFps: 54.0));

        var service = new PerformanceBaselineVarianceService();

        var isDeterministic = service.IsDeterministic(profile);

        isDeterministic.Should().BeFalse("two repeated runs must remain inside the declared variance window");
    }

    [Fact]
    public void ShouldAcceptBaseline_WhenRepeatRunsStayWithinDeclaredVarianceWindow()
    {
        var profile = new BaselineCaptureProfile(
            CameraPathId: "locked_camera_path_city_loop",
            SessionScriptId: "session_script_perf_smoke",
            VarianceWindowPercent: 2.0,
            RunA: new PerformanceMetric(Fps1Low: 45.0, AverageFps: 60.0),
            RunB: new PerformanceMetric(Fps1Low: 44.4, AverageFps: 59.2));

        var service = new PerformanceBaselineVarianceService();

        var isDeterministic = service.IsDeterministic(profile);

        isDeterministic.Should().BeTrue();
    }

    [Fact]
    public void ShouldRejectBaseline_WhenCaptureIsNotLocked()
    {
        var profile = new BaselineCaptureProfile(
            CameraPathId: "",
            SessionScriptId: "session_script_perf_smoke",
            VarianceWindowPercent: 2.0,
            RunA: new PerformanceMetric(Fps1Low: 45.0, AverageFps: 60.0),
            RunB: new PerformanceMetric(Fps1Low: 45.0, AverageFps: 60.0));

        var service = new PerformanceBaselineVarianceService();

        var isDeterministic = service.IsDeterministic(profile);

        isDeterministic.Should().BeFalse("baseline capture must use a locked camera path and scripted session");
    }
}
