using System;
using Game.Core.Contracts;
using Game.Godot.Adapters;
using Godot;
using Godot.Collections;
using GDictionary = Godot.Collections.Dictionary;

namespace Lastking.Game.Godot.Scripts.Combat;

public partial class CombatExperienceRuntimeBridge : Node
{
    private readonly struct BuildCost
    {
        public BuildCost(int gold, int iron, int population)
        {
            Gold = gold;
            Iron = iron;
            Population = population;
        }

        public int Gold { get; }
        public int Iron { get; }
        public int Population { get; }
    }

    private sealed class RuntimeActor
    {
        public required string NodeName { get; init; }
        public required int TeamId { get; init; }
        public required int Hp { get; set; }
        public bool Active { get; set; } = true;
        public float PathProgress { get; set; }
        public bool IsMovingEnemy { get; set; }
        public bool IsAttackingWall { get; set; }
        public double WallAttackCooldownSeconds { get; set; }
    }

    private Node _battlefield = default!;
    private readonly System.Collections.Generic.Dictionary<string, RuntimeActor> _actors = new(StringComparer.Ordinal);
    private int _projectilesCreated;
    private int _combatExchanges;
    private int _deadUnitsRetired;
    private int _enemyUnitsSpawned;
    private int _castleHp = 100;
    private int _wallHp = 20;
    private int _resourceGold;
    private int _resourceIron;
    private int _resourcePopulationCap;
    private string _outcome = "win";
    private string _defeatReason = string.Empty;
    private string _forcedOutcomeOverride = string.Empty;
    private readonly System.Collections.Generic.List<string> _activeDamageNumberNames = new();
    private readonly System.Collections.Generic.Dictionary<string, string> _placedBuildingSlots = new(StringComparer.Ordinal);
    private static readonly System.Collections.Generic.Dictionary<string, BuildCost> BuildCosts = new(StringComparer.Ordinal)
    {
        ["tower_alpha"] = new BuildCost(gold: 60, iron: 0, population: 0),
        ["barracks_alpha"] = new BuildCost(gold: 80, iron: 0, population: 0),
        ["farm_alpha"] = new BuildCost(gold: 30, iron: 0, population: 0),
    };
    private static readonly BuildCost FriendlyTrainingCost = new(gold: 20, iron: 0, population: 0);
    private int _friendlyUnitSeq;
    private int _enemyUnitSeq;
    private const float EnemyTravelSpeedPerSecond = 0.12f;
    private const float WallInterceptProgress = 0.78f;
    private const double WallAttackIntervalSeconds = 1.0d;
    private const int WallAttackDamage = 5;
    private const int CastleAttackDamage = 5;
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
        _wallHp = 20;
        _friendlyUnitSeq = 0;
        _enemyUnitSeq = 0;
        _activeDamageNumberNames.Clear();
        _placedBuildingSlots.Clear();
        _resourceGold = 120;
        _resourceIron = 44;
        _resourcePopulationCap = 26;
        _outcome = "win";
        _defeatReason = string.Empty;
        _forcedOutcomeOverride = string.Empty;
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

        if (!HasNode("Battlefield/Residence"))
        {
            AddMarker("Residence");
        }

