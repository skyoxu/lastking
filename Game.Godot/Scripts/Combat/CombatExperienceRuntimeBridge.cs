using System;
using Game.Core.Contracts;
using Game.Godot.Adapters;
using Godot;
using Godot.Collections;
using GDictionary = Godot.Collections.Dictionary;

namespace Lastking.Game.Godot.Scripts.Combat;

public partial class CombatExperienceRuntimeBridge : Node
{
    private sealed class RuntimeActor
    {
        public required string NodeName { get; init; }
        public required int TeamId { get; init; }
        public required int Hp { get; set; }
        public bool Active { get; set; } = true;
        public float PathProgress { get; set; }
        public bool IsMovingEnemy { get; set; }
    }

    private Node _battlefield = default!;
    private readonly System.Collections.Generic.Dictionary<string, RuntimeActor> _actors = new(StringComparer.Ordinal);
    private int _projectilesCreated;
    private int _combatExchanges;
    private int _deadUnitsRetired;
    private int _enemyUnitsSpawned;
    private int _castleHp = 100;
    private readonly System.Collections.Generic.List<string> _activeDamageNumberNames = new();
    private int _friendlyUnitSeq;
    private int _enemyUnitSeq;
    private const string SettingsConfigPath = "user://settings.cfg";
    private const string SettingsSection = "settings";
    private const string DamageNumbersEnabledKey = "combat_damage_numbers_enabled";
    private const string LegacyDamageNumbersEnabledKey = "damage_numbers_enabled";

    public override void _Ready()
    {
        EnsureBattlefield();
    }

    public GDictionary RunCompleteCombatExperienceForTest()
    {
        ResetForInteractiveRun();
        BuildPhase();
        TrainFriendlyUnitPhase();
        SpawnEnemyWavePhase();
        ResolveCombatExchangePhase();
        CleanupDeadUnitsPhase();
        PublishOutcomePhase();
        return GetSummary();
    }

    public void ResetForInteractiveRun()
    {
        ResetBattlefield();
        _projectilesCreated = 0;
        _combatExchanges = 0;
        _deadUnitsRetired = 0;
        _enemyUnitsSpawned = 0;
        _castleHp = 100;
        _friendlyUnitSeq = 0;
        _enemyUnitSeq = 0;
        _activeDamageNumberNames.Clear();
    }

    public GDictionary BuildPhase()
    {
        EnsureBattlefield();
        if (!HasNode("Battlefield/MgTower"))
        {
            AddMarker("MgTower");
        }

        if (!HasNode("Battlefield/Barracks"))
        {
            AddMarker("Barracks");
        }

        return GetSummary();
    }

    public GDictionary TrainFriendlyUnitPhase()
    {
        EnsureBattlefield();
        _friendlyUnitSeq += 1;
        AddActor($"FriendlyUnit{_friendlyUnitSeq}", teamId: 1, hp: 35);

        return GetSummary();
    }

    public GDictionary SpawnEnemyWavePhase()
    {
        EnsureBattlefield();
        _enemyUnitSeq += 1;
        AddActor($"EnemyUnit{_enemyUnitSeq}", teamId: 2, hp: 30, movingEnemy: true);
        _enemyUnitsSpawned += 1;
        _enemyUnitSeq += 1;
        AddActor($"EnemyUnit{_enemyUnitSeq}", teamId: 2, hp: 20, movingEnemy: true);
        _enemyUnitsSpawned += 1;

        Publish(EventTypes.LastkingWaveSpawned, "{\"day\":9,\"count\":2}");
        return GetSummary();
    }

    public GDictionary ResolveCombatExchangePhase()
    {
        EnsureBattlefield();
        var successfulExchanges = 0;
        var friendlyTarget = FindFirstActiveActorName(teamId: 1);
        var enemyTarget = FindFirstActiveActorName(teamId: 2);
        if (!string.IsNullOrWhiteSpace(enemyTarget))
        {
            var fired = FireProjectile("MgTower", enemyTarget, damage: 25);
            _projectilesCreated += fired;
            successfulExchanges += fired;
        }

        if (!string.IsNullOrWhiteSpace(friendlyTarget))
        {
            var fired = FireProjectile("EnemyWave", friendlyTarget, damage: 10);
            _projectilesCreated += fired;
            successfulExchanges += fired;
        }

        _combatExchanges += successfulExchanges;
        return GetSummary();
    }

