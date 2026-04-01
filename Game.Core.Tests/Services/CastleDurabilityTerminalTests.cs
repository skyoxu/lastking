using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Game.Core.Contracts.Lastking;
using Game.Core.Services;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class CastleDurabilityTerminalTests
{
    // ACC:T7.18
    // ACC:T7.19
    [Fact]
    public void ShouldWriteReplayableTerminalEvidenceAndFailValidation_WhenArtifactPresenceChanges()
    {
        var first = CreateRuntime(startHp: 9, out var firstFlow);
        var second = CreateRuntime(startHp: 9, out var secondFlow);
        var attackSequence = new[] { 2, 3, 4 };
        var logsRoot = Path.Combine(FindRepositoryRoot(), "logs");
        var writer = new CastleTerminalEvidenceWriter(logsRoot);
        var evidenceDir = Path.Combine(logsRoot, "unit", "castle-terminal-evidence");
        var evidencePath = Path.Combine(evidenceDir, "run-7-terminal-castle-terminal-evidence.json");

        if (File.Exists(evidencePath))
        {
            File.Delete(evidencePath);
        }

        var firstTrace = attackSequence
            .Select(damage => first.ResolveEnemyAttack(CreateCastleDecision(first), damage))
            .Select(ToTracePoint)
            .ToArray();
        var secondTrace = attackSequence
            .Select(damage => second.ResolveEnemyAttack(CreateCastleDecision(second), damage))
            .Select(ToTracePoint)
            .ToArray();

        firstTrace.Should().Equal(secondTrace);
        first.CurrentHp.Should().Be(0);
        second.CurrentHp.Should().Be(0);
        firstFlow.State.Should().Be(GameFlowState.GameOver);
        secondFlow.State.Should().Be(GameFlowState.GameOver);

        var evidenceRuntime = CreateRuntime(startHp: 9, out var evidenceFlow);
        var hpTrace = attackSequence
            .Select(damage => evidenceRuntime.ResolveEnemyAttack(CreateCastleDecision(evidenceRuntime), damage).CastleHpChangedEvent)
            .OfType<CastleHpChanged>()
            .ToArray();

        writer.Write(evidenceDir, "run-7-terminal", 1, evidenceFlow.State, evidenceRuntime.CurrentHp, hpTrace);
        File.Exists(evidencePath).Should().BeTrue();
        writer.Validate(evidencePath, "run-7-terminal").Should().BeTrue();

        File.Delete(evidencePath);
        writer.Validate(evidencePath, "run-7-terminal").Should().BeFalse();
    }

    [Fact]
    public void ShouldRejectWriteOutsideLogsRoot_WhenEvidenceDirectoryEscapesHostSafeBoundary()
    {
        var logsRoot = Path.Combine(FindRepositoryRoot(), "logs");
        var unsafeDirectory = Path.GetFullPath(Path.Combine(logsRoot, "..", "outside-terminal-evidence"));
        var writer = new CastleTerminalEvidenceWriter(logsRoot);

        var act = () => writer.Write(
            unsafeDirectory,
            "run-7-terminal",
            1,
            GameFlowState.GameOver,
            0,
            Array.Empty<CastleHpChanged>());

        act.Should().Throw<InvalidDataException>();
    }

    [Fact]
    public void ShouldRejectValidateOutsideLogsRoot_WhenEvidencePathEscapesHostSafeBoundary()
    {
        var logsRoot = Path.Combine(FindRepositoryRoot(), "logs");
        var unsafeEvidencePath = Path.GetFullPath(Path.Combine(logsRoot, "..", "outside-terminal-evidence.json"));
        var writer = new CastleTerminalEvidenceWriter(logsRoot);

        var act = () => writer.Validate(unsafeEvidencePath, "run-7-terminal");

        act.Should().Throw<InvalidDataException>();
    }

    // ACC:T7.19
    [Fact]
    public void ShouldEnterGameOverExactlyAtZeroBoundary_WhenPreviousHitLeavesHpAboveZero()
    {
        var runtime = CreateRuntime(startHp: 5, out var flow);

        var nonTerminalHit = runtime.ResolveEnemyAttack(CreateCastleDecision(runtime), 4);

        nonTerminalHit.AttackApplied.Should().BeTrue();
        nonTerminalHit.EnteredGameOver.Should().BeFalse();
        nonTerminalHit.CastleHpChangedEvent!.CurrentHp.Should().Be(1);
        runtime.CurrentHp.Should().Be(1);
        flow.State.Should().Be(GameFlowState.Running);

        var terminalHit = runtime.ResolveEnemyAttack(CreateCastleDecision(runtime), 1);

        terminalHit.AttackApplied.Should().BeTrue();
        terminalHit.EnteredGameOver.Should().BeTrue();
        terminalHit.CastleHpChangedEvent!.CurrentHp.Should().Be(0);
        runtime.CurrentHp.Should().Be(0);
        flow.State.Should().Be(GameFlowState.GameOver);
    }

    // ACC:T7.17
    // ACC:T7.21
    [Fact]
    public void ShouldEmitSingleTerminalDispatch_WhenRuntimeFirstReachesZero()
    {
        var flow = new GameStateMachine();
        var transitions = new List<GameFlowState>();
        flow.OnTransition += (_, next) => transitions.Add(next);
        var runtime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(10),
            flow,
            "run-7",
            1);

        var firstHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            3);
        var terminalHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            7);
        var repeatedHit = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            1);

        firstHit.EnteredGameOver.Should().BeFalse();
        terminalHit.EnteredGameOver.Should().BeTrue();
        terminalHit.CastleHpChangedEvent!.PreviousHp.Should().Be(7);
        terminalHit.CastleHpChangedEvent.CurrentHp.Should().Be(0);
        repeatedHit.AttackApplied.Should().BeFalse();
        repeatedHit.EnteredGameOver.Should().BeFalse();
        repeatedHit.CastleHpChangedEvent.Should().BeNull();
        runtime.CurrentHp.Should().Be(0);
        flow.State.Should().Be(GameFlowState.GameOver);
        transitions.Should().ContainSingle(state => state == GameFlowState.GameOver);
    }

    // ACC:T7.20
    // ACC:T7.22
    [Fact]
    public void ShouldClampOverkillDamageToZeroAndFreezeAfterTerminal_WhenFurtherHitsArrive()
    {
        var flow = new GameStateMachine();
        var runtime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(3),
            flow,
            "run-7",
            1);

        var terminalHit = runtime.ResolveEnemyAttack(CreateCastleDecision(runtime), 5);
        var afterTerminalHit = runtime.ResolveEnemyAttack(
            CreateCastleDecision(runtime),
            5);

        terminalHit.CastleHpChangedEvent.Should().NotBeNull();
        terminalHit.EnteredGameOver.Should().BeTrue();
        terminalHit.CastleHpChangedEvent!.PreviousHp.Should().Be(3);
        terminalHit.CastleHpChangedEvent.CurrentHp.Should().Be(0);
        afterTerminalHit.AttackApplied.Should().BeFalse();
        afterTerminalHit.EnteredGameOver.Should().BeFalse();
        afterTerminalHit.CastleHpChangedEvent.Should().BeNull();
        runtime.CurrentHp.Should().Be(0);
        flow.State.Should().Be(GameFlowState.GameOver);
    }

    private static EnemyAiTargetDecision CreateCastleDecision(CastleBattleRuntime runtime)
    {
        return new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "project.godot")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test host base directory.");
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

    private static object ToTracePoint(CastleBattleResult result)
    {
        return new
        {
            result.AttackApplied,
            result.EnteredGameOver,
            PreviousHp = result.CastleHpChangedEvent?.PreviousHp,
            CurrentHp = result.CastleHpChangedEvent?.CurrentHp,
            ChangedAt = result.CastleHpChangedEvent?.ChangedAt
        };
    }
}
