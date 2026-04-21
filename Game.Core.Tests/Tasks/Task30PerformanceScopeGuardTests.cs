using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task30PerformanceScopeGuardTests
{
    // ACC:T30.1
    [Fact]
    public void ShouldRejectCompletion_WhenEvidenceContainsOutOfScopeChange()
    {
        var guard = new Task30CompletionScopeGuardService();
        var evidence = new[]
        {
            new ChangeEvidence("PRD:T30:PerformanceTargets", "profiling-summary.md"),
            new ChangeEvidence("PRD:T12:UITheme", "main-menu-colors.md")
        };

        var decision = guard.Evaluate(evidence);

        decision.IsAccepted.Should().BeFalse("completion must stay limited to the PRD-locked scope for Task 30");
        decision.Reason.Should().Contain("out-of-scope");
    }

    [Fact]
    public void ShouldAcceptCompletion_WhenAllEvidenceIsWithinPrdLockedScope()
    {
        var guard = new Task30CompletionScopeGuardService();
        var evidence = new[]
        {
            new ChangeEvidence("PRD:T30:PerformanceTargets", "frame-budget.md"),
            new ChangeEvidence("PRD:T30:PerformanceTargets", "benchmark-notes.md")
        };

        var decision = guard.Evaluate(evidence);

        decision.IsAccepted.Should().BeTrue();
        decision.Reason.Should().Be("accepted");
    }
}
