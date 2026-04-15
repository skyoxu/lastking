using System;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class WindowsExportProfileLockTests
{
    // ACC:T21.5
    [Fact]
    public void ShouldRejectAcceptance_WhenUnrelatedScopeChangesArePresent()
    {
        var profile = ExportProfile(
            WindowsDesktop64Enabled: true,
            SteamAppId: "480",
            LinuxEnabled: false,
            MacOsEnabled: false,
            WebEnabled: false,
            AndroidEnabled: false,
            IosEnabled: false);

        var changedScopes = new[] { "windows.export.profile", "ui.theme" };
        var validator = new WindowsExportProfileLockValidator();

        var result = validator.Validate(profile, changedScopes);

        result.IsAccepted.Should().BeFalse("Task #21 must fail acceptance when unrelated scope changes are present.");
        result.Errors.Should().Contain(error => error.Contains("scope", StringComparison.OrdinalIgnoreCase));
    }

    // ACC:T21.11
    [Theory]
    [InlineData(false, "480")]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    public void ShouldRejectContract_WhenWindowsDesktop64OrSteamAppIdIsMissing(bool windowsDesktop64Enabled, string steamAppId)
    {
        var profile = ExportProfile(
            WindowsDesktop64Enabled: windowsDesktop64Enabled,
            SteamAppId: steamAppId,
            LinuxEnabled: false,
            MacOsEnabled: false,
            WebEnabled: false,
            AndroidEnabled: false,
            IosEnabled: false);

        var changedScopes = new[] { "windows.export.profile" };
        var validator = new WindowsExportProfileLockValidator();

        var result = validator.Validate(profile, changedScopes);

        result.IsAccepted.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    // ACC:T21.12
    [Theory]
    [InlineData("linux")]
    [InlineData("macos")]
    [InlineData("web")]
    [InlineData("android")]
    [InlineData("ios")]
    public void ShouldRejectContract_WhenAnyNonWindowsTargetIsEnabled(string nonWindowsTarget)
    {
        var profile = BuildProfileWithEnabledNonWindowsTarget(nonWindowsTarget);
        var changedScopes = new[] { "windows.export.profile" };
        var validator = new WindowsExportProfileLockValidator();

        var result = validator.Validate(profile, changedScopes);

        result.IsAccepted.Should().BeFalse("locked export profile must omit or disable every non-Windows target.");
        result.Errors.Should().Contain(error => error.Contains("non-Windows", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShouldAcceptContract_WhenWindowsDesktop64OnlyAndSteamAppIdPresent()
    {
        var profile = ExportProfile(
            WindowsDesktop64Enabled: true,
            SteamAppId: "480",
            LinuxEnabled: false,
            MacOsEnabled: false,
            WebEnabled: false,
            AndroidEnabled: false,
            IosEnabled: false);

        var changedScopes = new[] { "windows.export.profile" };
        var validator = new WindowsExportProfileLockValidator();

        var result = validator.Validate(profile, changedScopes);

        result.IsAccepted.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    private static WindowsExportProfileContract BuildProfileWithEnabledNonWindowsTarget(string nonWindowsTarget)
    {
        return nonWindowsTarget switch
        {
            "linux" => new WindowsExportProfileContract(true, "480", true, false, false, false, false),
            "macos" => new WindowsExportProfileContract(true, "480", false, true, false, false, false),
            "web" => new WindowsExportProfileContract(true, "480", false, false, true, false, false),
            "android" => new WindowsExportProfileContract(true, "480", false, false, false, true, false),
            "ios" => new WindowsExportProfileContract(true, "480", false, false, false, false, true),
            _ => throw new ArgumentOutOfRangeException(nameof(nonWindowsTarget), nonWindowsTarget, "Unknown non-Windows target.")
        };
    }
    
    private static WindowsExportProfileContract ExportProfile(
        bool WindowsDesktop64Enabled,
        string SteamAppId,
        bool LinuxEnabled,
        bool MacOsEnabled,
        bool WebEnabled,
        bool AndroidEnabled,
        bool IosEnabled)
    {
        return new WindowsExportProfileContract(
            WindowsDesktop64Enabled,
            SteamAppId,
            LinuxEnabled,
            MacOsEnabled,
            WebEnabled,
            AndroidEnabled,
            IosEnabled);
    }
}
