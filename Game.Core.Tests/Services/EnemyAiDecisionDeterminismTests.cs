using System;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class EnemyAiDecisionDeterminismTests
{
    // ACC:T6.3
    [Fact]
    public void ShouldMatchApprovedBaseline_WhenInitialStateAndSeedAreUnchanged()
    {
        var sut = new EnemyAiDeterminismService();
        var state = EnemyAiDeterminismState.Create(
            new EnemyAiDeterminismCandidate("alpha", 10, IsPathBlocked: false),
            new EnemyAiDeterminismCandidate("bravo", 10, IsPathBlocked: false),
            new EnemyAiDeterminismCandidate("charlie", 8, IsPathBlocked: false));

        const int seed = 42;
        var baseline = new[] { "alpha", "bravo", "alpha", "bravo", "alpha", "bravo" };

        var actual = sut.RunLoop(state, seed, steps: baseline.Length);

        actual.Should().Equal(baseline);
    }

    // ACC:T6.5
    [Fact]
    public void ShouldResolveSameTarget_WhenEqualPriorityTieUsesSameSeedAndState()
    {
        var sut = new EnemyAiDeterminismService();
        var state = EnemyAiDeterminismState.Create(
            new EnemyAiDeterminismCandidate("alpha", 10, IsPathBlocked: false),
            new EnemyAiDeterminismCandidate("bravo", 10, IsPathBlocked: false));

        const int seed = 9;
        var first = sut.DecideTarget(state, seed, tick: 0);

        for (var i = 0; i < 10; i++)
        {
            var replay = sut.DecideTarget(state, seed, tick: 0);
            replay.Should().Be(first);
        }
    }

    // ACC:T6.14
    // ACC:T10.17
    [Fact]
    public void ShouldProduceIdenticalDecisionTrace_WhenTieAndBlockedPathFallbackAreRepeated()
    {
        var sut = new EnemyAiDeterminismService();
        var tieState = EnemyAiDeterminismState.Create(
            new EnemyAiDeterminismCandidate("alpha", 10, IsPathBlocked: false),
            new EnemyAiDeterminismCandidate("bravo", 10, IsPathBlocked: false),
            new EnemyAiDeterminismCandidate("charlie", 8, IsPathBlocked: false));

        var blockedFallbackState = EnemyAiDeterminismState.Create(
            new EnemyAiDeterminismCandidate("alpha", 10, IsPathBlocked: true),
            new EnemyAiDeterminismCandidate("bravo", 10, IsPathBlocked: true),
            new EnemyAiDeterminismCandidate("charlie", 7, IsPathBlocked: false));

        const int seed = 7;

        var firstTrace = sut.RunLoop(tieState, seed, steps: 4)
            .Concat(sut.RunLoop(blockedFallbackState, seed, steps: 4))
            .ToArray();

        var secondTrace = sut.RunLoop(tieState, seed, steps: 4)
            .Concat(sut.RunLoop(blockedFallbackState, seed, steps: 4))
            .ToArray();

        secondTrace.Should().Equal(firstTrace);
        secondTrace.Should().Contain(item => item.StartsWith("fallback:", StringComparison.Ordinal));
        secondTrace.Should().ContainInOrder("fallback:bravo", "fallback:alpha", "fallback:bravo", "fallback:alpha");
    }

    // ACC:T6.16
    [Fact]
    public void ShouldVerifyFixedSeedDeterminism_WhenEvaluatingObligationLockO9()
    {
        var sut = new EnemyAiDeterminismService();
        var state = EnemyAiDeterminismState.Create(
            new EnemyAiDeterminismCandidate("alpha", 10, IsPathBlocked: false),
            new EnemyAiDeterminismCandidate("bravo", 10, IsPathBlocked: true),
            new EnemyAiDeterminismCandidate("charlie", 9, IsPathBlocked: false));

        const int seed = 123;
        var expected = sut.RunLoop(state, seed, steps: 12);

        for (var run = 0; run < 25; run++)
        {
            var replay = sut.RunLoop(state, seed, steps: 12);
            replay.Should().Equal(expected);
        }
    }

    [Fact]
    public void ShouldNotMatchApprovedBaseline_WhenSeedChanges()
    {
        var sut = new EnemyAiDeterminismService();
        var state = EnemyAiDeterminismState.Create(
            new EnemyAiDeterminismCandidate("alpha", 10, IsPathBlocked: false),
            new EnemyAiDeterminismCandidate("bravo", 10, IsPathBlocked: false),
            new EnemyAiDeterminismCandidate("charlie", 8, IsPathBlocked: false));

        var approvedBaseline = sut.RunLoop(state, seed: 42, steps: 6);
        var drifted = sut.RunLoop(state, seed: 43, steps: 6);

        drifted.Should().NotEqual(approvedBaseline);
    }
}
