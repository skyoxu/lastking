using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;
using Game.Core.Contracts;
using Game.Core.Services;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public class CombatServiceTests
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

    // ACC:T20.1
    // ACC:T20.2
    [Fact]
    public void ShouldApplyResistanceAndCritical_WhenCalculatingDamage()
    {
        var cfg = new CombatConfig { CritMultiplier = 2.0 };
        cfg.Resistances[DamageType.Fire] = 0.5; // 50% resist

        var svc = new CombatService();
        var baseFire = new Damage(100, DamageType.Fire);
        var reduced = svc.CalculateDamage(baseFire, cfg);
        Assert.Equal(50, reduced);

        var crit = new Damage(100, DamageType.Fire, IsCritical: true);
        var reducedCrit = svc.CalculateDamage(crit, cfg);
        Assert.Equal(100, reducedCrit); // 100 * 0.5 * 2.0
    }

    // ACC:T20.3
    // ACC:T20.4
    [Fact]
    public void ShouldMitigateLinearly_WhenArmorAppliedInDamageCalculation()
    {
        var cfg = new CombatConfig();
        var svc = new CombatService();
        var dmg = new Damage(40, DamageType.Physical);
        var res = svc.CalculateDamage(dmg, cfg, armor: 10);
        Assert.Equal(30, res);
    }

    // ACC:T20.7
    // ACC:T20.8
    [Fact]
    public void ShouldReducePlayerHealth_WhenApplyDamageCalled()
    {
        var p = new Player(maxHealth: 100);
        var svc = new CombatService();
        svc.ApplyDamage(p, new Damage(25, DamageType.Physical));
        Assert.Equal(75, p.Health.Current);
    }

    // ACC:T20.11
    [Fact]
    public void ShouldClampNegativeInputAndArmor_WhenCalculatingDamage()
    {
        var cfg = new CombatConfig { CritMultiplier = 1.5 };
        var svc = new CombatService();
        var negativeDamage = new Damage(-10, DamageType.Magical);

        var zero = svc.CalculateDamage(negativeDamage, cfg);
        var withNegativeArmor = svc.CalculateDamage(new Damage(10, DamageType.Magical), cfg, armor: -5);

        zero.Should().Be(0);
        withNegativeArmor.Should().Be(10);
    }

    // ACC:T20.12
    [Fact]
    public void ShouldPublishEvent_WhenApplyingDamageWithEventBus()
    {
        var bus = new CapturingEventBus();
        var player = new Player(maxHealth: 100);
        var svc = new CombatService(bus);

        svc.ApplyDamage(player, new Damage(20, DamageType.Fire, true));

        player.Health.Current.Should().Be(80);
        bus.Events.Should().ContainSingle();
        bus.Events[0].Type.Should().Be("player.damaged");
        bus.Events[0].Source.Should().Be(nameof(CombatService));
        bus.Events[0].DataElement.HasValue.Should().BeTrue();
        var payload = bus.Events[0].DataElement!.Value;
        payload.GetProperty("AttackerId").GetString().Should().BeEmpty();
        payload.GetProperty("AttackerTeam").GetInt32().Should().Be(-1);
        payload.GetProperty("TargetId").GetString().Should().Be("player");
        payload.GetProperty("TargetTeam").GetInt32().Should().Be(-1);
        payload.GetProperty("ResolvedDamage").GetInt32().Should().Be(20);
        payload.GetProperty("Outcome").GetString().Should().Be("damage_applied");
    }

    // ACC:T20.13
    // ACC:T20.14
    // ACC:T20.15
    // ACC:T20.19
    [Fact]
    public void ShouldUseCalculatedDamageAndPublishEvent_WhenApplyingDamageWithConfig()
    {
        var bus = new CapturingEventBus();
        var player = new Player(maxHealth: 200);
        var cfg = new CombatConfig { CritMultiplier = 2.0 };
        cfg.Resistances[DamageType.Physical] = 0.5;
        var svc = new CombatService(bus);

        svc.ApplyDamage(player, new Damage(50, DamageType.Physical, true), cfg);

        player.Health.Current.Should().Be(150);
        bus.Events.Should().ContainSingle();
        bus.Events[0].Type.Should().Be("player.damaged");
        var payload = bus.Events[0].DataElement!.Value;
        payload.GetProperty("ResolvedDamage").GetInt32().Should().Be(50);
        payload.GetProperty("Outcome").GetString().Should().Be("damage_applied");
    }

    // ACC:T20.11
    // ACC:T20.12
    // ACC:T20.13
    // ACC:T48.2
    // ACC:T50.3
    // ACC:T50.6
    [Fact]
    public void ShouldRefuseFriendlyFireAndResolveZeroDamage_WhenAttackerAndTargetShareTeam()
    {
        var svc = new CombatService();

        var resolution = svc.ResolveAttack(
            new Damage(30, DamageType.Physical, IsCritical: true),
            attackerTeamId: 7,
            targetTeamId: 7);

        resolution.CanCommitDamage.Should().BeFalse();
        resolution.ResolvedDamage.Should().Be(0);
        resolution.Outcome.Should().Be("friendly_fire_refused");
    }

    // ACC:T20.14
    // ACC:T48.8
    [Fact]
    public void ShouldAllowDamageCommit_WhenAttackerAndTargetTeamsDiffer()
    {
        var svc = new CombatService();
        var cfg = new CombatConfig();
        cfg.Resistances[DamageType.Physical] = 1.0;

        var resolution = svc.ResolveAttack(
            new Damage(18, DamageType.Physical),
            attackerTeamId: 1,
            targetTeamId: 2,
            config: cfg);

        resolution.CanCommitDamage.Should().BeTrue();
        resolution.ResolvedDamage.Should().Be(18);
        resolution.Outcome.Should().Be("damage_applied");
    }

    // ACC:T20.15
    // ACC:T20.19
    // ACC:T50.5
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(10, 10)]
    public void ShouldAlwaysRefuseFriendlyFireInDenseAttempts_WhenTeamsAreIdentical(int attackerTeamId, int targetTeamId)
    {
        var svc = new CombatService();

        var resolution = svc.ResolveAttack(
            new Damage(5, DamageType.Physical),
            attackerTeamId,
            targetTeamId);

        resolution.CanCommitDamage.Should().BeFalse();
        resolution.ResolvedDamage.Should().Be(0);
        resolution.Outcome.Should().Be("friendly_fire_refused");
    }

    [Fact]
    // ACC:T20.12
    // ACC:T20.13
    // ACC:T20.14
    public void ShouldPublishRefusalEventAndKeepHp_WhenResolveAndApplyAttackHitsFriendlyFire()
    {
        var bus = new CapturingEventBus();
        var player = new Player(maxHealth: 100);
        var svc = new CombatService(bus);

        var resolution = svc.ResolveAndApplyAttack(
            player: player,
            damage: new Damage(25, DamageType.Physical),
            attackerId: "tower_blue",
            attackerTeam: 1,
            targetId: "unit_blue",
            targetTeam: 1);

        resolution.CanCommitDamage.Should().BeFalse();
        resolution.ResolvedDamage.Should().Be(0);
        player.Health.Current.Should().Be(100);
        bus.Events.Should().ContainSingle();
        bus.Events[0].Type.Should().Be("player.damage_refused");
        var payload = bus.Events[0].DataElement!.Value;
        payload.GetProperty("AttackerId").GetString().Should().Be("tower_blue");
        payload.GetProperty("AttackerTeam").GetInt32().Should().Be(1);
        payload.GetProperty("TargetId").GetString().Should().Be("unit_blue");
        payload.GetProperty("TargetTeam").GetInt32().Should().Be(1);
        payload.GetProperty("ResolvedDamage").GetInt32().Should().Be(0);
        payload.GetProperty("Outcome").GetString().Should().Be("friendly_fire_refused");
    }

    [Fact]
    // ACC:T20.13
    public void ShouldPublishTargetPickDiagnostic_WhenTargetIsSelected()
    {
        var bus = new CapturingEventBus();
        var svc = new CombatService(bus);

        svc.RecordTargetPickDiagnostic(
            targetId: "unit_red_1",
            targetClass: "Unit",
            isFallbackAttack: false,
            tick: 12,
            sequence: 2);

        bus.Events.Should().ContainSingle();
        bus.Events[0].Type.Should().Be("combat.target_selected");
        bus.Events[0].Source.Should().Be(nameof(CombatService));
        var payload = bus.Events[0].DataElement!.Value;
        payload.GetProperty("TargetId").GetString().Should().Be("unit_red_1");
        payload.GetProperty("TargetClass").GetString().Should().Be("Unit");
        payload.GetProperty("IsFallbackAttack").GetBoolean().Should().BeFalse();
        payload.GetProperty("Tick").GetInt32().Should().Be(12);
        payload.GetProperty("Sequence").GetInt32().Should().Be(2);
        payload.GetProperty("Outcome").GetString().Should().Be("target_selected");
    }

    // ACC:T48.1
    // ACC:T48.3
    // ACC:T48.6
    // ACC:T50.1
    // ACC:T50.8
    [Fact]
    public void ShouldSelectReachableHostileAndApplyDeterministicDamage_WhenMgTowerAttackRuns()
    {
        var selector = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Reachable("enemy_unit_1", EnemyTargetClass.Unit, 2),
            EnemyAiTargetCandidate.Reachable("enemy_castle_1", EnemyTargetClass.Castle, 6),
            EnemyAiTargetCandidate.Reachable("enemy_tower_1", EnemyTargetClass.ArmedDefense, 8)
        };

        var firstDecision = selector.SelectTarget(candidates);
        var secondDecision = selector.SelectTarget(candidates);
        firstDecision.Should().Be(secondDecision, "same runtime inputs must keep tower targeting deterministic");
        firstDecision.TargetId.Should().Be("enemy_unit_1");
        firstDecision.IsFallbackAttack.Should().BeFalse();

        var bus = new CapturingEventBus();
        var player = new Player(maxHealth: 90);
        var svc = new CombatService(bus);
        var resolution = svc.ResolveAndApplyAttack(
            player: player,
            damage: new Damage(15, DamageType.Physical),
            attackerId: "mg_tower_blue_01",
            attackerTeam: 1,
            targetId: "enemy_unit_1",
            targetTeam: 2);

        resolution.CanCommitDamage.Should().BeTrue();
        resolution.Outcome.Should().Be("damage_applied");
        player.Health.Current.Should().Be(75);
        bus.Events.Should().ContainSingle();
        bus.Events[0].Type.Should().Be("player.damaged");
    }

    // ACC:T50.1
    // ACC:T50.6
    [Fact]
    public void ShouldShareProjectileRuntimeForTowerAndRangedEnemy_WhenInputsMatch()
    {
        var svc = new CombatService();
        var runtime = new ProjectileRuntimeProfile(
            TravelSpeedPerTick: 3,
            TimeoutTicks: 8,
            ImpactScale: 1.0m);

        var tower = svc.ResolveProjectileAttack(
            ownerKind: ProjectileOwnerKind.Tower,
            hasFiringSolution: true,
            shouldImpact: true,
            travelTicks: 3,
            damage: new Damage(20, DamageType.Physical),
            attackerTeamId: 1,
            targetTeamId: 2,
            runtimeProfile: runtime);
        var ranged = svc.ResolveProjectileAttack(
            ownerKind: ProjectileOwnerKind.RangedEnemy,
            hasFiringSolution: true,
            shouldImpact: true,
            travelTicks: 3,
            damage: new Damage(20, DamageType.Physical),
            attackerTeamId: 1,
            targetTeamId: 2,
            runtimeProfile: runtime);

        tower.ProjectileCreated.Should().BeTrue();
        tower.ImpactResolved.Should().BeTrue();
        tower.TimedOut.Should().BeFalse();
        tower.CleanedUp.Should().BeTrue();
        tower.ResolvedDamage.Should().Be(20);
        tower.Outcome.Should().Be("damage_applied");

        ranged.ProjectileCreated.Should().BeTrue();
        ranged.ImpactResolved.Should().BeTrue();
        ranged.TimedOut.Should().BeFalse();
        ranged.CleanedUp.Should().BeTrue();
        ranged.ResolvedDamage.Should().Be(20);
        ranged.Outcome.Should().Be("damage_applied");
        ranged.RuntimeProfile.Should().Be(tower.RuntimeProfile);
    }

    // ACC:T50.1
    // ACC:T50.8
    [Fact]
    public void ShouldCreateNoProjectileAndEmitNoEvents_WhenNoFiringSolution()
    {
        var bus = new CapturingEventBus();
        var svc = new CombatService(bus);
        var runtime = new ProjectileRuntimeProfile(
            TravelSpeedPerTick: 2,
            TimeoutTicks: 5,
            ImpactScale: 1.0m);

        var result = svc.ResolveProjectileAttack(
            ownerKind: ProjectileOwnerKind.RangedEnemy,
            hasFiringSolution: false,
            shouldImpact: false,
            travelTicks: 0,
            damage: new Damage(15, DamageType.Physical),
            attackerTeamId: 2,
            targetTeamId: 1,
            runtimeProfile: runtime);

        result.ProjectileCreated.Should().BeFalse();
        result.ImpactResolved.Should().BeFalse();
        result.TimedOut.Should().BeFalse();
        result.CleanedUp.Should().BeFalse();
        result.DamageCommitted.Should().BeFalse();
        result.ResolvedDamage.Should().Be(0);
        result.Outcome.Should().Be("no_firing_solution");
        bus.Events.Should().BeEmpty();
    }

    // ACC:T50.1
    // ACC:T50.4
    [Fact]
    public void ShouldMarkProjectileAsTimedOutAndCleanedUp_WhenTravelExceedsTimeout()
    {
        var svc = new CombatService();
        var runtime = new ProjectileRuntimeProfile(
            TravelSpeedPerTick: 1,
            TimeoutTicks: 2,
            ImpactScale: 1.0m);

        var result = svc.ResolveProjectileAttack(
            ownerKind: ProjectileOwnerKind.Tower,
            hasFiringSolution: true,
            shouldImpact: false,
            travelTicks: 2,
            damage: new Damage(9, DamageType.Physical),
            attackerTeamId: 1,
            targetTeamId: 2,
            runtimeProfile: runtime);

        result.ProjectileCreated.Should().BeTrue();
        result.ImpactResolved.Should().BeFalse();
        result.TimedOut.Should().BeTrue();
        result.CleanedUp.Should().BeTrue();
        result.DamageCommitted.Should().BeFalse();
        result.ResolvedDamage.Should().Be(0);
        result.Outcome.Should().Be("projectile_timeout");
    }

    // ACC:T50.3
    [Fact]
    public void ShouldKeepFriendlyFireDisabled_WhenProjectileImpactsFriendlyTarget()
    {
        var svc = new CombatService();
        var runtime = new ProjectileRuntimeProfile(
            TravelSpeedPerTick: 3,
            TimeoutTicks: 6,
            ImpactScale: 1.0m);

        var result = svc.ResolveProjectileAttack(
            ownerKind: ProjectileOwnerKind.Tower,
            hasFiringSolution: true,
            shouldImpact: true,
            travelTicks: 2,
            damage: new Damage(30, DamageType.Physical),
            attackerTeamId: 7,
            targetTeamId: 7,
            runtimeProfile: runtime);

        result.ProjectileCreated.Should().BeTrue();
        result.ImpactResolved.Should().BeTrue();
        result.CleanedUp.Should().BeTrue();
        result.DamageCommitted.Should().BeFalse();
        result.ResolvedDamage.Should().Be(0);
        result.Outcome.Should().Be("friendly_fire_refused");
    }
}
