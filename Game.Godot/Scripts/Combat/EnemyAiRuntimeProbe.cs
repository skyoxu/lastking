using Godot;
using Godot.Collections;
using Game.Core.Domain.ValueObjects;
using Game.Core.Services;
using GodotArray = Godot.Collections.Array;

namespace Lastking.Game.Godot.Scripts.Combat;

public partial class EnemyAiRuntimeProbe : EnemyAi
{
    private static int ReadConfigManagerCadence(ConfigManager manager) => manager.Snapshot.SpawnCadenceSeconds;

    public new Dictionary ProbeNavigationPath(Vector2 origin, Vector2 destination)
    {
        return base.ProbeNavigationPath(origin, destination);
    }

    public new Dictionary SelectTargetWithNavigation(GodotArray candidates, Vector2 origin)
    {
        return base.SelectTargetWithNavigation(candidates, origin);
    }

    public new Dictionary SimulateBlockedMapFallbackWithNavigation(
        int enemyCount,
        int timeoutTicks,
        int attackReadyTick,
        Vector2 origin,
        Vector2 destination)
    {
        return base.SimulateBlockedMapFallbackWithNavigation(
            enemyCount,
            timeoutTicks,
            attackReadyTick,
            origin,
            destination);
    }

    public Dictionary SimulateProjectileRuntime(
        bool hasFiringSolution,
        bool shouldImpact,
        int travelTicks,
        int attackerTeamId,
        int targetTeamId)
    {
        var service = new CombatService();
        var runtime = ProjectileRuntimeProfile.FromSnapshot(
            BalanceSnapshot.Default,
            ProjectileOwnerKind.RangedEnemy);
        var resolution = service.ResolveProjectileAttack(
            ownerKind: ProjectileOwnerKind.RangedEnemy,
            hasFiringSolution: hasFiringSolution,
            shouldImpact: shouldImpact,
            travelTicks: travelTicks,
            damage: new Damage(10, DamageType.Physical),
            attackerTeamId: attackerTeamId,
            targetTeamId: targetTeamId,
            runtimeProfile: runtime);

        return new Dictionary
        {
            ["projectile_created"] = resolution.ProjectileCreated,
            ["impact_resolved"] = resolution.ImpactResolved,
            ["timed_out"] = resolution.TimedOut,
            ["cleaned_up"] = resolution.CleanedUp,
            ["damage_committed"] = resolution.DamageCommitted,
            ["resolved_damage"] = resolution.ResolvedDamage,
            ["outcome"] = resolution.Outcome,
            ["travel_speed_per_tick"] = resolution.RuntimeProfile.TravelSpeedPerTick,
            ["timeout_ticks"] = resolution.RuntimeProfile.TimeoutTicks
        };
    }
}
