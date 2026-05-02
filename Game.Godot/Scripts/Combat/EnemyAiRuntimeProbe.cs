using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Game.Core.Domain;
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

    public Dictionary SimulateAreaDamageRuntime(
        GodotArray targets,
        bool areaCapableSourceActive,
        int attackerTeamId = 1,
        int baseDamage = 20,
        float radius = 3.0f,
        float falloffPerUnit = 0.25f,
        float minFalloffScale = 0.4f,
        int minResolvedDamage = 5,
        int maxResolvedDamage = 12)
    {
        var mappedTargets = new List<AreaTargetCandidate>(targets.Count);
        foreach (var raw in targets)
        {
            if (!TryAsDictionary(raw, out var target))
            {
                continue;
            }

            var targetId = ReadString(target, "id");
            var teamId = ReadInt(target, "team_id");
            var distance = target.ContainsKey("distance")
                ? Convert.ToDecimal(target["distance"].AsDouble())
                : 0m;
            var isDamageable = !target.ContainsKey("damageable") || target["damageable"].AsBool();
            mappedTargets.Add(new AreaTargetCandidate(
                TargetId: targetId,
                TeamId: teamId,
                DistanceFromImpact: distance,
                IsDamageable: isDamageable));
        }

        var service = new CombatService();
        var config = new AreaDamageConfig(
            Radius: Convert.ToDecimal(radius),
            FalloffPerUnit: Convert.ToDecimal(falloffPerUnit),
            MinFalloffScale: Convert.ToDecimal(minFalloffScale),
            MinResolvedDamage: minResolvedDamage,
            MaxResolvedDamage: maxResolvedDamage);
        var results = service.ResolveAreaDamage(
            damage: new Damage(baseDamage, DamageType.Physical),
            attackerTeamId: attackerTeamId,
            targets: mappedTargets,
            config: config,
            areaCapableSourceActive: areaCapableSourceActive);

        var totalDamage = 0;
        var committedCount = 0;
        var outOfRadiusCount = 0;
        var detail = new GodotArray();
        foreach (var item in results)
        {
            totalDamage += item.ResolvedDamage;
            if (item.DamageCommitted)
            {
                committedCount++;
            }

            if (string.Equals(item.Outcome, "out_of_radius", StringComparison.Ordinal))
            {
                outOfRadiusCount++;
            }

            detail.Add(new Dictionary
            {
                ["target_id"] = item.TargetId,
                ["distance"] = Convert.ToDouble(item.DistanceFromImpact),
                ["area_applied"] = item.AreaApplied,
                ["damage_committed"] = item.DamageCommitted,
                ["resolved_damage"] = item.ResolvedDamage,
                ["outcome"] = item.Outcome
            });
        }

        return new Dictionary
        {
            ["area_active"] = areaCapableSourceActive,
            ["candidate_count"] = mappedTargets.Count,
            ["resolved_count"] = results.Count,
            ["committed_count"] = committedCount,
            ["out_of_radius_count"] = outOfRadiusCount,
            ["total_damage"] = totalDamage,
            ["results"] = detail
        };
    }
}
