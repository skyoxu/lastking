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

    public ProjectileAttackResolution ResolveProjectileAttack(
        ProjectileOwnerKind ownerKind,
        bool hasFiringSolution,
        bool shouldImpact,
        int travelTicks,
        Damage damage,
        int attackerTeamId,
        int targetTeamId,
        ProjectileRuntimeProfile runtimeProfile,
        CombatConfig? config = null)
    {
        if (!hasFiringSolution)
        {
            return new ProjectileAttackResolution(
                OwnerKind: ownerKind,
                ProjectileCreated: false,
                ImpactResolved: false,
                TimedOut: false,
                CleanedUp: false,
                DamageCommitted: false,
                ResolvedDamage: 0,
                Outcome: "no_firing_solution",
                RuntimeProfile: runtimeProfile);
        }

        var normalizedTravelTicks = Math.Max(0, travelTicks);
        var timedOut = normalizedTravelTicks >= runtimeProfile.TimeoutTicks;
        if (timedOut)
        {
            return new ProjectileAttackResolution(
                OwnerKind: ownerKind,
                ProjectileCreated: true,
                ImpactResolved: false,
                TimedOut: true,
                CleanedUp: true,
                DamageCommitted: false,
                ResolvedDamage: 0,
                Outcome: "projectile_timeout",
                RuntimeProfile: runtimeProfile);
        }

        if (!shouldImpact)
        {
            return new ProjectileAttackResolution(
                OwnerKind: ownerKind,
                ProjectileCreated: true,
                ImpactResolved: false,
                TimedOut: false,
                CleanedUp: true,
                DamageCommitted: false,
                ResolvedDamage: 0,
                Outcome: "projectile_miss",
                RuntimeProfile: runtimeProfile);
        }

        var scaledAmount = (int)Math.Round(Math.Max(0, damage.EffectiveAmount) * runtimeProfile.ImpactScale);
        var scaledDamage = new Damage(
            Amount: scaledAmount,
            Type: damage.Type,
            IsCritical: damage.IsCritical);
        var resolution = ResolveAttack(scaledDamage, attackerTeamId, targetTeamId, config);
        var committed = resolution.CanCommitDamage && resolution.ResolvedDamage > 0;

        return new ProjectileAttackResolution(
            OwnerKind: ownerKind,
            ProjectileCreated: true,
            ImpactResolved: true,
            TimedOut: false,
            CleanedUp: true,
            DamageCommitted: committed,
            ResolvedDamage: resolution.ResolvedDamage,
            Outcome: resolution.Outcome,
            RuntimeProfile: runtimeProfile);
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

public enum ProjectileOwnerKind
{
    Tower,
    RangedEnemy
}

public readonly record struct ProjectileRuntimeProfile(
    int TravelSpeedPerTick,
    int TimeoutTicks,
    decimal ImpactScale)
{
    public static ProjectileRuntimeProfile FromSnapshot(BalanceSnapshot snapshot, ProjectileOwnerKind ownerKind)
    {
        _ = ownerKind;
        var travelSpeed = Math.Max(1, snapshot.SpawnCadenceSeconds);
        var timeoutTicks = Math.Max(1, snapshot.RegularSpawnCadenceSeconds ?? snapshot.SpawnCadenceSeconds);
        return new ProjectileRuntimeProfile(
            TravelSpeedPerTick: travelSpeed,
            TimeoutTicks: timeoutTicks,
            ImpactScale: 1.0m);
    }
}

public readonly record struct ProjectileAttackResolution(
    ProjectileOwnerKind OwnerKind,
    bool ProjectileCreated,
    bool ImpactResolved,
    bool TimedOut,
    bool CleanedUp,
    bool DamageCommitted,
    int ResolvedDamage,
    string Outcome,
    ProjectileRuntimeProfile RuntimeProfile);
