using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public class EnemyAiTargetSelectionTests
{
    // ACC:T6.4
    [Fact]
    public void ShouldSelectUnit_WhenUnitAndOtherReachableTargetsExist()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Reachable("castle-1", EnemyTargetClass.Castle, 1),
            EnemyAiTargetCandidate.Reachable("unit-1", EnemyTargetClass.Unit, 3),
            EnemyAiTargetCandidate.Reachable("armed-1", EnemyTargetClass.ArmedDefense, 2),
            EnemyAiTargetCandidate.Reachable("wall-1", EnemyTargetClass.WallGate, 4)
        };

        var decision = sut.SelectTarget(candidates);

        decision.TargetClass.Should().Be(EnemyTargetClass.Unit);
        decision.TargetId.Should().Be("unit-1");
    }

    // ACC:T6.13
    [Fact]
    public void ShouldExcludeUnreachableHigherPriority_WhenReachableLowerPriorityExists()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Reachable("castle-1", EnemyTargetClass.Castle, 5),
            EnemyAiTargetCandidate.Reachable("wall-1", EnemyTargetClass.WallGate, 1)
        };

        var decision = sut.SelectTarget(candidates);

        decision.TargetClass.Should().Be(EnemyTargetClass.Castle);
        decision.TargetId.Should().Be("castle-1");
    }

    // ACC:T6.10
    [Fact]
    public void ShouldNotSelectLowerPriority_WhenHigherPriorityReachableInSameDecisionCycle()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Reachable("unit-1", EnemyTargetClass.Unit, 8),
            EnemyAiTargetCandidate.Reachable("castle-1", EnemyTargetClass.Castle, 2)
        };

        var decision = sut.SelectTarget(candidates);

        decision.TargetClass.Should().Be(EnemyTargetClass.Unit);
        decision.TargetId.Should().Be("unit-1");
    }

    // ACC:T6.6
    [Fact]
    public void ShouldSelectNearestBlockingStructure_WhenHigherPriorityTargetsAreBlocked()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Blocker("blocker-near", 2),
            EnemyAiTargetCandidate.Blocker("blocker-far", 6),
            EnemyAiTargetCandidate.Reachable("decor-1", EnemyTargetClass.Decoration, 1)
        };

        var decision = sut.SelectTarget(candidates);

        decision.IsFallbackAttack.Should().BeTrue();
        decision.TargetId.Should().Be("blocker-near");
        decision.TargetClass.Should().Be(EnemyTargetClass.BlockingStructure);
    }

    // ACC:T6.11
    [Fact]
    public void ShouldEmitAttackEventToBlockingStructure_WhenFallbackAttackTriggered()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Blocker("gate-1", 4)
        };

        var decision = sut.SelectTarget(candidates);

        decision.IsFallbackAttack.Should().BeTrue();
        decision.TargetId.Should().Be("gate-1");
        decision.AttackEventTargetId.Should().Be("gate-1");
    }

    // ACC:T6.1
    [Fact]
    public void ShouldExecuteFallbackAttack_WhenBlockedRouteAndMustNotSwitchToUnrelatedTarget()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Blocker("wall-2", 3),
            EnemyAiTargetCandidate.Reachable("decoration-1", EnemyTargetClass.Decoration, 1)
        };

        var decision = sut.SelectTarget(candidates);

        decision.Should().NotBeNull();
        decision.TargetId.Should().NotBe("decoration-1");
        decision.IsFallbackAttack.Should().BeTrue();
    }

    // ACC:T6.20
    [Fact]
    public void ShouldNotEnterFallback_WhenHigherPriorityReachableTargetExists()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Reachable("unit-1", EnemyTargetClass.Unit, 4),
            EnemyAiTargetCandidate.Blocker("blocker-near", 1),
            EnemyAiTargetCandidate.Blocker("blocker-far", 2)
        };

        var decision = sut.SelectTarget(candidates);

        decision.TargetId.Should().Be("unit-1");
        decision.IsFallbackAttack.Should().BeFalse();
    }

    // ACC:T10.6
    [Fact]
    public void ShouldStayResponsiveUnderBlockedPathAndRecover_WhenRouteBecomesReachableAgain()
    {
        var sut = new EnemyAiTargetSelector();
        var blockedCandidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Blocker("wall-1", 2),
        };

        for (var tick = 0; tick < 200; tick++)
        {
            var blockedDecision = sut.SelectTarget(blockedCandidates);
            blockedDecision.IsFallbackAttack.Should().BeTrue();
            blockedDecision.TargetClass.Should().Be(EnemyTargetClass.BlockingStructure);
            blockedDecision.TargetId.Should().Be("wall-1");
        }

        var recoveredCandidates = new[]
        {
            EnemyAiTargetCandidate.Reachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Blocker("wall-1", 2),
        };
        var recoveredDecision = sut.SelectTarget(recoveredCandidates);

        recoveredDecision.IsFallbackAttack.Should().BeFalse();
        recoveredDecision.TargetClass.Should().Be(EnemyTargetClass.Unit);
        recoveredDecision.TargetId.Should().Be("unit-1");
    }

    [Fact]
    public void ShouldStayIdle_WhenOnlyBlockersExistWithoutBlockedHigherPriorityTargets()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Blocker("blocker-near", 1),
            EnemyAiTargetCandidate.Blocker("blocker-far", 2)
        };

        var decision = sut.SelectTarget(candidates);

        decision.Should().Be(EnemyAiTargetDecision.Idle);
    }

}
