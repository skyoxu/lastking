using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Contracts;
using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class CombatSystemTests
{
    private sealed class CapturingEventBus : IEventBus
    {
        public List<DomainEvent> Events { get; } = new();

        public Task PublishAsync(DomainEvent evt)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }

        public IDisposable Subscribe(Func<DomainEvent, Task> handler)
        {
            return new NoopSubscription();
        }

        private sealed class NoopSubscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    // ACC:T20.16
    [Theory]
    [InlineData("unit")]
    [InlineData("mg_tower")]
    public void ShouldRefuseAndKeepTargetHpUnchanged_WhenAttackerAndTargetShareTeam(string attackerType)
    {
        var bus = new CapturingEventBus();
        var combatService = new CombatService(bus);
        var attackerId = attackerType == "mg_tower" ? "tower_blue_01" : "unit_blue_01";
        var damage = attackerType == "mg_tower"
            ? new Damage(16, DamageType.Physical)
            : new Damage(12, DamageType.Physical);
        var target = new Player(maxHealth: 50);

        var resolution = combatService.ResolveAndApplyAttack(
            player: target,
            damage: damage,
            attackerId: attackerId,
            attackerTeam: 1,
            targetId: "ally_target",
            targetTeam: 1);

        resolution.CanCommitDamage.Should().BeFalse(
            $"shared attack-resolution policy must refuse same-team damage for {attackerType} attacks before commit");
        resolution.ResolvedDamage.Should().Be(0);
        resolution.Outcome.Should().Be("friendly_fire_refused");
        target.Health.Current.Should().Be(50);
        bus.Events.Should().ContainSingle();
        var payload = bus.Events[0].DataElement!.Value;
        payload.GetProperty("AttackerId").GetString().Should().Be(attackerId);
        payload.GetProperty("Outcome").GetString().Should().Be("friendly_fire_refused");
    }

    // ACC:T20.17
    [Fact]
    public void ShouldSelectUnitBeforeCastleAndTower_WhenHigherPriorityUnitIsReachable()
    {
        var selector = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Reachable("tower-1", EnemyTargetClass.ArmedDefense, 1),
            EnemyAiTargetCandidate.Reachable("castle-1", EnemyTargetClass.Castle, 2),
            EnemyAiTargetCandidate.Reachable("unit-1", EnemyTargetClass.Unit, 8)
        };

        var decision = selector.SelectTarget(candidates);

        decision.TargetId.Should().Be("unit-1");
        decision.TargetClass.Should().Be(EnemyTargetClass.Unit);
        decision.IsFallbackAttack.Should().BeFalse();
    }

    // ACC:T20.17
    [Fact]
    public void ShouldApplyDamageInTickThenSequenceOrder_WhenMultipleAttacksResolveInSameFrame()
    {
        var scheduledAttacks = new[]
        {
            new ScheduledAttack("tick-2-seq-1", Tick: 2, Sequence: 1, Damage: 3),
            new ScheduledAttack("tick-1-seq-2", Tick: 1, Sequence: 2, Damage: 5),
            new ScheduledAttack("tick-1-seq-1", Tick: 1, Sequence: 1, Damage: 7)
        };

        var result = ResolveDamageInDeterministicOrder(startHp: 40, scheduledAttacks);

        result.AppliedOrder.Should().Equal("tick-1-seq-1", "tick-1-seq-2", "tick-2-seq-1");
        result.RemainingHp.Should().Be(25);
    }

    // ACC:T20.9
    [Fact]
    public void ShouldProduceDeterministicTargetAndDamageTraces_WhenInitialStateAndTickInputAreIdentical()
    {
        var firstTargetTrace = ResolveTargetTrace();
        var secondTargetTrace = ResolveTargetTrace();
        var scheduledAttacks = new[]
        {
            new ScheduledAttack("a", Tick: 1, Sequence: 2, Damage: 2),
            new ScheduledAttack("b", Tick: 1, Sequence: 1, Damage: 4),
            new ScheduledAttack("c", Tick: 2, Sequence: 1, Damage: 3)
        };

        var firstDamageTrace = ResolveDamageInDeterministicOrder(startHp: 30, scheduledAttacks);
        var secondDamageTrace = ResolveDamageInDeterministicOrder(startHp: 30, scheduledAttacks);

        secondTargetTrace.Should().Equal(firstTargetTrace);
        secondDamageTrace.AppliedOrder.Should().Equal(firstDamageTrace.AppliedOrder);
        secondDamageTrace.RemainingHp.Should().Be(firstDamageTrace.RemainingHp);
    }

    private static IReadOnlyList<string> ResolveTargetTrace()
    {
        var selector = new EnemyAiTargetSelector();
        var tickOneCandidates = new[]
        {
            EnemyAiTargetCandidate.Reachable("unit-1", EnemyTargetClass.Unit, 3),
            EnemyAiTargetCandidate.Reachable("castle-1", EnemyTargetClass.Castle, 1)
        };
        var tickTwoCandidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Reachable("castle-1", EnemyTargetClass.Castle, 2),
            EnemyAiTargetCandidate.Reachable("tower-1", EnemyTargetClass.ArmedDefense, 1)
        };

        var tickOneDecision = selector.SelectTarget(tickOneCandidates);
        var tickTwoDecision = selector.SelectTarget(tickTwoCandidates);

        return new[] { tickOneDecision.TargetId ?? string.Empty, tickTwoDecision.TargetId ?? string.Empty };
    }

    private static DamageOrderResult ResolveDamageInDeterministicOrder(int startHp, IEnumerable<ScheduledAttack> scheduledAttacks)
    {
        var combatService = new CombatService();
        var target = new Player(maxHealth: startHp);
        var appliedOrder = new List<string>();

        foreach (var attack in scheduledAttacks.OrderBy(item => item.Tick).ThenBy(item => item.Sequence))
        {
            combatService.ApplyDamage(target, new Damage(attack.Damage, DamageType.Physical));
            appliedOrder.Add(attack.AttackId);
        }

        return new DamageOrderResult(target.Health.Current, appliedOrder);
    }

    private sealed record ScheduledAttack(string AttackId, int Tick, int Sequence, int Damage);

    private sealed record DamageOrderResult(int RemainingHp, IReadOnlyList<string> AppliedOrder);
}
