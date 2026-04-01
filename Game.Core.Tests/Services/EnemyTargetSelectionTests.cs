using FluentAssertions;
using Game.Core.Services;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class EnemyTargetSelectionTests
{
    [Fact]
    public void ShouldSelectCastle_WhenCastleIsHighestReachableCombatTarget()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Reachable("castle-1", EnemyTargetClass.Castle, 2),
            EnemyAiTargetCandidate.Reachable("tower-1", EnemyTargetClass.ArmedDefense, 1),
            EnemyAiTargetCandidate.Reachable("wall-1", EnemyTargetClass.WallGate, 1)
        };

        var decision = sut.SelectTarget(candidates);

        decision.TargetClass.Should().Be(EnemyTargetClass.Castle);
        decision.TargetId.Should().Be("castle-1");
        decision.AttackEventTargetId.Should().Be("castle-1");
        decision.IsFallbackAttack.Should().BeFalse();
    }

    [Fact]
    public void ShouldNotSelectWallGate_WhenReachableCastleExists()
    {
        var sut = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Reachable("castle-1", EnemyTargetClass.Castle, 3),
            EnemyAiTargetCandidate.Reachable("wall-1", EnemyTargetClass.WallGate, 1)
        };

        var decision = sut.SelectTarget(candidates);

        decision.TargetClass.Should().Be(EnemyTargetClass.Castle);
        decision.TargetId.Should().NotBe("wall-1");
    }

    // ACC:T7.10
    [Fact]
    public void ShouldRouteCastleTargetAttackIntoCastleHpReduction_WhenCastleIsSelected()
    {
        var selector = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Reachable("castle-core", EnemyTargetClass.Castle, 2),
            EnemyAiTargetCandidate.Reachable("tower-1", EnemyTargetClass.ArmedDefense, 3)
        };
        var decision = selector.SelectTarget(candidates);
        var runtime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(StartHp: 12, CastleTargetId: "castle-core"),
            new GameStateMachine(),
            "run-7",
            1);

        var result = runtime.ResolveEnemyAttack(decision, 3);

        decision.TargetClass.Should().Be(EnemyTargetClass.Castle);
        decision.AttackEventTargetId.Should().Be("castle-core");
        result.AttackApplied.Should().BeTrue();
        result.CastleHpChangedEvent!.CurrentHp.Should().Be(9);
        runtime.CurrentHp.Should().Be(9);
    }
}
