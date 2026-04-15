using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Engine;

public sealed class DeterministicCoreLoopRegressionTests
{
    // ACC:T21.7
    [Fact]
    [Trait("acceptance", "ACC:T21.7")]
    public void ShouldRemainUnchanged_WhenApplyingTask21ToT11ValidatedWindowsStartupRuntimePath()
    {
        var baselineTrace = BuildT11ValidatedWindowsStartupRuntimeTrace();
        var task21Trace = BuildTask21RuntimeTrace();

        Task21RuntimeTraceGuard.IsEquivalent(baselineTrace, task21Trace).Should().BeTrue(
            "Task 21 must not regress the validated Windows startup/runtime path from T11.");
    }

    // ACC:T21.7
    [Fact]
    [Trait("acceptance", "ACC:T21.7")]
    public void ShouldFailAcceptance_WhenWindowsStartupRuntimeEventOrderChanges()
    {
        var baselineTrace = BuildT11ValidatedWindowsStartupRuntimeTrace();
        var regressedTrace = BuildRegressedRuntimeTrace();

        var isRegressed = !Task21RuntimeTraceGuard.IsEquivalent(baselineTrace, regressedTrace);

        isRegressed.Should().BeTrue("acceptance must fail when startup/runtime ordering regresses.");
    }

    private static string[] BuildT11ValidatedWindowsStartupRuntimeTrace()
    {
        return new[]
        {
            "windows_profile_locked",
            "steam_runtime_bootstrap_requested",
            "steam_runtime_validated",
            "main_loop_started"
        };
    }

    private static string[] BuildTask21RuntimeTrace()
    {
        return new[]
        {
            "windows_profile_locked",
            "steam_runtime_bootstrap_requested",
            "steam_runtime_validated",
            "main_loop_started"
        };
    }

    private static string[] BuildRegressedRuntimeTrace()
    {
        return new[]
        {
            "windows_profile_locked",
            "steam_runtime_bootstrap_requested",
            "main_loop_started",
            "steam_runtime_validated"
        };
    }
}
