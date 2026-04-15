using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class ProjectSettingsBaselineTests
{
    // ACC:T21.9
    [Fact]
    [Trait("acceptance", "ACC:T21.9")]
    public void ShouldMatchLockedNameAndMainScene_WhenValidatingTask21ReusedBaseline()
    {
        var baseline = BuildReusedBaselineForTask21Validation();

        var isAccepted = ProjectSettingsBaselineValidator.IsNameAndMainSceneLocked(baseline);

        isAccepted.Should().BeTrue(
            "Task 21 locks application/config/name and application/run/main_scene on the reused baseline.");
    }

    [Fact]
    public void ShouldFailAcceptance_WhenNameOrMainSceneDeviatesFromLockedValues()
    {
        var deviatedBaseline = new ProjectSettingsBaselineSnapshot(
            "LASTKING-DEV",
            "res://scenes/Main.tscn",
            1920,
            1080);

        var isAccepted = ProjectSettingsBaselineValidator.IsNameAndMainSceneLocked(deviatedBaseline);

        isAccepted.Should().BeFalse(
            "any deviation must fail acceptance for the locked name/main_scene baseline.");
    }

    // ACC:T21.10
    [Fact]
    [Trait("acceptance", "ACC:T21.10")]
    public void ShouldMatchLockedWindowResolution_WhenValidatingTask21ReusedBaseline()
    {
        var baseline = BuildReusedBaselineForTask21Validation();

        var isAccepted = ProjectSettingsBaselineValidator.IsWindowResolutionLocked(baseline);

        isAccepted.Should().BeTrue(
            "Task 21 locks display/window/size/width and display/window/size/height on the reused baseline.");
    }

    [Fact]
    public void ShouldFailAcceptance_WhenWindowResolutionDeviatesFromLockedValues()
    {
        var deviatedBaseline = new ProjectSettingsBaselineSnapshot(
            "LASTKING",
            "res://scenes/Main.tscn",
            1919,
            1080);

        var isAccepted = ProjectSettingsBaselineValidator.IsWindowResolutionLocked(deviatedBaseline);

        isAccepted.Should().BeFalse(
            "any deviation must fail acceptance for the locked window size baseline.");
    }

    private static ProjectSettingsBaselineSnapshot BuildReusedBaselineForTask21Validation()
    {
        return new ProjectSettingsBaselineSnapshot(
            "LASTKING",
            "res://scenes/Main.tscn",
            1920,
            1080);
    }
}
