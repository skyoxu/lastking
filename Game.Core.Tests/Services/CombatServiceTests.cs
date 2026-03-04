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

    [Fact]
    public void ShouldMitigateLinearly_WhenArmorAppliedInDamageCalculation()
    {
        var cfg = new CombatConfig();
        var svc = new CombatService();
        var dmg = new Damage(40, DamageType.Physical);
        var res = svc.CalculateDamage(dmg, cfg, armor: 10);
        Assert.Equal(30, res);
    }

    [Fact]
    public void ShouldReducePlayerHealth_WhenApplyDamageCalled()
    {
        var p = new Player(maxHealth: 100);
        var svc = new CombatService();
        svc.ApplyDamage(p, new Damage(25, DamageType.Physical));
        Assert.Equal(75, p.Health.Current);
    }

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
    }

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
    }
}