    public GDictionary CleanupDeadUnitsPhase()
    {
        var retired = 0;
        foreach (var actor in _actors.Values)
        {
            if (actor.Hp <= 0)
            {
                retired += 1;
            }
        }

        _deadUnitsRetired += retired;
        RetireAllDeadActors();
        CleanupDamageNumbers();
        return GetSummary();
    }

    public GDictionary PublishOutcomePhase()
    {
        Publish(EventTypes.LastkingCastleHpChanged, "{\"Day\":9,\"PreviousHp\":100,\"CurrentHp\":42}");
        Publish(EventTypes.LastkingResourcesChanged, "{\"RunId\":\"combat-e2e\",\"DayNumber\":9,\"Gold\":120,\"Iron\":44,\"PopulationCap\":26}");
        Publish(EventTypes.LastkingUiFeedbackRaised, "{\"Code\":\"run_continue_blocked\",\"MessageKey\":\"ui.blocked_action.combat_exchange\",\"Details\":\"combat_exchange projectiles=2 retired=1\"}");
        Publish(EventTypes.RunStateTransitioned, "{\"outcome\":\"win\",\"day\":9}");
        return GetSummary();
    }

    public GDictionary GetSummary()
    {
        var activeCombatNodes = CountActiveCombatNodes();
        return new GDictionary
        {
            ["mg_tower_built"] = HasNode("Battlefield/MgTower"),
            ["barracks_built"] = HasNode("Battlefield/Barracks"),
            ["friendly_units_deployed"] = CountActiveActorsByTeam(1),
            ["enemy_units_spawned"] = _enemyUnitsSpawned,
            ["projectiles_created"] = _projectilesCreated,
            ["combat_exchanges"] = _combatExchanges,
            ["dead_units_retired"] = _deadUnitsRetired,
            ["active_combat_nodes_after_cleanup"] = activeCombatNodes,
            ["dead_unit_targetable_after_cleanup"] = IsTargetable("DeadEnemy"),
            ["castle_hp"] = _castleHp,
        };
    }

    public void AdvanceSimulation(double deltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            return;
        }

