using System;
using System.Linq;
using Game.Core.Services;
using Game.Core.Contracts;
using Game.Godot.Adapters;
using Godot;
using Godot.Collections;
using System.IO;
using System.Text.Json;
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
        public int WallAttackDamage { get; set; }
        public float MoveSpeedProgressPerSecond { get; set; }
        public float AttackRangePx { get; set; }
        public double AttackIntervalSeconds { get; set; }
        public string VisualTier { get; set; } = "grunt";
        public bool IsElite { get; set; }
        public bool IsBoss { get; set; }
        public SpawnLane Lane { get; set; } = SpawnLane.Right;
    }

    private sealed class RuntimeEnemyProfile
    {
        public required string EnemyId { get; init; }
        public required int Health { get; init; }
        public required int Damage { get; init; }
        public required float MoveSpeedProgressPerSecond { get; init; }
        public required float AttackRangePx { get; init; }
        public required double AttackIntervalSeconds { get; init; }
        public required string VisualTier { get; init; }
        public required bool IsElite { get; init; }
        public required bool IsBoss { get; init; }
    }

    private readonly struct BattleMapWaveSpawn
    {
        public BattleMapWaveSpawn(string enemyId, SpawnLane lane)
        {
            EnemyId = enemyId;
            Lane = lane;
        }

        public string EnemyId { get; }
        public SpawnLane Lane { get; }
    }

    private sealed class BattleMapWaveDefinition
    {
        public BattleMapWaveDefinition(System.Collections.Generic.IReadOnlyList<BattleMapWaveSpawn> spawns)
        {
            Spawns = spawns;
        }

        public System.Collections.Generic.IReadOnlyList<BattleMapWaveSpawn> Spawns { get; }
    }

    private enum SpawnLane
    {
        Left = 0,
        Right = 1,
    }

    private readonly struct TowerPlacementRuntime
    {
        public TowerPlacementRuntime(string slotId, string nodeName, Vector2 center)
        {
            SlotId = slotId;
            NodeName = nodeName;
            Center = center;
        }

        public string SlotId { get; }
        public string NodeName { get; }
        public Vector2 Center { get; }
    }

    private readonly struct TowerCombatProfile
    {
        public TowerCombatProfile(float rangePx, double attackIntervalSeconds, int attackDamage, string targetingMode)
        {
            RangePx = rangePx;
            AttackIntervalSeconds = attackIntervalSeconds;
            AttackDamage = attackDamage;
            TargetingMode = targetingMode;
        }

        public float RangePx { get; }
        public double AttackIntervalSeconds { get; }
        public int AttackDamage { get; }
        public string TargetingMode { get; }
    }

    private Node _battlefield = default!;
    private readonly System.Collections.Generic.Dictionary<string, RuntimeActor> _actors = new(StringComparer.Ordinal);
    private readonly System.Collections.Generic.Dictionary<string, string> _placedBuildingNodeNames = new(StringComparer.Ordinal);
    private int _projectilesCreated;
    private int _combatExchanges;
    private int _deadUnitsRetired;
    private int _enemyUnitsSpawned;
    private int _castleHp = 100;
    private int _wallHp = 100;
    private int _resourceGold;
    private int _resourceIron;
    private int _resourcePopulationCap;
    private string _outcome = "win";
    private string _defeatReason = string.Empty;
    private string _forcedOutcomeOverride = string.Empty;
    private readonly System.Collections.Generic.List<string> _activeDamageNumberNames = new();
    private readonly System.Collections.Generic.Dictionary<string, string> _placedBuildingSlots = new(StringComparer.Ordinal);
    private readonly System.Collections.Generic.List<GDictionary> _towerShotEvents = new();
    private static readonly System.Collections.Generic.Dictionary<string, BuildCost> BuildCosts = new(StringComparer.Ordinal)
    {
        ["tower_alpha"] = new BuildCost(gold: 60, iron: 0, population: 0),
        ["tower_beta"] = new BuildCost(gold: 90, iron: 10, population: 0),
        ["barracks_alpha"] = new BuildCost(gold: 80, iron: 0, population: 0),
        ["farm_alpha"] = new BuildCost(gold: 30, iron: 0, population: 0),
    };
    private static readonly BuildCost FriendlyTrainingCost = new(gold: 20, iron: 0, population: 0);
    private int _friendlyUnitSeq;
    private int _enemyUnitSeq;
    private const float EnemyTravelSpeedPerSecond = 0.12f;
    private const double WallAttackIntervalSeconds = 2.0d;
    private const int WallAttackDamage = 5;
    private const int CastleAttackDamage = 5;
    private static readonly Vector2[] RightLanePathPoints =
    {
        new(1488f, 312f),
        new(96f, 312f),
    };
    private static readonly Vector2[] LeftLanePathPoints =
    {
        new(96f, 312f),
        new(1488f, 312f),
    };
    private static readonly System.Collections.Generic.Dictionary<string, Vector2> RuntimeMarkerPositions = new(StringComparer.Ordinal)
    {
        ["MgTower"] = new Vector2(792f, 312f),
        ["Barracks"] = new Vector2(648f, 312f),
        ["Residence"] = new Vector2(936f, 312f),
    };
    private const float LeftWallCenterX = 600f;
    private const float RightWallCenterX = 984f;
    private const string SettingsConfigPath = "user://settings.cfg";
    private const string SettingsSection = "settings";
    private const string DamageNumbersEnabledKey = "combat_damage_numbers_enabled";
    private const string LegacyDamageNumbersEnabledKey = "damage_numbers_enabled";
    private string _enemyRuntimeConfigJson = string.Empty;
    private bool _testEnemyRuntimeConfigOverrideActive;
    private readonly ConfigManager _enemyRuntimeConfigManager = new();
    private readonly EnemyConfigRuntimeResolver _enemyConfigResolver = new();
    private bool _battleMapAutoSpawnEnabled = true;
    private int _battleMapSpawnCadenceSeconds = 8;
    private int _battleMapConfiguredWaveSize = 2;
    private readonly System.Collections.Generic.List<BattleMapWaveDefinition> _battleMapWaveSequence = new();
    private int _battleMapWaveCursor;
    private const string BattleMapRuntimeConfigPath = "res://Game.Godot/Config/battlemap-runtime.config.json";
    private readonly System.Collections.Generic.Dictionary<string, double> _towerAttackCooldownSecondsByNodeName = new(StringComparer.Ordinal);
    private string _lastTowerTargetName = string.Empty;
    private readonly System.Collections.Generic.Dictionary<string, string> _lastTowerTargetNameByNodeName = new(StringComparer.Ordinal);
    private static readonly TowerCombatProfile MgTowerProfile = new(
        rangePx: 300f,
        attackIntervalSeconds: 2.0d,
        attackDamage: 25,
        targetingMode: "frontline_finisher_split");
    private static readonly TowerCombatProfile SniperTowerProfile = new(
        rangePx: 420f,
        attackIntervalSeconds: 1.2d,
        attackDamage: 14,
        targetingMode: "frontline_pressure_split");

    public override void _Ready()
    {
        EnsureBattlefield();
    }

    public GDictionary RunCompleteCombatExperienceForTest()
    {
        ResetForInteractiveRun();
        EnsureBattlefield();
        if (!HasNode("Battlefield/MgTower"))
        {
            AddMarker("MgTower");
        }

        if (!HasNode("Battlefield/Barracks"))
        {
            AddMarker("Barracks");
        }
        TrainFriendlyUnitPhase();
        SpawnEnemyWavePhase();
        ResolveCombatExchangePhase();
        CleanupDeadUnitsPhase();
        PublishOutcomePhase();
        return GetSummary();
    }

    public void ResetForInteractiveRun()
    {
        LoadBattleMapRuntimeConfig();
        ResetBattlefield();
        _projectilesCreated = 0;
        _combatExchanges = 0;
        _deadUnitsRetired = 0;
        _enemyUnitsSpawned = 0;
        _castleHp = 100;
        _wallHp = 100;
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
        _towerAttackCooldownSecondsByNodeName.Clear();
        _lastTowerTargetName = string.Empty;
        _lastTowerTargetNameByNodeName.Clear();
        _towerShotEvents.Clear();
        _battleMapWaveCursor = 0;
        if (_enemyRuntimeConfigManager.Snapshot.CastleStartHp > 0)
        {
            _castleHp = _enemyRuntimeConfigManager.Snapshot.CastleStartHp;
        }
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

        var nodeName = ResolvePlacedBuildingNodeName(selectionId, slotId);

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
            AddMarker(nodeName, ResolveMarkerPosition(nodeName, slotId));
        }

        _resourceGold -= cost.Gold;
        _resourceIron -= cost.Iron;
        _resourcePopulationCap -= cost.Population;
        _placedBuildingSlots[slotId] = selectionId;
        _placedBuildingNodeNames[slotId] = nodeName;
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
        var spawnedCount = 0;
        foreach (var spawn in ResolveCurrentWaveSpawns())
        {
            var enemyProfile = spawn.Profile;
            _enemyUnitSeq += 1;
            AddActor($"EnemyUnit{_enemyUnitSeq}", teamId: 2, hp: enemyProfile.Health, movingEnemy: true, enemyProfile: enemyProfile, lane: spawn.Lane);
            _enemyUnitsSpawned += 1;
            spawnedCount += 1;
        }

        Publish(EventTypes.LastkingWaveSpawned, $"{{\"day\":9,\"count\":{spawnedCount}}}");
        return GetSummary();
    }

    public int GetSpawnCadenceSeconds()
    {
        return Math.Max(1, _battleMapSpawnCadenceSeconds);
    }

    public int GetConfiguredWaveSize()
    {
        if (_battleMapWaveSequence.Count > 0)
        {
            var waveIndex = Math.Clamp(_battleMapWaveCursor, 0, _battleMapWaveSequence.Count - 1);
            return _battleMapWaveSequence[waveIndex].Spawns.Count;
        }

        return Math.Max(1, _battleMapConfiguredWaveSize);
    }

    public bool IsAutoSpawnEnabled()
    {
        return _battleMapAutoSpawnEnabled;
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
            ["mg_tower_built"] = GetTowerNodeNames().Count > 0,
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

    public GDictionary GetPlacedBuildingNodeNames()
    {
        var nodes = new GDictionary();
        foreach (var pair in _placedBuildingSlots)
        {
            var nodeName = _placedBuildingNodeNames.TryGetValue(pair.Key, out var storedNodeName)
                && !string.IsNullOrWhiteSpace(storedNodeName)
                ? storedNodeName
                : ResolvePlacedBuildingNodeName(pair.Value, pair.Key);
            nodes[pair.Key] = nodeName;
        }

        return nodes;
    }

    public global::Godot.Collections.Array ConsumeTowerShotEvents()
    {
        var events = new global::Godot.Collections.Array();
        foreach (var shotEvent in _towerShotEvents)
        {
            events.Add(shotEvent);
        }

        _towerShotEvents.Clear();
        return events;
    }

    public GDictionary DebugDescribeResolvedEnemyProfiles()
    {
        var profiles = ResolveActiveEnemyProfiles();
        var firstRange = profiles.Length > 0 ? profiles[0].AttackRangePx : -1f;
        var firstInterval = profiles.Length > 0 ? profiles[0].AttackIntervalSeconds : -1d;
        return new GDictionary
        {
            ["override_active"] = _testEnemyRuntimeConfigOverrideActive,
            ["config_json_set"] = !string.IsNullOrWhiteSpace(_enemyRuntimeConfigJson),
            ["profile_count"] = profiles.Length,
            ["first_range_px"] = firstRange,
            ["first_interval_seconds"] = firstInterval,
        };
    }

    public void AdvanceSimulation(double deltaSeconds)
    {
        if (deltaSeconds <= 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(_defeatReason))
        {
            return;
        }

        AdvanceTowerAutoAttack(deltaSeconds);

        foreach (var actor in _actors.Values)
        {
            if (!actor.Active || !actor.IsMovingEnemy)
            {
                continue;
            }

            if (_wallHp > 0)
            {
                var speedPerSecond = actor.IsMovingEnemy && actor.AttackIntervalSeconds >= 0d
                    ? ResolveTravelSpeedProgressPerSecond(actor)
                    : EnemyTravelSpeedPerSecond;
                var nextProgress = Math.Clamp(actor.PathProgress + (float)deltaSeconds * speedPerSecond, 0f, 1f);
                var attackEngageProgress = ResolveWallAttackEngageProgress(actor.PathProgress, nextProgress, actor.AttackRangePx, actor.Lane);
                if (actor.IsAttackingWall || attackEngageProgress.HasValue)
                {
                    var enteringWallAttack = !actor.IsAttackingWall;
                    actor.IsAttackingWall = true;
                    actor.PathProgress = attackEngageProgress ?? actor.PathProgress;
                    SyncActorNodePosition(actor);
                    if (enteringWallAttack)
                    {
                        actor.WallAttackCooldownSeconds = actor.AttackIntervalSeconds;
                        continue;
                    }
                    actor.WallAttackCooldownSeconds -= deltaSeconds;
                    if (actor.WallAttackCooldownSeconds <= 0.000001d)
                    {
                        actor.WallAttackCooldownSeconds += actor.AttackIntervalSeconds;
                        _wallHp = Math.Max(0, _wallHp - actor.WallAttackDamage);
                        if (_wallHp <= 0)
                        {
                            _defeatReason = "wall_breached";
                            actor.IsAttackingWall = false;
                            actor.WallAttackCooldownSeconds = 0d;
                            return;
                        }
                    }
                    continue;
                }
            }

            actor.IsAttackingWall = false;
            actor.WallAttackCooldownSeconds = 0d;
            actor.PathProgress = Math.Clamp(actor.PathProgress + (float)deltaSeconds * ResolveTravelSpeedProgressPerSecond(actor), 0f, 1f);
            SyncActorNodePosition(actor);
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
            var world = SamplePath(actor.PathProgress, actor.Lane);
            snapshots.Add(new GDictionary
            {
                ["name"] = actor.NodeName,
                ["team_id"] = actor.TeamId,
                ["hp"] = actor.Hp,
                ["active"] = actor.Active,
                ["path_progress"] = actor.PathProgress,
                ["world_x"] = world.X,
                ["world_y"] = world.Y,
                ["is_moving_enemy"] = actor.IsMovingEnemy,
                ["state"] = actor.IsAttackingWall ? "attacking_wall" : "advancing",
                ["attack_range_px"] = actor.AttackRangePx,
                ["attack_interval_seconds"] = actor.AttackIntervalSeconds,
                ["visual_tier"] = actor.VisualTier,
                ["is_elite"] = actor.IsElite,
                ["is_boss"] = actor.IsBoss,
                ["lane"] = actor.Lane == SpawnLane.Left ? "left" : "right",
            });
        }

        return snapshots;
    }

    public float GetTowerRangePx()
    {
        return MgTowerProfile.RangePx;
    }

    public GDictionary GetTowerCombatDebugSnapshot()
    {
        var towerNodeNames = GetTowerNodeNames();
        var targetName = _lastTowerTargetName;
        var targetHp = -1;
        var targetActive = false;
        if (!string.IsNullOrWhiteSpace(targetName) && _actors.TryGetValue(targetName, out var actor))
        {
            targetHp = actor.Hp;
            targetActive = actor.Active;
        }

        var towerNodes = new global::Godot.Collections.Array();
        foreach (var towerNodeName in towerNodeNames)
        {
            towerNodes.Add(towerNodeName);
        }

        var towerTargets = new global::Godot.Collections.Array();
        foreach (var towerNodeName in towerNodeNames)
        {
            var targetNameByTower = _lastTowerTargetNameByNodeName.TryGetValue(towerNodeName, out var storedTargetName)
                ? storedTargetName
                : string.Empty;
            var cooldownByTower = _towerAttackCooldownSecondsByNodeName.TryGetValue(towerNodeName, out var storedCooldown)
                ? storedCooldown
                : 0d;
            var towerProfile = ResolveTowerCombatProfile(towerNodeName);
            towerTargets.Add(new GDictionary
            {
                ["tower_name"] = towerNodeName,
                ["target_name"] = string.IsNullOrWhiteSpace(targetNameByTower) ? "n/a" : targetNameByTower,
                ["cooldown_seconds"] = cooldownByTower,
                ["range_px"] = towerProfile.RangePx,
                ["attack_damage"] = towerProfile.AttackDamage,
                ["attack_interval_seconds"] = towerProfile.AttackIntervalSeconds,
                ["targeting_mode"] = towerProfile.TargetingMode,
            });
        }

        return new GDictionary
        {
            ["target_name"] = string.IsNullOrWhiteSpace(targetName) ? "n/a" : targetName,
            ["target_hp"] = targetHp,
            ["target_active"] = targetActive,
            ["projectiles_created"] = _projectilesCreated,
            ["active_enemy_count"] = CountActiveActorsByTeam(2),
            ["tower_cooldown_seconds"] = ResolveTowerCooldownSeconds(),
            ["tower_count"] = towerNodeNames.Count,
            ["tower_nodes"] = towerNodes,
            ["tower_targets"] = towerTargets,
        };
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
        _placedBuildingNodeNames.Clear();
    }

    private void AddMarker(string name, Vector2? positionOverride = null)
    {
        var marker = new Node2D { Name = name };
        if (positionOverride.HasValue)
        {
            marker.Position = positionOverride.Value;
        }
        else if (RuntimeMarkerPositions.TryGetValue(name, out var position))
        {
            marker.Position = position;
        }
        _battlefield.AddChild(marker);
    }

    private RuntimeActor AddActor(string name, int teamId, int hp, bool movingEnemy = false, SpawnLane lane = SpawnLane.Right)
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
            WallAttackDamage = WallAttackDamage,
            MoveSpeedProgressPerSecond = EnemyTravelSpeedPerSecond,
            AttackRangePx = 0f,
            AttackIntervalSeconds = WallAttackIntervalSeconds,
            Lane = lane,
        };
        _actors[name] = actor;
        SyncActorNodePosition(actor);
        return actor;
    }

    private RuntimeActor AddActor(string name, int teamId, int hp, bool movingEnemy, RuntimeEnemyProfile enemyProfile, SpawnLane lane)
    {
        var actor = AddActor(name, teamId, hp, movingEnemy, lane);
        actor.WallAttackDamage = enemyProfile.Damage;
        actor.MoveSpeedProgressPerSecond = enemyProfile.MoveSpeedProgressPerSecond;
        actor.AttackRangePx = enemyProfile.AttackRangePx;
        actor.AttackIntervalSeconds = enemyProfile.AttackIntervalSeconds;
        actor.VisualTier = enemyProfile.VisualTier;
        actor.IsElite = enemyProfile.IsElite;
        actor.IsBoss = enemyProfile.IsBoss;
        SyncActorNodePosition(actor);
        return actor;
    }

    private void SyncActorNodePosition(RuntimeActor actor)
    {
        var node = _battlefield.GetNodeOrNull<Node2D>(actor.NodeName);
        if (node == null)
        {
            return;
        }

        node.Position = SamplePath(actor.PathProgress, actor.Lane);
    }

    private static float? ResolveWallInterceptProgress(float currentProgress, float nextProgress, SpawnLane lane)
    {
        var startPoint = SamplePath(currentProgress, lane);
        var endPoint = SamplePath(nextProgress, lane);

        if (TryResolveInterceptForWallX(startPoint, endPoint, LeftWallCenterX, out var leftT))
        {
            return Math.Clamp(currentProgress + ((nextProgress - currentProgress) * leftT), 0f, 1f);
        }

        if (TryResolveInterceptForWallX(startPoint, endPoint, RightWallCenterX, out var rightT))
        {
            return Math.Clamp(currentProgress + ((nextProgress - currentProgress) * rightT), 0f, 1f);
        }

        return null;
    }

    private static float? ResolveWallAttackEngageProgress(float currentProgress, float nextProgress, float attackRangePx, SpawnLane lane)
    {
        var startPoint = SamplePath(currentProgress, lane);
        var endPoint = SamplePath(nextProgress, lane);
        var bestProgress = ResolveWallRangeEngageProgressForX(currentProgress, nextProgress, startPoint, endPoint, LeftWallCenterX, attackRangePx);
        var rightProgress = ResolveWallRangeEngageProgressForX(currentProgress, nextProgress, startPoint, endPoint, RightWallCenterX, attackRangePx);
        if (!bestProgress.HasValue)
        {
            return rightProgress;
        }

        if (!rightProgress.HasValue)
        {
            return bestProgress;
        }

        return Math.Max(bestProgress.Value, rightProgress.Value);
    }

    private static float? ResolveWallRangeEngageProgressForX(
        float currentProgress,
        float nextProgress,
        Vector2 startPoint,
        Vector2 endPoint,
        float wallX,
        float attackRangePx)
    {
        var deltaX = endPoint.X - startPoint.X;
        if (Math.Abs(deltaX) <= 0.0001f)
        {
            return null;
        }

        var targetX = deltaX < 0f
            ? wallX + Math.Max(0f, attackRangePx)
            : wallX - Math.Max(0f, attackRangePx);
        var minX = Math.Min(startPoint.X, endPoint.X);
        var maxX = Math.Max(startPoint.X, endPoint.X);
        if (targetX < minX || targetX > maxX)
        {
            return null;
        }

        var segmentT = (targetX - startPoint.X) / deltaX;
        if (segmentT < 0f || segmentT > 1f)
        {
            return null;
        }

        return Math.Clamp(currentProgress + ((nextProgress - currentProgress) * segmentT), 0f, 1f);
    }

    private static bool TryResolveInterceptForWallX(Vector2 startPoint, Vector2 endPoint, float wallX, out float segmentT)
    {
        segmentT = 0f;
        var minX = Math.Min(startPoint.X, endPoint.X);
        var maxX = Math.Max(startPoint.X, endPoint.X);
        if (wallX < minX || wallX > maxX)
        {
            return false;
        }

        var deltaX = endPoint.X - startPoint.X;
        if (Math.Abs(deltaX) <= 0.0001f)
        {
            return false;
        }

        segmentT = (wallX - startPoint.X) / deltaX;
        return segmentT >= 0f && segmentT <= 1f;
    }

    private static Vector2 SamplePath(float progress, SpawnLane lane)
    {
        var p = Math.Clamp(progress, 0f, 1f);
        var pathPoints = lane == SpawnLane.Left ? LeftLanePathPoints : RightLanePathPoints;
        var segmentCount = pathPoints.Length - 1;
        if (segmentCount <= 0)
        {
            return Vector2.Zero;
        }

        var scaled = p * segmentCount;
        var index = Math.Min((int)Math.Floor(scaled), segmentCount - 1);
        var localT = scaled - index;
        return pathPoints[index].Lerp(pathPoints[index + 1], localT);
    }

    private int FireProjectile(string sourceNodeName, string targetNodeName, int damage)
    {
        if (!IsTargetable(targetNodeName))
        {
            return 0;
        }

        var sourceNode = _battlefield.GetNodeOrNull<Node2D>(sourceNodeName);
        var targetNode = _battlefield.GetNodeOrNull<Node2D>(targetNodeName);
        var projectile = new Node2D { Name = "Projectile" };
        _battlefield.AddChild(projectile);
        if (_actors.TryGetValue(targetNodeName, out var target))
        {
            var previousHp = target.Hp;
            target.Hp = Math.Max(0, target.Hp - damage);
            if (target.Hp <= 0)
            {
                target.Active = false;
                target.IsAttackingWall = false;
                target.WallAttackCooldownSeconds = 0d;
            }
            if (AreDamageNumbersEnabled())
            {
                var damageNumberName = $"DamageNumber{_projectilesCreated + 1}";
                var damageNumber = new Node2D { Name = damageNumberName };
                _battlefield.AddChild(damageNumber);
                _activeDamageNumberNames.Add(damageNumberName);
            }

            _towerShotEvents.Add(new GDictionary
            {
                ["source_name"] = sourceNodeName,
                ["target_name"] = targetNodeName,
                ["source_x"] = sourceNode?.GlobalPosition.X ?? 0f,
                ["source_y"] = sourceNode?.GlobalPosition.Y ?? 0f,
                ["target_x"] = targetNode?.GlobalPosition.X ?? 0f,
                ["target_y"] = targetNode?.GlobalPosition.Y ?? 0f,
                ["damage"] = Math.Max(0, previousHp - target.Hp),
                ["target_hp"] = target.Hp,
            });
        }

        projectile.QueueFree();
        return 1;
    }

    private void AdvanceTowerAutoAttack(double deltaSeconds)
    {
        if (deltaSeconds <= 0d)
        {
            return;
        }

        var towerPlacements = GetTowerPlacements();
        if (towerPlacements.Count == 0)
        {
            _lastTowerTargetName = string.Empty;
            _towerAttackCooldownSecondsByNodeName.Clear();
            _lastTowerTargetNameByNodeName.Clear();
            return;
        }

        var activeTowerNames = new System.Collections.Generic.HashSet<string>(
            towerPlacements.Select(tower => tower.NodeName),
            StringComparer.Ordinal);
        foreach (var staleTowerName in _towerAttackCooldownSecondsByNodeName.Keys.Where(name => !activeTowerNames.Contains(name)).ToArray())
        {
            _towerAttackCooldownSecondsByNodeName.Remove(staleTowerName);
        }
        foreach (var staleTowerName in _lastTowerTargetNameByNodeName.Keys.Where(name => !activeTowerNames.Contains(name)).ToArray())
        {
            _lastTowerTargetNameByNodeName.Remove(staleTowerName);
        }

        var reservedTargets = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        foreach (var towerPlacement in towerPlacements)
        {
            var towerNodeName = towerPlacement.NodeName;
            if (!HasNode($"Battlefield/{towerNodeName}"))
            {
                AddMarker(towerNodeName, towerPlacement.Center);
            }
            var cooldown = _towerAttackCooldownSecondsByNodeName.TryGetValue(towerNodeName, out var storedCooldown)
                ? storedCooldown
                : 0d;
            var towerProfile = ResolveTowerCombatProfile(towerNodeName);
            if (cooldown > 0d)
            {
                cooldown = Math.Max(0d, cooldown - deltaSeconds);
            }

            var targetName = FindNearestEnemyInRange(towerNodeName, towerProfile.RangePx, reservedTargets);
            if (string.IsNullOrWhiteSpace(targetName))
            {
                targetName = FindNearestEnemyInRange(towerNodeName, towerProfile.RangePx);
            }
            if (string.IsNullOrWhiteSpace(targetName))
            {
                _lastTowerTargetNameByNodeName.Remove(towerNodeName);
                _towerAttackCooldownSecondsByNodeName[towerNodeName] = cooldown;
                continue;
            }

            _lastTowerTargetName = targetName;
            _lastTowerTargetNameByNodeName[towerNodeName] = targetName;
            reservedTargets.Add(targetName);

            if (cooldown > 0.000001d)
            {
                _towerAttackCooldownSecondsByNodeName[towerNodeName] = cooldown;
                continue;
            }

            var fired = FireProjectile(towerNodeName, targetName, towerProfile.AttackDamage);
            if (fired <= 0)
            {
                _towerAttackCooldownSecondsByNodeName[towerNodeName] = cooldown;
                continue;
            }

            _projectilesCreated += fired;
            _combatExchanges += fired;
            _towerAttackCooldownSecondsByNodeName[towerNodeName] = towerProfile.AttackIntervalSeconds;
        }
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

    private string? FindNearestEnemyInRange(
        string sourceNodeName,
        float rangePx,
        System.Collections.Generic.ISet<string>? excludedTargetNames = null)
    {
        var sourceNode = _battlefield.GetNodeOrNull<Node2D>(sourceNodeName);
        if (sourceNode == null)
        {
            return null;
        }

        var rangeSquared = rangePx * rangePx;
        string? bestTarget = null;
        var bestTargetProgress = float.MinValue;
        var bestTargetHp = int.MaxValue;
        var bestDistanceSquared = float.MaxValue;
        foreach (var pair in _actors)
        {
            var actor = pair.Value;
            if (!actor.Active || actor.TeamId != 2)
            {
                continue;
            }

            if (excludedTargetNames != null && excludedTargetNames.Contains(actor.NodeName))
            {
                continue;
            }

            var actorNode = _battlefield.GetNodeOrNull<Node2D>(actor.NodeName);
            if (actorNode == null)
            {
                continue;
            }

            var distanceSquared = sourceNode.Position.DistanceSquaredTo(actorNode.Position);
            if (distanceSquared > rangeSquared)
            {
                continue;
            }

            if (actor.PathProgress < bestTargetProgress)
            {
                continue;
            }

            if (actor.PathProgress > bestTargetProgress)
            {
                bestTargetProgress = actor.PathProgress;
                bestTargetHp = actor.Hp;
                bestDistanceSquared = distanceSquared;
                bestTarget = actor.NodeName;
                continue;
            }

            if (actor.Hp > bestTargetHp)
            {
                continue;
            }

            if (actor.Hp == bestTargetHp && distanceSquared >= bestDistanceSquared)
            {
                continue;
            }

            bestTargetHp = actor.Hp;
            bestDistanceSquared = distanceSquared;
            bestTarget = actor.NodeName;
        }

        return bestTarget;
    }

    private string ResolvePlacedBuildingNodeName(string selectionId, string slotId)
    {
        return selectionId switch
        {
            "tower_alpha" => _placedBuildingNodeNames.Values.Contains("MgTower", StringComparer.Ordinal)
                ? $"MgTower_{SanitizeSlotId(slotId)}"
                : "MgTower",
            "tower_beta" => _placedBuildingNodeNames.Values.Contains("SniperTower", StringComparer.Ordinal)
                ? $"SniperTower_{SanitizeSlotId(slotId)}"
                : "SniperTower",
            "barracks_alpha" => "Barracks",
            "farm_alpha" => "Residence",
            _ => string.Empty,
        };
    }

    private Vector2 ResolveMarkerPosition(string nodeName, string slotId)
    {
        if (TryResolveSlotCenter(slotId, out var slotCenter))
        {
            return slotCenter;
        }

        if (RuntimeMarkerPositions.TryGetValue(nodeName, out var position))
        {
            return position;
        }

        return Vector2.Zero;
    }

    private static string SanitizeSlotId(string slotId)
    {
        return string.IsNullOrWhiteSpace(slotId)
            ? "Slot"
            : slotId.Replace('/', '_').Replace(':', '_');
    }

    private static bool TryResolveSlotCenter(string slotId, out Vector2 center)
    {
        center = Vector2.Zero;
        if (string.IsNullOrWhiteSpace(slotId))
        {
            return false;
        }

        var parts = slotId.Split('_');
        if (parts.Length < 3)
        {
            return false;
        }

        if (!int.TryParse(parts[^2], out var column) || !int.TryParse(parts[^1], out var row))
        {
            return false;
        }

        float regionOffsetX = 0f;
        if (slotId.StartsWith("LeftOuterFieldSlot_", StringComparison.Ordinal))
        {
            regionOffsetX = 0f;
        }
        else if (slotId.StartsWith("InnerCastleRegionSlot_", StringComparison.Ordinal))
        {
            regionOffsetX = 624f;
        }
        else if (slotId.StartsWith("RightOuterFieldSlot_", StringComparison.Ordinal))
        {
            regionOffsetX = 1008f;
        }
        else
        {
            return false;
        }

        center = new Vector2(regionOffsetX + (column * 48f) + 24f, (row * 48f) + 24f);
        return true;
    }

    private System.Collections.Generic.List<string> GetTowerNodeNames()
    {
        var towerNodeNames = new System.Collections.Generic.List<string>();
        foreach (var towerPlacement in GetTowerPlacements())
        {
            towerNodeNames.Add(towerPlacement.NodeName);
        }

        if (towerNodeNames.Count == 0 && HasNode("Battlefield/MgTower"))
        {
            towerNodeNames.Add("MgTower");
        }

        return towerNodeNames;
    }

    private double ResolveTowerCooldownSeconds()
    {
        if (_towerAttackCooldownSecondsByNodeName.Count == 0)
        {
            return 0d;
        }

        return _towerAttackCooldownSecondsByNodeName.Values.Max();
    }

    private string ResolveSlotIdForTowerNodeName(string towerNodeName)
    {
        foreach (var pair in _placedBuildingNodeNames)
        {
            if (string.Equals(pair.Value, towerNodeName, StringComparison.Ordinal))
            {
                return pair.Key;
            }
        }

        if (string.Equals(towerNodeName, "MgTower", StringComparison.Ordinal))
        {
            foreach (var pair in _placedBuildingSlots)
            {
                if (string.Equals(pair.Value, "tower_alpha", StringComparison.Ordinal))
                {
                    return pair.Key;
                }
            }
        }

        return string.Empty;
    }

    private System.Collections.Generic.List<TowerPlacementRuntime> GetTowerPlacements()
    {
        var placements = new System.Collections.Generic.List<TowerPlacementRuntime>();
        foreach (var pair in _placedBuildingSlots)
        {
            if (!IsTowerSelectionId(pair.Value))
            {
                continue;
            }

            var nodeName = _placedBuildingNodeNames.TryGetValue(pair.Key, out var storedNodeName)
                && !string.IsNullOrWhiteSpace(storedNodeName)
                ? storedNodeName
                : ResolvePlacedBuildingNodeName(pair.Value, pair.Key);
            var center = ResolveMarkerPosition(nodeName, pair.Key);
            placements.Add(new TowerPlacementRuntime(pair.Key, nodeName, center));
        }

        if (placements.Count == 0 && HasNode("Battlefield/MgTower"))
        {
            placements.Add(new TowerPlacementRuntime(string.Empty, "MgTower", ResolveMarkerPosition("MgTower", string.Empty)));
        }
        if (placements.Count == 0 && HasNode("Battlefield/SniperTower"))
        {
            placements.Add(new TowerPlacementRuntime(string.Empty, "SniperTower", ResolveMarkerPosition("SniperTower", string.Empty)));
        }

        return placements;
    }

    private static TowerCombatProfile ResolveTowerCombatProfile(string towerNodeName)
    {
        if (towerNodeName.StartsWith("SniperTower", StringComparison.Ordinal))
        {
            return SniperTowerProfile;
        }

        if (towerNodeName.StartsWith("MgTower", StringComparison.Ordinal))
        {
            return MgTowerProfile;
        }

        return MgTowerProfile;
    }

    private static bool IsTowerSelectionId(string selectionId)
    {
        return string.Equals(selectionId, "tower_alpha", StringComparison.Ordinal)
            || string.Equals(selectionId, "tower_beta", StringComparison.Ordinal);
    }

    public void LoadEnemyRuntimeConfigForTest(string configJson)
    {
        _enemyRuntimeConfigJson = configJson ?? string.Empty;
        _testEnemyRuntimeConfigOverrideActive = !string.IsNullOrWhiteSpace(_enemyRuntimeConfigJson);
        if (!string.IsNullOrWhiteSpace(_enemyRuntimeConfigJson))
        {
            _enemyRuntimeConfigManager.LoadInitialFromJson(_enemyRuntimeConfigJson, "memory://combat-experience-runtime-enemy-config.json");
        }
    }

    private void LoadBattleMapRuntimeConfig()
    {
        var overrideJson = _enemyRuntimeConfigJson;
        _enemyRuntimeConfigJson = string.Empty;
        _battleMapAutoSpawnEnabled = true;
        _battleMapSpawnCadenceSeconds = 8;
        _battleMapConfiguredWaveSize = 2;
        _battleMapWaveSequence.Clear();

        string json;
        if (_testEnemyRuntimeConfigOverrideActive)
        {
            json = overrideJson;
            _enemyRuntimeConfigJson = overrideJson;
        }
        else
        {
            var absolutePath = ProjectSettings.GlobalizePath(BattleMapRuntimeConfigPath);
            if (!File.Exists(absolutePath))
            {
                return;
            }

            json = File.ReadAllText(absolutePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            _enemyRuntimeConfigJson = json;
        }

        _enemyRuntimeConfigManager.LoadInitialFromJson(
            json,
            _testEnemyRuntimeConfigOverrideActive
                ? "memory://combat-experience-runtime-enemy-config.json"
                : BattleMapRuntimeConfigPath);
        _battleMapSpawnCadenceSeconds = Math.Max(1, _enemyRuntimeConfigManager.Snapshot.SpawnCadenceSeconds);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (TryGetPropertyIgnoreCase(root, "battlemap_runtime", out var runtimeSection) && runtimeSection.ValueKind == JsonValueKind.Object)
        {
            if (TryGetBool(runtimeSection, "auto_spawn_enabled", out var autoSpawnEnabled))
            {
                _battleMapAutoSpawnEnabled = autoSpawnEnabled;
            }

            if (TryGetInt(runtimeSection, "wave_size", out var waveSize))
            {
                _battleMapConfiguredWaveSize = Math.Max(1, waveSize);
            }

            if (TryGetPropertyIgnoreCase(runtimeSection, "wave_sequence", out var waveSequence) && waveSequence.ValueKind == JsonValueKind.Array)
            {
                foreach (var waveEntry in waveSequence.EnumerateArray())
                {
                    if (waveEntry.ValueKind == JsonValueKind.Array)
                    {
                        var enemyIds = waveEntry.EnumerateArray()
                            .Where(item => item.ValueKind == JsonValueKind.String)
                            .Select(item => item.GetString() ?? string.Empty)
                            .Where(item => !string.IsNullOrWhiteSpace(item))
                            .Select(item => new BattleMapWaveSpawn(item, SpawnLane.Right))
                            .ToArray();
                        if (enemyIds.Length > 0)
                        {
                            _battleMapWaveSequence.Add(new BattleMapWaveDefinition(enemyIds));
                        }
                        continue;
                    }
                    if (waveEntry.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var spawns = new System.Collections.Generic.List<BattleMapWaveSpawn>();
                    AppendWaveLaneSpawns(waveEntry, "left", SpawnLane.Left, spawns);
                    AppendWaveLaneSpawns(waveEntry, "right", SpawnLane.Right, spawns);
                    if (spawns.Count > 0)
                    {
                        _battleMapWaveSequence.Add(new BattleMapWaveDefinition(spawns.ToArray()));
                    }
                }
            }
        }
    }

    private static void AppendWaveLaneSpawns(
        JsonElement waveEntry,
        string propertyName,
        SpawnLane lane,
        System.Collections.Generic.ICollection<BattleMapWaveSpawn> result)
    {
        if (!TryGetPropertyIgnoreCase(waveEntry, propertyName, out var laneEntries) || laneEntries.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in laneEntries.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var enemyId = entry.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                continue;
            }

            result.Add(new BattleMapWaveSpawn(enemyId, lane));
        }
    }

    private RuntimeEnemyProfile[] ResolveActiveEnemyProfiles()
    {
        if (!string.IsNullOrWhiteSpace(_enemyRuntimeConfigJson))
        {
            var resolved = _enemyConfigResolver.Resolve(_enemyRuntimeConfigManager, _enemyRuntimeConfigJson)
                .Select(ToRuntimeEnemyProfile)
                .ToArray();
            if (resolved.Length > 0)
            {
                return resolved;
            }
        }

        return new[]
        {
            BuildDefaultEnemyProfile(30),
            BuildDefaultEnemyProfile(20),
        };
    }

    private readonly struct WaveSpawnRuntime
    {
        public WaveSpawnRuntime(RuntimeEnemyProfile profile, SpawnLane lane)
        {
            Profile = profile;
            Lane = lane;
        }

        public RuntimeEnemyProfile Profile { get; }
        public SpawnLane Lane { get; }
    }

    private WaveSpawnRuntime[] ResolveCurrentWaveSpawns()
    {
        var resolvedProfiles = ResolveActiveEnemyProfiles();
        if (_battleMapWaveSequence.Count > 0)
        {
            var waveIndex = Math.Clamp(_battleMapWaveCursor, 0, _battleMapWaveSequence.Count - 1);
            var configuredWave = _battleMapWaveSequence[waveIndex];
            if (_battleMapWaveCursor < _battleMapWaveSequence.Count)
            {
                _battleMapWaveCursor += 1;
            }

            if (configuredWave.Spawns.Count == 0)
            {
                return global::System.Array.Empty<WaveSpawnRuntime>();
            }

            if (resolvedProfiles.Length == 0)
            {
                return global::System.Array.Empty<WaveSpawnRuntime>();
            }

            var byId = resolvedProfiles.ToDictionary(
                profile => ResolveProfileId(profile),
                profile => profile,
                StringComparer.OrdinalIgnoreCase);
            var result = new System.Collections.Generic.List<WaveSpawnRuntime>();
            foreach (var spawn in configuredWave.Spawns)
            {
                if (byId.TryGetValue(spawn.EnemyId, out var profile))
                {
                    result.Add(new WaveSpawnRuntime(profile, spawn.Lane));
                }
            }
            return result.ToArray();
        }

        if (resolvedProfiles.Length == 0)
        {
            return new[]
            {
                new WaveSpawnRuntime(BuildDefaultEnemyProfile(30), SpawnLane.Right),
                new WaveSpawnRuntime(BuildDefaultEnemyProfile(20), SpawnLane.Right),
            };
        }

        var fallbackWaveSize = Math.Max(1, _battleMapConfiguredWaveSize);
        var fallbackProfiles = new System.Collections.Generic.List<WaveSpawnRuntime>();
        for (var index = 0; index < fallbackWaveSize; index++)
        {
            fallbackProfiles.Add(new WaveSpawnRuntime(resolvedProfiles[index % resolvedProfiles.Length], SpawnLane.Right));
        }
        return fallbackProfiles.ToArray();
    }

    private static string ResolveProfileId(RuntimeEnemyProfile profile)
    {
        return profile.EnemyId;
    }

    private static RuntimeEnemyProfile ToRuntimeEnemyProfile(EnemyRuntimeStats stats)
    {
        var moveSpeedProgressPerSecond = Math.Max(0.0001f, (float)(stats.Speed / 100m));
        var attackIntervalSeconds = Math.Max(0.1d, stats.AttackIntervalMs / 1000d);
        return new RuntimeEnemyProfile
        {
            EnemyId = stats.EnemyId,
            Health = Math.Max(1, DecimalToInt(stats.Health)),
            Damage = Math.Max(1, DecimalToInt(stats.Damage)),
            MoveSpeedProgressPerSecond = moveSpeedProgressPerSecond,
            AttackRangePx = Math.Max(0f, (float)stats.AttackRange),
            AttackIntervalSeconds = attackIntervalSeconds,
            VisualTier = stats.IsBoss ? "boss" : stats.IsElite ? "elite" : "grunt",
            IsElite = stats.IsElite,
            IsBoss = stats.IsBoss,
        };
    }

    private static RuntimeEnemyProfile BuildDefaultEnemyProfile(int health)
    {
        return new RuntimeEnemyProfile
        {
            EnemyId = "grunt",
            Health = health,
            Damage = WallAttackDamage,
            MoveSpeedProgressPerSecond = EnemyTravelSpeedPerSecond,
            AttackRangePx = 80f,
            AttackIntervalSeconds = WallAttackIntervalSeconds,
            VisualTier = "grunt",
            IsElite = false,
            IsBoss = false,
        };
    }

    private static int DecimalToInt(decimal value)
    {
        return Convert.ToInt32(Math.Round(value, MidpointRounding.AwayFromZero));
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetInt(JsonElement root, string propertyName, out int value)
    {
        value = 0;
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetBool(JsonElement root, string propertyName, out bool value)
    {
        value = false;
        if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        return false;
    }

    private static float ResolveTravelSpeedProgressPerSecond(RuntimeActor actor)
    {
        return actor.IsMovingEnemy
            ? Math.Max(0.0001f, actor.MoveSpeedProgressPerSecond)
            : EnemyTravelSpeedPerSecond;
    }

    private void Publish(string type, string payload)
    {
        var bus = GetNodeOrNull<EventBusAdapter>("/root/EventBus");
        bus?.PublishSimple(type, "combat-experience-runtime", payload);
    }
}

