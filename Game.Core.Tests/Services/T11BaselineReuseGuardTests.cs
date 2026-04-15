using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class T11BaselineReuseGuardTests
{
    // ACC:T21.1
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Trait("acceptance", "ACC:T21.1")]
    public void ShouldRejectValidation_WhenFlowCreatesOrInitializesNewProject(bool createsNewProject, bool initializesProjectScaffold)
    {
        var request = BuildValidTask21Request() with
        {
            CreatesNewProject = createsNewProject,
            InitializesProjectScaffold = initializesProjectScaffold
        };

        var result = T11BaselineReuseGuard.Validate(request);

        result.IsAccepted.Should().BeFalse();
        result.FailureReason.Should().Be("project re-initialization is forbidden for Task 21 validation");
    }

    // ACC:T21.13
    [Fact]
    [Trait("acceptance", "ACC:T21.13")]
    public void ShouldAcceptValidation_WhenReusedT11RootContainsAllRequiredBaselineFolders()
    {
        var request = BuildValidTask21Request();

        var result = T11BaselineReuseGuard.Validate(request);

        result.IsAccepted.Should().BeTrue("all required baseline folders exist on the reused T11 root");
        result.FailureReason.Should().BeNull();
    }

    // ACC:T21.16
    [Fact]
    [Trait("acceptance", "ACC:T21.16")]
    public void ShouldRejectValidation_WhenFlowRebootstrapsProjectOutsideTask21Scope()
    {
        var request = BuildValidTask21Request() with { RebootstrapsProject = true };

        var result = T11BaselineReuseGuard.Validate(request);

        result.IsAccepted.Should().BeFalse();
        result.FailureReason.Should().Be("scope violation: project bootstrap is out of Task 21 validation scope");
    }

    private static Task21BaselineValidationRequest BuildValidTask21Request()
    {
        return new Task21BaselineValidationRequest(
            ProjectRootName: "T11",
            ReusesExistingT11Root: true,
            CreatesNewProject: false,
            InitializesProjectScaffold: false,
            RebootstrapsProject: false,
            ValidatesWindowsExport: true,
            ValidatesSteamRuntime: true,
            ExistingFolders: new[]
            {
                "scripts",
                "scenes",
                "configs",
                "saves",
                "assets",
                "ui",
                "audio"
            });
    }

}
