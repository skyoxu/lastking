using System.Collections.Generic;
using Game.Core.Services;
using Godot;
using Godot.Collections;
using GodotArray = Godot.Collections.Array;

namespace Lastking.Game.Godot.Scripts.Combat;

public partial class EnemyAi : Node
{
    private static int ReadConfigManagerCadence(ConfigManager manager) => manager.Snapshot.SpawnCadenceSeconds;

    [Export]
    public bool EnemyAiActive { get; set; } = true;

    private readonly EnemyAiTargetSelector _targetSelector = new();
    private readonly EnemyAiRetargetingService _retargetingService = new();
    private readonly EnemyAiDeterminismService _determinismService = new();

    public bool IsEnemyAiAttachedAndActive()
    {
        return EnemyAiActive && GetScript().VariantType != Variant.Type.Nil;
    }

    public Dictionary SelectTarget(GodotArray candidates)
    {
        var mapped = new List<EnemyAiTargetCandidate>();
        foreach (var item in candidates)
        {
            if (!TryAsDictionary(item, out var candidate))
            {
                continue;
            }

            var id = ReadString(candidate, "id");
            var targetClass = ParseTargetClass(ReadString(candidate, "class"));
            var reachable = ReadBool(candidate, "reachable");
            var blocked = ReadBool(candidate, "blocked");
            var pathPoints = ReadInt(candidate, "path_points");
            var distance = ReadInt(candidate, "distance");
            var blocksRoute = ReadBool(candidate, "blocks_route_to_higher_priority");

            var isReachable = reachable && !blocked && pathPoints > 0;
            mapped.Add(new EnemyAiTargetCandidate(id, targetClass, isReachable, distance, blocksRoute));
        }

        var decision = _targetSelector.SelectTarget(mapped);
        return new Dictionary
        {
            ["target_id"] = decision.TargetId ?? string.Empty,
            ["is_fallback_attack"] = decision.IsFallbackAttack,
            ["attack_event_target_id"] = decision.AttackEventTargetId ?? string.Empty,
            ["target_class"] = decision.TargetClass?.ToString() ?? string.Empty
        };
    }

    public Dictionary SelectTargetWithNavigation(GodotArray candidates, Vector2 origin)
    {
        var mapRid = ResolveNavigationMapRid();
        var navigationApiUsed = mapRid.IsValid;
        var mapped = new List<EnemyAiTargetCandidate>();

        foreach (var item in candidates)
        {
            if (!TryAsDictionary(item, out var candidate))
            {
                continue;
            }

            var id = ReadString(candidate, "id");
            var targetClass = ParseTargetClass(ReadString(candidate, "class"));
            var distance = ReadInt(candidate, "distance");
            var blocksRoute = ReadBool(candidate, "blocks_route_to_higher_priority");
            var explicitlyBlocked = ReadBool(candidate, "blocked");
            var navPosition = candidate.ContainsKey("nav_position")
                ? candidate["nav_position"].AsVector2()
                : origin;

            var pathPoints = QueryNavigationPathPoints(mapRid, origin, navPosition);
            var isReachable = pathPoints > 0 && !explicitlyBlocked;
            var measuredDistance = pathPoints > 0 ? pathPoints : distance;
            mapped.Add(new EnemyAiTargetCandidate(id, targetClass, isReachable, measuredDistance, blocksRoute));
        }

        var decision = _targetSelector.SelectTarget(mapped);
        return new Dictionary
        {
            ["target_id"] = decision.TargetId ?? string.Empty,
            ["is_fallback_attack"] = decision.IsFallbackAttack,
            ["attack_event_target_id"] = decision.AttackEventTargetId ?? string.Empty,
            ["target_class"] = decision.TargetClass?.ToString() ?? string.Empty,
            ["navigation_api_used"] = navigationApiUsed
        };
    }

    public Dictionary ProbeNavigationPath(Vector2 origin, Vector2 destination)
    {
        var mapRid = ResolveNavigationMapRid();
        var pathPoints = QueryNavigationPathPoints(mapRid, origin, destination);
        return new Dictionary
        {
            ["navigation_api_used"] = mapRid.IsValid,
            ["path_points"] = pathPoints
        };
    }

    public bool CanHitLayer(int attackMask, int targetLayer)
    {
        return (attackMask & targetLayer) != 0;
    }

    public bool IsFriendlyFirePrevented(int attackMask, int friendlyLayer, int playerLayer)
    {
        var hitFriendly = CanHitLayer(attackMask, friendlyLayer);
        var hitPlayer = CanHitLayer(attackMask, playerLayer);
        return !hitFriendly && !hitPlayer;
    }

    public Dictionary SimulateBlockedMapFallback(int enemyCount, int timeoutTicks, int attackReadyTick)
    {
        var diagnostics = new Array<string>();
        var enemiesReachedFallbackAttack = 0;
        var deadlockCount = 0;

        for (var enemyIndex = 0; enemyIndex < enemyCount; enemyIndex++)
        {
            var attacked = false;
            for (var tick = 0; tick < timeoutTicks; tick++)
            {
                diagnostics.Add($"enemy_{enemyIndex}:fallback_decision_{tick}");
                if (tick >= attackReadyTick)
                {
                    attacked = true;
                    break;
                }
            }

            if (attacked)
            {
                enemiesReachedFallbackAttack++;
            }
            else
            {
                diagnostics.Add($"enemy_{enemyIndex}:deadlock");
                deadlockCount++;
            }
        }

        return new Dictionary
        {
            ["diagnostics"] = diagnostics,
            ["enemies_reached_fallback_attack"] = enemiesReachedFallbackAttack,
            ["enemy_count"] = enemyCount,
            ["timeout_ticks"] = timeoutTicks,
            ["deadlock_count"] = deadlockCount
        };
    }

