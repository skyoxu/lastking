using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class EnemyAiTargetingServiceTests
{
    // ACC:T47.1
    // ACC:T47.5
    [Fact]
    public void ShouldSelectLowestPathCost_WhenCandidatesSharePriority()
    {
        var sut = new EnemyAiTargetingService();
        var candidates = new[]
        {
            new EnemyAiPriorityCandidate("alpha", Priority: 2, PathCost: 5),
            new EnemyAiPriorityCandidate("bravo", Priority: 2, PathCost: 2),
            new EnemyAiPriorityCandidate("charlie", Priority: 1, PathCost: 0)
        };

        var selected = sut.SelectTargetId(candidates, seed: 42);

        selected.Should().Be("bravo");
    }

    // ACC:T6.17
    [Fact]
    public void ShouldReturnIdenticalTargetAcrossRuns_WhenEqualCostTieBreakUsesFixedSeed()
    {
        var sut = new EnemyAiTargetingService();
        var candidates = new[]
        {
            new EnemyAiPriorityCandidate("alpha", Priority: 3, PathCost: 4),
            new EnemyAiPriorityCandidate("bravo", Priority: 3, PathCost: 4),
            new EnemyAiPriorityCandidate("charlie", Priority: 2, PathCost: 1)
        };

        var results = Enumerable.Range(0, 30)
            .Select(_ => sut.SelectTargetId(candidates, seed: 1337))
            .ToList();

        results.Distinct().Should().ContainSingle();
        results[0].Should().BeOneOf("alpha", "bravo");
    }

    // ACC:T47.3
    // ACC:T47.6
    [Fact]
    public void ShouldIgnoreUnreachableCandidates_WhenSelectingTarget()
    {
        var sut = new EnemyAiTargetingService();
        var candidates = new[]
        {
            new EnemyAiPriorityCandidate("alpha", Priority: 5, PathCost: -1),
            new EnemyAiPriorityCandidate("bravo", Priority: 5, PathCost: 7),
            new EnemyAiPriorityCandidate("charlie", Priority: 4, PathCost: 1)
        };

        var selected = sut.SelectTargetId(candidates, seed: 99);

        selected.Should().Be("bravo");
    }

    // ACC:T47.7
    [Fact]
    public void ShouldReturnStableNoTarget_WhenNoReachableCandidatesExist()
    {
        var sut = new EnemyAiTargetingService();
        var candidates = new[]
        {
            new EnemyAiPriorityCandidate("alpha", Priority: 5, PathCost: -1),
            new EnemyAiPriorityCandidate("bravo", Priority: 4, PathCost: -1)
        };

        var first = sut.SelectTargetId(candidates, seed: 11);
        var second = sut.SelectTargetId(candidates, seed: 999);

        first.Should().BeEmpty();
        second.Should().BeEmpty();
    }
}