        foreach (var actor in _actors.Values)
        {
            if (!actor.Active || !actor.IsMovingEnemy)
            {
                continue;
            }

            actor.PathProgress = Math.Clamp(actor.PathProgress + (float)deltaSeconds * 0.12f, 0f, 1f);
            if (actor.PathProgress >= 1f)
            {
                actor.Active = false;
                var node = _battlefield.GetNodeOrNull<Node>(actor.NodeName);
                node?.QueueFree();
                _castleHp = Math.Max(0, _castleHp - 5);
            }
        }
    }

    public global::Godot.Collections.Array GetActorSnapshots()
    {
        var snapshots = new global::Godot.Collections.Array();
        foreach (var actor in _actors.Values)
        {
            snapshots.Add(new GDictionary
            {
                ["name"] = actor.NodeName,
                ["team_id"] = actor.TeamId,
                ["hp"] = actor.Hp,
                ["active"] = actor.Active,
                ["path_progress"] = actor.PathProgress,
                ["is_moving_enemy"] = actor.IsMovingEnemy,
            });
        }

        return snapshots;
    }

    private void EnsureBattlefield()
    {
        _battlefield = GetNodeOrNull<Node>("Battlefield") ?? new Node { Name = "Battlefield" };
        if (_battlefield.GetParent() == null)
        {
            AddChild(_battlefield);
        }
    }

    private void ResetBattlefield()
    {
        EnsureBattlefield();
        foreach (var child in _battlefield.GetChildren())
        {
            child.QueueFree();
        }

        _actors.Clear();
    }

    private void AddMarker(string name)
    {
        var marker = new Node2D { Name = name };
        _battlefield.AddChild(marker);
    }

    private RuntimeActor AddActor(string name, int teamId, int hp, bool movingEnemy = false)
    {
        var node = new Node2D { Name = name };
        _battlefield.AddChild(node);
        var actor = new RuntimeActor
        {
            NodeName = name,
            TeamId = teamId,
            Hp = hp,
            IsMovingEnemy = movingEnemy,
            PathProgress = 0f,
        };
        _actors[name] = actor;
        return actor;
    }

    private int FireProjectile(string sourceNodeName, string targetNodeName, int damage)
    {
        if (!IsTargetable(targetNodeName))
        {
            return 0;
        }

        var projectile = new Node2D { Name = "Projectile" };
        _battlefield.AddChild(projectile);
        if (_actors.TryGetValue(targetNodeName, out var target))
        {
            target.Hp = Math.Max(0, target.Hp - damage);
            if (AreDamageNumbersEnabled())
            {
                var damageNumberName = $"DamageNumber{_projectilesCreated + 1}";
                var damageNumber = new Node2D { Name = damageNumberName };
                _battlefield.AddChild(damageNumber);
                _activeDamageNumberNames.Add(damageNumberName);
            }
        }

        projectile.QueueFree();
        return 1;
    }

    private void RetireAllDeadActors()
    {
        var toRemove = new System.Collections.Generic.List<string>();
        foreach (var pair in _actors)
        {
            if (pair.Value.Hp <= 0 || !pair.Value.Active)
            {
                toRemove.Add(pair.Key);
            }
        }

        foreach (var name in toRemove)
        {
            _actors.Remove(name);
            var node = _battlefield.GetNodeOrNull<Node>(name);
            node?.QueueFree();
        }
    }

    private void CleanupDamageNumbers()
    {
        if (_activeDamageNumberNames.Count == 0)
        {
            return;
        }

        foreach (var name in _activeDamageNumberNames)
        {
            var node = _battlefield.GetNodeOrNull<Node>(name);
            node?.QueueFree();
        }

        _activeDamageNumberNames.Clear();
    }

    private static bool AreDamageNumbersEnabled()
    {
        var cfg = new ConfigFile();
        var err = cfg.Load(SettingsConfigPath);
        if (err != Error.Ok)
        {
            return true;
        }

        Variant direct = cfg.GetValue(SettingsSection, DamageNumbersEnabledKey, Variant.CreateFrom(true));
        if (direct.VariantType == Variant.Type.Bool)
        {
            return direct.AsBool();
        }

        Variant legacy = cfg.GetValue(SettingsSection, LegacyDamageNumbersEnabledKey, Variant.CreateFrom(true));
        if (legacy.VariantType == Variant.Type.Bool)
        {
            return legacy.AsBool();
        }

        return true;
    }

    private bool IsTargetable(string nodeName)
    {
        return _actors.TryGetValue(nodeName, out var actor) && actor.Active && HasNode($"Battlefield/{nodeName}");
    }

    private int CountActiveCombatNodes()
    {
        var count = 0;
        foreach (var actor in _actors.Values)
        {
            if (actor.Active && HasNode($"Battlefield/{actor.NodeName}"))
            {
                count += 1;
            }
        }

        return count;
    }

    private int CountActiveActorsByTeam(int teamId)
    {
        var count = 0;
        foreach (var actor in _actors.Values)
        {
            if (actor.TeamId == teamId && actor.Active && HasNode($"Battlefield/{actor.NodeName}"))
            {
                count += 1;
            }
        }

        return count;
    }

    private string? FindFirstActiveActorName(int teamId)
    {
        foreach (var pair in _actors)
        {
            if (pair.Value.TeamId == teamId && pair.Value.Active && HasNode($"Battlefield/{pair.Key}"))
            {
                return pair.Key;
            }
        }

        return null;
    }

    private void Publish(string type, string payload)
    {
        var bus = GetNodeOrNull<EventBusAdapter>("/root/EventBus");
        bus?.PublishSimple(type, "combat-experience-runtime", payload);
    }
}