    public Dictionary SimulateBlockedMapFallbackWithNavigation(
        int enemyCount,
        int timeoutTicks,
        int attackReadyTick,
        Vector2 origin,
        Vector2 destination)
    {
        var mapRid = ResolveNavigationMapRid();
        var navigationApiUsed = mapRid.IsValid;
        var diagnostics = new Array<string>();
        var enemiesReachedFallbackAttack = 0;
        var deadlockCount = 0;

        for (var enemyIndex = 0; enemyIndex < enemyCount; enemyIndex++)
        {
            var attacked = false;
            for (var tick = 0; tick < timeoutTicks; tick++)
            {
                var pathPoints = QueryNavigationPathPoints(mapRid, origin, destination);
                diagnostics.Add($"enemy_{enemyIndex}:fallback_decision_{tick}:path={pathPoints}");
                if (tick >= attackReadyTick)
                {
                    attacked = true;
                    break;
                }
            }

            if (attacked)
            {
                enemiesReachedFallbackAttack++;
            }
            else
            {
                diagnostics.Add($"enemy_{enemyIndex}:deadlock");
                deadlockCount++;
            }
        }

        return new Dictionary
        {
            ["diagnostics"] = diagnostics,
            ["enemies_reached_fallback_attack"] = enemiesReachedFallbackAttack,
            ["enemy_count"] = enemyCount,
            ["timeout_ticks"] = timeoutTicks,
            ["deadlock_count"] = deadlockCount,
            ["navigation_api_used"] = navigationApiUsed
        };
    }

    public Dictionary SelectNextReachableTarget(GodotArray candidates, string currentTargetId)
    {
        var mapped = new List<EnemyAiRetargetCandidate>();
        foreach (var item in candidates)
        {
            if (!TryAsDictionary(item, out var candidate))
            {
                continue;
            }

            var isValid = !candidate.ContainsKey("valid") || candidate["valid"].AsBool();
            if (!isValid)
            {
                continue;
            }

            mapped.Add(new EnemyAiRetargetCandidate(
                ReadString(candidate, "id"),
                ReadInt(candidate, "priority"),
                ReadBool(candidate, "reachable")));
        }

        var selected = _retargetingService.SelectNextReachableTarget(mapped, currentTargetId);
        return new Dictionary
        {
            ["target_id"] = selected?.TargetId ?? string.Empty,
            ["is_reachable"] = selected?.IsReachable ?? false
        };
    }

    public GodotArray BuildDeterminismTrace(GodotArray candidates, int seed, int steps)
    {
        var mapped = new List<EnemyAiDeterminismCandidate>();
        foreach (var item in candidates)
        {
            if (!TryAsDictionary(item, out var candidate))
            {
                continue;
            }

            mapped.Add(new EnemyAiDeterminismCandidate(
                ReadString(candidate, "id"),
                ReadInt(candidate, "priority"),
                ReadBool(candidate, "blocked")));
        }

        var state = EnemyAiDeterminismState.Create(mapped.ToArray());
        var decisions = _determinismService.RunLoop(state, seed, steps);
        var trace = new GodotArray();
        foreach (var decision in decisions)
        {
            trace.Add(decision);
        }

        return trace;
    }

    protected static bool TryAsDictionary(Variant value, out Dictionary dictionary)
    {
        dictionary = new Dictionary();
        if (value.VariantType != Variant.Type.Dictionary)
        {
            return false;
        }

        dictionary = (Dictionary)value;
        return true;
    }

    protected static string ReadString(Dictionary dictionary, string key)
    {
        return dictionary.ContainsKey(key) ? dictionary[key].ToString() : string.Empty;
    }

    protected static bool ReadBool(Dictionary dictionary, string key)
    {
        return dictionary.ContainsKey(key) && dictionary[key].AsBool();
    }

    protected static int ReadInt(Dictionary dictionary, string key)
    {
        return dictionary.ContainsKey(key) ? dictionary[key].AsInt32() : 0;
    }

    protected static EnemyTargetClass ParseTargetClass(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "unit" => EnemyTargetClass.Unit,
            "castle" => EnemyTargetClass.Castle,
            "armed_defense" => EnemyTargetClass.ArmedDefense,
            "wall" => EnemyTargetClass.WallGate,
            "gate" => EnemyTargetClass.WallGate,
            "wall/gate" => EnemyTargetClass.WallGate,
            "blocking_structure" => EnemyTargetClass.BlockingStructure,
            _ => EnemyTargetClass.Decoration
        };
    }

    private static int QueryNavigationPathPoints(Rid mapRid, Vector2 origin, Vector2 destination)
    {
        if (!mapRid.IsValid)
        {
            return 0;
        }

        var closestPoint = NavigationServer2D.MapGetClosestPoint(mapRid, destination);
        if (closestPoint.DistanceTo(destination) > 0.01f)
        {
            return 0;
        }

        var path = NavigationServer2D.MapGetPath(mapRid, origin, destination, optimize: true);
        if (path.Length == 0)
        {
            return 0;
        }

        var endPoint = path[path.Length - 1];
        var reachedDestination = endPoint.DistanceTo(destination) <= 0.01f;
        return reachedDestination ? path.Length : 0;
    }

    private Rid ResolveNavigationMapRid()
    {
        var viewport = GetViewport();
        if (viewport is null)
        {
            return default;
        }

        var world2D = viewport.GetWorld2D();
        return world2D?.NavigationMap ?? default;
    }
}
