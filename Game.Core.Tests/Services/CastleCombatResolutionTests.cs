using FluentAssertions;
using Game.Core.Services;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class CastleCombatResolutionTests
{
    // ACC:T7.2
    [Fact]
    public void ShouldResolveCastleHpReductionAndLossOnTheSameCombatPath_WhenRuntimeProcessesCastleHits()
    {
        var runtime = CreateRuntime(startHp: 10, out var flow);

        var firstHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            4);
        var secondHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            6);

        firstHit.AttackApplied.Should().BeTrue();
        firstHit.EnteredGameOver.Should().BeFalse();
        firstHit.CastleHpChangedEvent!.CurrentHp.Should().Be(6);

        secondHit.AttackApplied.Should().BeTrue();
        secondHit.EnteredGameOver.Should().BeTrue();
        secondHit.CastleHpChangedEvent!.CurrentHp.Should().Be(0);
        runtime.CurrentHp.Should().Be(0);
        flow.State.Should().Be(GameFlowState.GameOver);
    }

    // ACC:T7.3
    [Fact]
    public void ShouldKeepCombatRunning_WhenCastleHpRemainsAboveZeroAfterResolvedDamage()
    {
        var runtime = CreateRuntime(startHp: 10, out var flow);

        var firstHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            4);
        var secondHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            1);

        firstHit.AttackApplied.Should().BeTrue();
        firstHit.EnteredGameOver.Should().BeFalse();
        secondHit.AttackApplied.Should().BeTrue();
        secondHit.EnteredGameOver.Should().BeFalse();
        runtime.CurrentHp.Should().Be(5);
        flow.State.Should().Be(GameFlowState.Running);
    }

    // ACC:T7.5
    [Fact]
    public void ShouldDecreaseCurrentHpByResolvedDamage_WhenCastleAttackApplies()
    {
        var runtime = CreateRuntime(startHp: 12, out _);

        var result = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            3);

        result.AttackApplied.Should().BeTrue();
        result.CastleHpChangedEvent!.PreviousHp.Should().Be(12);
        result.CastleHpChangedEvent.CurrentHp.Should().Be(9);
        runtime.CurrentHp.Should().Be(9);
    }

    // ACC:T7.6
    [Fact]
    public void ShouldKeepCurrentHpUnchanged_WhenAttackDoesNotApplyToCastle()
    {
        var runtime = CreateRuntime(startHp: 12, out _);

        var nullTarget = runtime.ResolveEnemyAttack(EnemyAiTargetDecision.Idle, 3);
        var wrongTarget = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("tower", false, "tower", EnemyTargetClass.ArmedDefense),
            3);
        var zeroDamage = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            0);

        nullTarget.AttackApplied.Should().BeFalse();
        wrongTarget.AttackApplied.Should().BeFalse();
        zeroDamage.AttackApplied.Should().BeFalse();
        runtime.CurrentHp.Should().Be(12);
    }

    // ACC:T7.7
    [Fact]
    public void ShouldEnterGameOverImmediately_WhenResolvedDamageDropsHpToZero()
    {
        var runtime = CreateRuntime(startHp: 5, out var flow);

        var result = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            5);

        result.AttackApplied.Should().BeTrue();
        result.EnteredGameOver.Should().BeTrue();
        runtime.CurrentHp.Should().Be(0);
        flow.State.Should().Be(GameFlowState.GameOver);
    }

    // ACC:T7.8
    [Fact]
    public void ShouldRejectFurtherDamage_WhenBattleIsAlreadyInGameOver()
    {
        var runtime = CreateRuntime(startHp: 3, out var flow);

        var terminalHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            3);
        var repeatedHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            1);

        terminalHit.AttackApplied.Should().BeTrue();
        terminalHit.EnteredGameOver.Should().BeTrue();
        repeatedHit.AttackApplied.Should().BeFalse();
        repeatedHit.EnteredGameOver.Should().BeFalse();
        repeatedHit.CastleHpChangedEvent.Should().BeNull();
        runtime.CurrentHp.Should().Be(0);
        flow.State.Should().Be(GameFlowState.GameOver);
    }

    // ACC:T7.11
    [Theory]
    [InlineData(6, 5, 1, false)]
    [InlineData(6, 6, 0, true)]
    public void ShouldTriggerLossOnlyAtZeroThreshold_WhenResolvedDamageIsApplied(
        int startHp,
        int damage,
        int expectedHp,
        bool expectedGameOver)
    {
        var runtime = CreateRuntime(startHp, out var flow);

        var result = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            damage);

        result.AttackApplied.Should().BeTrue();
        result.EnteredGameOver.Should().Be(expectedGameOver);
        runtime.CurrentHp.Should().Be(expectedHp);
        flow.State.Should().Be(expectedGameOver ? GameFlowState.GameOver : GameFlowState.Running);
    }

    // ACC:T7.16
    [Fact]
    public void ShouldMutateCurrentHpOnly_WhenCastleInitializationOrDamageApplies()
    {
        var runtime = CreateRuntime(startHp: 9, out _);

        runtime.CurrentHp.Should().Be(9);

        runtime.ResolveEnemyAttack(EnemyAiTargetDecision.Idle, 3).AttackApplied.Should().BeFalse();
        runtime.CurrentHp.Should().Be(9);

        runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("wall", false, "wall", EnemyTargetClass.WallGate),
            3).AttackApplied.Should().BeFalse();
        runtime.CurrentHp.Should().Be(9);

        runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            4).AttackApplied.Should().BeTrue();
        runtime.CurrentHp.Should().Be(5);
    }

    private static CastleBattleRuntime CreateRuntime(int startHp, out GameStateMachine flow)
    {
        flow = new GameStateMachine();
        return CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(startHp),
            flow,
            "run-7",
            1);
    }
}
