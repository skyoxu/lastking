using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class SteamRuntimeStartupValidationTests
{
    // ACC:T21.17
    [Fact]
    [Trait("acceptance", "ACC:T21.17")]
    public void ShouldPassValidation_WhenSteamRuntimeLaunchIsPresentAndStartupReachesReadyState()
    {
        var launches = new[]
        {
            Task21StartupLaunchEvidence.WindowsSteamRuntimeStub(
                runId: "run-001",
                startupReachedReadyState: true,
                hasSteamBlockingIntegrationErrors: false,
                steamRuntimeChecksExecuted: true)
        };

        var result = SteamRuntimeStartupValidator.Validate(launches);

        result.IsAccepted.Should().BeTrue("Steam launch stub is an allowed Steam runtime context");
        result.FailureReason.Should().BeNull();
    }

    // ACC:T21.2
    [Fact]
    [Trait("acceptance", "ACC:T21.2")]
    public void ShouldFailValidation_WhenSteamRuntimeLaunchChecksAreSkipped()
    {
        var launches = new[]
        {
            Task21StartupLaunchEvidence.WindowsSteamRuntimeRealAppId(
                runId: "run-002",
                steamAppId: "480",
                startupReachedReadyState: true,
                hasSteamBlockingIntegrationErrors: false,
                steamRuntimeChecksExecuted: false)
        };

        var result = SteamRuntimeStartupValidator.Validate(launches);

        result.IsAccepted.Should().BeFalse();
        result.FailureReason.Should().Be("steam runtime launch checks were skipped");
    }

    [Fact]
    public void ShouldFailValidation_WhenNoWindowsSteamRuntimeLaunchReachesStartupWithoutBlockingErrors()
    {
        var launches = new[]
        {
            Task21StartupLaunchEvidence.WindowsSteamRuntimeRealAppId(
                runId: "run-003",
                steamAppId: "480",
                startupReachedReadyState: false,
                hasSteamBlockingIntegrationErrors: false,
                steamRuntimeChecksExecuted: true),
            Task21StartupLaunchEvidence.WindowsSteamRuntimeRealAppId(
                runId: "run-004",
                steamAppId: "480",
                startupReachedReadyState: true,
                hasSteamBlockingIntegrationErrors: true,
                steamRuntimeChecksExecuted: true)
        };

        var result = SteamRuntimeStartupValidator.Validate(launches);

        result.IsAccepted.Should().BeFalse();
        result.FailureReason.Should().Be("no qualifying Steam runtime startup launch was found");
    }

}
