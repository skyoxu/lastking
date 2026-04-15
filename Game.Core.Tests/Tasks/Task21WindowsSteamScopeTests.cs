using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Tasks;

public class Task21WindowsSteamScopeTests
{
    // ACC:T21.16
    [Fact]
    public void ShouldFailAcceptance_WhenUnrelatedScopeChangesAreIncluded()
    {
        var request = BuildRequest(
            baselineId: "T11",
            validatesWindowsExportProfile: true,
            validatesSteamStartupPath: true,
            rebootstrapProject: false,
            touchesUnrelatedScope: true);

        var result = Task21ScopeGuard.Validate(request);

        result.IsAccepted.Should().BeFalse("Task 21 must fail acceptance when unrelated scope changes are present.");
    }

    // ACC:T21.5
    [Fact]
    public void ShouldRejectValidation_WhenFlowRequestsProjectRebootstrap()
    {
        var request = BuildRequest(
            baselineId: "T11",
            validatesWindowsExportProfile: true,
            validatesSteamStartupPath: true,
            rebootstrapProject: true,
            touchesUnrelatedScope: false);

        var result = Task21ScopeGuard.Validate(request);

        result.IsAccepted.Should().BeFalse();
        result.Reason.Should().Be("Project re-bootstrap is out of scope for Task 21.");
    }

    [Fact]
    public void ShouldAcceptValidation_WhenUsingT11BaselineWithWindowsAndSteamOnly()
    {
        var request = BuildRequest(
            baselineId: "T11",
            validatesWindowsExportProfile: true,
            validatesSteamStartupPath: true,
            rebootstrapProject: false,
            touchesUnrelatedScope: false);

        var result = Task21ScopeGuard.Validate(request);

        result.IsAccepted.Should().BeTrue();
    }

    private static Task21ScopeValidationRequest BuildRequest(
        string baselineId,
        bool validatesWindowsExportProfile,
        bool validatesSteamStartupPath,
        bool rebootstrapProject,
        bool touchesUnrelatedScope)
    {
        return new Task21ScopeValidationRequest(
            BaselineId: baselineId,
            ValidatesWindowsExportProfile: validatesWindowsExportProfile,
            ValidatesSteamStartupPath: validatesSteamStartupPath,
            RebootstrapProject: rebootstrapProject,
            TouchesUnrelatedScope: touchesUnrelatedScope);
    }
}
