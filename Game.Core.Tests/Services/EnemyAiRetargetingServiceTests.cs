using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class EnemyAiRetargetingServiceTests
{
    // ACC:T6.18
    [Fact]
    public void ShouldSwitchToNextReachableTarget_WhenCurrentTargetBecomesUnreachable()
    {
        var sut = new EnemyAiRetargetingService();
        var candidates = new[]
        {
            new EnemyAiRetargetCandidate("boss", Priority: 100, IsReachable: false),
            new EnemyAiRetargetCandidate("archer", Priority: 90, IsReachable: true),
            new EnemyAiRetargetCandidate("mage", Priority: 80, IsReachable: true)
        };

        var selected = sut.SelectNextReachableTarget(candidates, currentTargetId: "boss");

        selected.Should().NotBeNull();
        selected!.TargetId.Should().Be("archer");
        selected.IsReachable.Should().BeTrue();
    }

    [Fact]
    public void ShouldPreferHighestPriorityReachableTarget_WhenMultipleTargetsAreReachable()
    {
        var sut = new EnemyAiRetargetingService();
        var candidates = new[]
        {
            new EnemyAiRetargetCandidate("tank", Priority: 40, IsReachable: true),
            new EnemyAiRetargetCandidate("healer", Priority: 60, IsReachable: true),
            new EnemyAiRetargetCandidate("dps", Priority: 50, IsReachable: true)
        };

        var selected = sut.SelectNextReachableTarget(candidates, currentTargetId: "missing");

        selected.Should().NotBeNull();
        selected!.TargetId.Should().Be("healer");
    }

    [Fact]
    public void ShouldReturnNull_WhenNoReachableTargetsExist()
    {
        var sut = new EnemyAiRetargetingService();
        var candidates = new[]
        {
            new EnemyAiRetargetCandidate("boss", Priority: 100, IsReachable: false),
            new EnemyAiRetargetCandidate("elite", Priority: 90, IsReachable: false)
        };

        var selected = sut.SelectNextReachableTarget(candidates, currentTargetId: "boss");

        selected.Should().BeNull();
    }
}
