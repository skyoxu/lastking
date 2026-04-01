using System;
using System.IO;
using Game.Core.Contracts.Lastking;
using Game.Core.State;

namespace Game.Core.Services;

public sealed record CastleBattleConfig(int StartHp, string CastleTargetId = "castle");

public sealed record CastleBattleResult(
    bool AttackApplied,
    bool EnteredGameOver,
    CastleHpChanged? CastleHpChangedEvent);

public sealed class CastleBattleRuntime
{
    private readonly GameStateMachine flowStateMachine;
    private readonly string runId;
    private readonly int dayNumber;
    private int hitIndex;

    public CastleBattleRuntime(
        CastleBattleConfig config,
        GameStateMachine flowStateMachine,
        string runId,
        int dayNumber)
    {
        ArgumentNullException.ThrowIfNull(config);
        this.flowStateMachine = flowStateMachine ?? throw new ArgumentNullException(nameof(flowStateMachine));
        this.runId = string.IsNullOrWhiteSpace(runId) ? "run-1" : runId;
        this.dayNumber = dayNumber;

        if (config.StartHp <= 0)
        {
            throw new InvalidDataException("Config.StartHp must be greater than 0.");
        }

        CastleTargetId = string.IsNullOrWhiteSpace(config.CastleTargetId) ? "castle" : config.CastleTargetId;
        CurrentHp = config.StartHp;

        if (this.flowStateMachine.State == GameFlowState.Initialized)
        {
            this.flowStateMachine.Start();
        }
    }

    public int CurrentHp { get; private set; }

    public string CastleTargetId { get; }

    public bool IsAlive => CurrentHp > 0;

    public static CastleBattleRuntime StartBattleFromConfig(
        CastleBattleConfig config,
        GameStateMachine flowStateMachine,
        string runId,
        int dayNumber)
    {
        return new CastleBattleRuntime(config, flowStateMachine, runId, dayNumber);
    }

    public CastleBattleResult ResolveEnemyAttack(EnemyAiTargetDecision decision, int enemyAttackDamage)
    {
        if (flowStateMachine.State == GameFlowState.GameOver)
        {
            return new CastleBattleResult(false, false, null);
        }

        if (decision.AttackEventTargetId is null)
        {
            return new CastleBattleResult(false, false, null);
        }

        if (!string.Equals(decision.AttackEventTargetId, CastleTargetId, StringComparison.OrdinalIgnoreCase))
        {
            return new CastleBattleResult(false, false, null);
        }

        var resolvedDamage = enemyAttackDamage;
        if (resolvedDamage <= 0)
        {
            return new CastleBattleResult(false, false, null);
        }

        var previousHp = CurrentHp;
        CurrentHp = CurrentHp - resolvedDamage;
        if (CurrentHp < 0)
        {
            CurrentHp = 0;
        }

        hitIndex += 1;
        var hpChanged = new CastleHpChanged(
            runId,
            dayNumber,
            previousHp,
            CurrentHp,
            DateTimeOffset.UnixEpoch.AddSeconds(hitIndex));

        if (CurrentHp > 0)
        {
            return new CastleBattleResult(true, false, hpChanged);
        }

        var enteredGameOver = false;
        if (CurrentHp <= 0 && flowStateMachine.State != GameFlowState.GameOver)
        {
            enteredGameOver = flowStateMachine.End();
        }

        return new CastleBattleResult(true, enteredGameOver, hpChanged);
    }
}
