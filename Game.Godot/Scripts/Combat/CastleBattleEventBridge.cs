using System;
using Godot;
using Game.Core.Contracts;
using Game.Core.Contracts.Lastking;
using Game.Core.Services;
using Game.Core.State;
using Game.Godot.Adapters;

namespace Game.Godot.Scripts.Combat;

public partial class CastleBattleEventBridge : Node
{
    private CastleBattleRuntime? runtime;
    private EventBusAdapter? eventBus;

    public override void _Ready()
    {
        ResolveEventBus();
    }

    public void StartBattle(int startHp, string runId = "run-1", int dayNumber = 1, string castleTargetId = "castle")
    {
        ResolveEventBus();
        var flowStateMachine = new GameStateMachine();
        runtime = CastleBattleRuntime.StartBattleFromConfig(
            new CastleBattleConfig(startHp, castleTargetId),
            flowStateMachine,
            runId,
            dayNumber);

        PublishCastleHpChanged(new CastleHpChanged(
            runId,
            dayNumber,
            runtime.CurrentHp,
            runtime.CurrentHp,
            System.DateTimeOffset.UnixEpoch));
    }

    public void ResolveCastleAttack(int damage)
    {
        if (runtime is null)
        {
            throw new InvalidOperationException("StartBattle must be called before ResolveCastleAttack.");
        }

        var result = runtime.ResolveEnemyAttack(
            new EnemyAiTargetDecision("castle", false, runtime.CastleTargetId, EnemyTargetClass.Castle),
            damage);

        if (result.CastleHpChangedEvent is not null)
        {
            PublishCastleHpChanged(result.CastleHpChangedEvent);
        }
    }

    public int GetCurrentHp()
    {
        if (runtime is null)
        {
            throw new InvalidOperationException("StartBattle must be called before GetCurrentHp.");
        }

        return runtime.CurrentHp;
    }

    private void PublishCastleHpChanged(CastleHpChanged hpChanged)
    {
        if (eventBus is null)
        {
            return;
        }

        var evt = DomainEvent.Create(
            type: CastleHpChanged.EventType,
            source: nameof(CastleBattleEventBridge),
            payload: hpChanged,
            timestamp: hpChanged.ChangedAt.UtcDateTime,
            id: $"{hpChanged.RunId}-castle-hp-{hpChanged.ChangedAt.ToUnixTimeSeconds()}");
        _ = eventBus.PublishAsync(evt);
    }

    private void ResolveEventBus()
    {
        eventBus ??= GetNodeOrNull<EventBusAdapter>("/root/EventBus");
    }
}
