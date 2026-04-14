using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;

namespace Game.Core.Services;

public class CombatService
{
    private readonly IEventBus? _bus;

    public CombatService(IEventBus? bus = null)
    {
        _bus = bus;
    }

    public void ApplyDamage(Player player, int amount)
    {
        player.TakeDamage(amount);
    }

    public CombatResolution ResolveAttack(Damage damage, int attackerTeamId, int targetTeamId, CombatConfig? config = null)
    {
        if (attackerTeamId == targetTeamId)
        {
            return new CombatResolution(
                CanCommitDamage: false,
                ResolvedDamage: 0,
                Outcome: "friendly_fire_refused");
        }

        var resolvedDamage = config is null
            ? CalculateDamage(damage)
            : CalculateDamage(damage, config);
        return new CombatResolution(
            CanCommitDamage: true,
            ResolvedDamage: resolvedDamage,
            Outcome: "damage_applied");
    }

    public void ApplyDamage(Player player, Damage damage)
    {
        // Placeholder for future type-based mitigation; for now apply raw amount
        player.TakeDamage(damage.EffectiveAmount);
        PublishCombatEvent(
            damage: damage,
            resolution: new CombatResolution(true, damage.EffectiveAmount, "damage_applied"),
            attackerId: string.Empty,
            attackerTeam: -1,
            targetId: "player",
            targetTeam: -1);
    }

    public int CalculateDamage(Damage damage, CombatConfig? config = null)
    {
        config ??= CombatConfig.Default;
        var amount = Math.Max(0, damage.EffectiveAmount);
        double mult = 1.0;
        if (config.Resistances.TryGetValue(damage.Type, out var r)) mult *= r;
        if (damage.IsCritical) mult *= Math.Max(1.0, config.CritMultiplier);
        var result = (int)Math.Round(amount * mult);
        return Math.Max(0, result);
    }

    public int CalculateDamage(Damage damage, CombatConfig config, int armor)
    {
        var baseDmg = CalculateDamage(damage, config);
        // Simple linear armor mitigate; can be replaced with non-linear curve later
        var mitigated = Math.Max(0, baseDmg - Math.Max(0, armor));
        return mitigated;
    }

    public void ApplyDamage(Player player, Damage damage, CombatConfig config)
    {
        var final = CalculateDamage(damage, config);
        player.TakeDamage(final);
        PublishCombatEvent(
            damage: damage,
            resolution: new CombatResolution(true, final, "damage_applied"),
            attackerId: string.Empty,
            attackerTeam: -1,
            targetId: "player",
            targetTeam: -1);
    }

    public CombatResolution ResolveAndApplyAttack(
        Player player,
        Damage damage,
        string attackerId,
        int attackerTeam,
        string targetId,
        int targetTeam,
        CombatConfig? config = null)
    {
        var resolution = ResolveAttack(damage, attackerTeam, targetTeam, config);
        if (resolution.CanCommitDamage && resolution.ResolvedDamage > 0)
        {
            player.TakeDamage(resolution.ResolvedDamage);
        }

        PublishCombatEvent(
            damage: damage,
            resolution: resolution,
            attackerId: attackerId,
            attackerTeam: attackerTeam,
            targetId: targetId,
            targetTeam: targetTeam);
        return resolution;
    }

    public void RecordTargetPickDiagnostic(
        string targetId,
        string targetClass,
        bool isFallbackAttack,
        int tick = -1,
        int sequence = -1)
    {
        if (_bus is null)
        {
            return;
        }

        var payload = new TargetPickDiagnosticPayload(
            TargetId: targetId,
            TargetClass: targetClass,
            IsFallbackAttack: isFallbackAttack,
            Tick: tick,
            Sequence: sequence,
            Outcome: "target_selected");
        _ = _bus.PublishAsync(Contracts.DomainEvent.Create(
            type: "combat.target_selected",
            source: nameof(CombatService),
            payload: payload,
            timestamp: DateTime.UtcNow,
            id: $"pick-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));
    }

    private void PublishCombatEvent(
        Damage damage,
        CombatResolution resolution,
        string attackerId,
        int attackerTeam,
        string targetId,
        int targetTeam)
    {
        var payload = new PlayerDamagedPayload(
            Amount: resolution.ResolvedDamage,
            Type: damage.Type.ToString(),
            Critical: damage.IsCritical,
            AttackerId: attackerId,
            AttackerTeam: attackerTeam,
            TargetId: targetId,
            TargetTeam: targetTeam,
            ResolvedDamage: resolution.ResolvedDamage,
            Outcome: resolution.Outcome
        );
        var eventType = resolution.CanCommitDamage ? "player.damaged" : "player.damage_refused";
        _ = _bus?.PublishAsync(Contracts.DomainEvent.Create(
            type: eventType,
            source: nameof(CombatService),
            payload: payload,
            timestamp: DateTime.UtcNow,
            id: $"dmg-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
        ));
    }

    private sealed record PlayerDamagedPayload(
        int Amount,
        string Type,
        bool Critical,
        string AttackerId,
        int AttackerTeam,
        string TargetId,
        int TargetTeam,
        int ResolvedDamage,
        string Outcome);

    private sealed record TargetPickDiagnosticPayload(
        string TargetId,
        string TargetClass,
        bool IsFallbackAttack,
        int Tick,
        int Sequence,
        string Outcome);
}

public readonly record struct CombatResolution(bool CanCommitDamage, int ResolvedDamage, string Outcome);