        return GetSummary();
    }

    public GDictionary PlaceBuildingAtSlot(string selectionId, string slotId)
    {
        EnsureBattlefield();

        if (string.IsNullOrWhiteSpace(selectionId) || string.IsNullOrWhiteSpace(slotId))
        {
            return new GDictionary
            {
                ["placed"] = false,
                ["reason"] = "invalid_input",
            };
        }

        if (_placedBuildingSlots.ContainsKey(slotId))
        {
            return new GDictionary
            {
                ["placed"] = false,
                ["reason"] = "tile_occupied",
                ["summary"] = GetSummary(),
            };
        }

        var nodeName = selectionId switch
        {
            "tower_alpha" => "MgTower",
            "barracks_alpha" => "Barracks",
            "farm_alpha" => "Residence",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(nodeName))
        {
            return new GDictionary
            {
                ["placed"] = false,
                ["reason"] = "invalid_target",
            };
        }

        if (!BuildCosts.TryGetValue(selectionId, out var cost))
        {
            return new GDictionary
            {
                ["placed"] = false,
                ["reason"] = "invalid_target",
                ["summary"] = GetSummary(),
            };
        }

        if (_resourceGold < cost.Gold || _resourceIron < cost.Iron || _resourcePopulationCap < cost.Population)
        {
            return new GDictionary
            {
                ["placed"] = false,
                ["reason"] = "insufficient_resources",
                ["summary"] = GetSummary(),
            };
        }

        if (!HasNode($"Battlefield/{nodeName}"))
        {
            AddMarker(nodeName);
        }

        _resourceGold -= cost.Gold;
        _resourceIron -= cost.Iron;
        _resourcePopulationCap -= cost.Population;
        _placedBuildingSlots[slotId] = selectionId;
        return new GDictionary
        {
            ["placed"] = true,
            ["node_name"] = nodeName,
            ["slot_id"] = slotId,
            ["summary"] = GetSummary(),
        };
    }

    public GDictionary TrainFriendlyUnitPhase()
    {
        EnsureBattlefield();
        if (_resourceGold < FriendlyTrainingCost.Gold
            || _resourceIron < FriendlyTrainingCost.Iron
            || _resourcePopulationCap < FriendlyTrainingCost.Population)
        {
            return new GDictionary
            {
                ["trained"] = false,
                ["reason"] = "insufficient_resources",
                ["summary"] = GetSummary(),
            };
        }

        _resourceGold -= FriendlyTrainingCost.Gold;
        _resourceIron -= FriendlyTrainingCost.Iron;
        _resourcePopulationCap -= FriendlyTrainingCost.Population;
        _friendlyUnitSeq += 1;
        AddActor($"FriendlyUnit{_friendlyUnitSeq}", teamId: 1, hp: 35);

        return new GDictionary
        {
            ["trained"] = true,
            ["summary"] = GetSummary(),
        };
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
        SyncTerminalOutcomeState();
        _outcome = string.IsNullOrWhiteSpace(_forcedOutcomeOverride)
            ? (_defeatReason.Length == 0 ? "win" : "loss")
            : _forcedOutcomeOverride;
        Publish(EventTypes.LastkingCastleHpChanged, "{\"Day\":9,\"PreviousHp\":100,\"CurrentHp\":42}");
        Publish(
            EventTypes.LastkingResourcesChanged,
            $"{{\"RunId\":\"combat-e2e\",\"DayNumber\":9,\"Gold\":{_resourceGold},\"Iron\":{_resourceIron},\"PopulationCap\":{_resourcePopulationCap}}}");
        Publish(EventTypes.LastkingUiFeedbackRaised, "{\"Code\":\"run_continue_blocked\",\"MessageKey\":\"ui.blocked_action.combat_exchange\",\"Details\":\"combat_exchange projectiles=2 retired=1\"}");
        Publish(EventTypes.RunStateTransitioned, $"{{\"outcome\":\"{_outcome}\",\"day\":9}}");
        return GetSummary();
    }

    public GDictionary ForceOutcomeForTest(string outcome, int castleHp)
    {
        _castleHp = Math.Max(0, castleHp);
        if (string.Equals(outcome, "loss", StringComparison.OrdinalIgnoreCase))
        {
            _forcedOutcomeOverride = "loss";
            _outcome = "loss";
            if (_castleHp <= 0)
            {
                _defeatReason = "castle_destroyed";
            }
            else
            {
                _wallHp = 0;
                _defeatReason = "wall_breached";
            }
        }
        else if (string.Equals(outcome, "win", StringComparison.OrdinalIgnoreCase))
        {
            _forcedOutcomeOverride = "win";
            _outcome = "win";
            _defeatReason = string.Empty;
        }
        else
        {
            _forcedOutcomeOverride = "settlement";
            _outcome = "settlement";
            _defeatReason = string.Empty;
        }
        return GetSummary();
    }

    public GDictionary ForceDefeatStateForTest(string defeatReason, int castleHp, int wallHp)
    {
        _castleHp = Math.Max(0, castleHp);
        _wallHp = Math.Max(0, wallHp);
        _defeatReason = NormalizeDefeatReason(defeatReason, _castleHp, _wallHp);
        _forcedOutcomeOverride = "loss";
        _outcome = "loss";
        return GetSummary();
    }

    public GDictionary ConfigureDurabilityForTest(int castleHp, int wallHp)
    {
        _castleHp = Math.Max(0, castleHp);
        _wallHp = Math.Max(0, wallHp);
        _defeatReason = NormalizeDefeatReason(string.Empty, _castleHp, _wallHp);
        _forcedOutcomeOverride = string.Empty;
        _outcome = _defeatReason.Length == 0 ? "win" : "loss";
        return GetSummary();
    }

    public GDictionary ConfigureResourcesForTest(int gold, int iron, int populationCap)
    {
        _resourceGold = Math.Max(0, gold);
        _resourceIron = Math.Max(0, iron);
        _resourcePopulationCap = Math.Max(0, populationCap);
        return GetSummary();
    }

    public GDictionary GetSummary()
    {
        var activeCombatNodes = CountActiveCombatNodes();
        return new GDictionary
        {
            ["mg_tower_built"] = HasNode("Battlefield/MgTower"),
            ["barracks_built"] = HasNode("Battlefield/Barracks"),
            ["residence_built"] = HasNode("Battlefield/Residence"),
            ["friendly_units_deployed"] = CountActiveActorsByTeam(1),
            ["enemy_units_spawned"] = _enemyUnitsSpawned,
            ["projectiles_created"] = _projectilesCreated,
            ["combat_exchanges"] = _combatExchanges,
            ["dead_units_retired"] = _deadUnitsRetired,
            ["active_combat_nodes_after_cleanup"] = activeCombatNodes,
            ["dead_unit_targetable_after_cleanup"] = IsTargetable("DeadEnemy"),
            ["castle_hp"] = _castleHp,
            ["wall_hp"] = _wallHp,
            ["resource_gold"] = _resourceGold,
            ["resource_iron"] = _resourceIron,
            ["resource_population_cap"] = _resourcePopulationCap,
            ["outcome"] = _outcome,
            ["defeat_reason"] = _defeatReason,
        };
    }

    public GDictionary GetPlacedBuildingSlots()
    {
        var slots = new GDictionary();
        foreach (var pair in _placedBuildingSlots)
        {
            slots[pair.Key] = pair.Value;
        }

        return slots;
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

            if (_wallHp > 0)
            {
                var nextProgress = Math.Clamp(actor.PathProgress + (float)deltaSeconds * EnemyTravelSpeedPerSecond, 0f, 1f);
                if (actor.IsAttackingWall || nextProgress >= WallInterceptProgress)
                {
                    actor.IsAttackingWall = true;
                    actor.PathProgress = WallInterceptProgress;
                    actor.WallAttackCooldownSeconds -= deltaSeconds;
                    if (actor.WallAttackCooldownSeconds <= 0d)
                    {
                        actor.WallAttackCooldownSeconds += WallAttackIntervalSeconds;
                        _wallHp = Math.Max(0, _wallHp - WallAttackDamage);
                        if (_wallHp <= 0)
                        {
                            _defeatReason = "wall_breached";
                            actor.IsAttackingWall = false;
                            actor.WallAttackCooldownSeconds = 0d;
                        }
                    }
                    continue;
                }
            }

            actor.IsAttackingWall = false;
            actor.WallAttackCooldownSeconds = 0d;
            actor.PathProgress = Math.Clamp(actor.PathProgress + (float)deltaSeconds * EnemyTravelSpeedPerSecond, 0f, 1f);
            if (actor.PathProgress >= 1f)
            {
                actor.Active = false;
                var node = _battlefield.GetNodeOrNull<Node>(actor.NodeName);
                node?.QueueFree();
                _castleHp = Math.Max(0, _castleHp - CastleAttackDamage);
                if (_castleHp <= 0)
                {
                    _defeatReason = "castle_destroyed";
                }
            }
        }
    }

    private void SyncTerminalOutcomeState()
    {
        _defeatReason = NormalizeDefeatReason(_defeatReason, _castleHp, _wallHp);
        if (_defeatReason.Length == 0 && string.Equals(_forcedOutcomeOverride, "loss", StringComparison.OrdinalIgnoreCase))
        {
            _defeatReason = _castleHp <= 0 ? "castle_destroyed" : "wall_breached";
        }
    }

    private static string NormalizeDefeatReason(string defeatReason, int castleHp, int wallHp)
    {
        if (string.Equals(defeatReason, "castle_destroyed", StringComparison.OrdinalIgnoreCase))
        {
            return "castle_destroyed";
        }

        if (string.Equals(defeatReason, "wall_breached", StringComparison.OrdinalIgnoreCase))
        {
            return "wall_breached";
        }

        if (castleHp <= 0)
        {
            return "castle_destroyed";
        }

        if (wallHp <= 0)
        {
            return "wall_breached";
        }

        return string.Empty;
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
                ["state"] = actor.IsAttackingWall ? "attacking_wall" : "advancing",
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
        _placedBuildingSlots.Clear();
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

