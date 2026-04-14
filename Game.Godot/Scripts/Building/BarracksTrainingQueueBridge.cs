using System;
using System.Collections.Generic;
using Game.Core.Services;
using Godot;
using Godot.Collections;
using GArray = Godot.Collections.Array;
using GDictionary = Godot.Collections.Dictionary;

namespace Game.Godot.Scripts.Building;

public partial class BarracksTrainingQueueBridge : Node
{
    private const string DefaultUnitType = "spearman";

    private static readonly IReadOnlyDictionary<string, double> BaselineStats = new System.Collections.Generic.Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["attack"] = 20.0d,
        ["damage"] = 20.0d,
        ["attack_speed"] = 1.0d,
        ["production_speed"] = 1.0d,
        ["range"] = 4.0d,
        ["hp"] = 100.0d,
        ["cost"] = 100.0d,
    };

    [Signal]
    public delegate void QueueEnqueuedEventHandler(int queueLength, int gold, int iron);

    [Signal]
    public delegate void QueueCancelledEventHandler(int queueLength, int gold, int iron);

    [Signal]
    public delegate void QueueCompletedEventHandler(string unitType, int queueLength, int gold, int iron);

    private ResourceManager resources = new(eventBus: null, runId: "barracks-ut", dayNumber: 1);
    private BarracksTrainingQueueRuntime runtime = new(capacity: 3);
    private System.Collections.Generic.Dictionary<string, double> statMultipliers = CreateDefaultStatMultipliers();
    private long deterministicSeed;

    public void ResetRuntime(int gold = ResourceManager.InitialGold, int iron = ResourceManager.InitialIron, int capacity = 3)
    {
        resources = new ResourceManager(eventBus: null, runId: "barracks-ut", dayNumber: 1);
        runtime = new BarracksTrainingQueueRuntime(capacity);
        _ = resources.TryImportSnapshot($"{{\"gold\":{gold},\"iron\":{iron},\"populationCap\":50}}");
        statMultipliers = CreateDefaultStatMultipliers();
        deterministicSeed = 0L;
    }

    public int GetQueueLength() => runtime.Count;

    public int GetGold() => resources.Gold;

    public int GetIron() => resources.Iron;

    public GArray GetDiagnostics()
    {
        var result = new GArray();
        foreach (var item in runtime.Diagnostics)
        {
            result.Add(new GDictionary
            {
                ["operation_index"] = item.OperationIndex,
                ["operation"] = item.Operation,
                ["transition"] = item.Transition,
                ["queue_snapshot"] = ToArray(item.QueueSnapshot),
                ["gold"] = item.Gold,
                ["iron"] = item.Iron,
            });
        }

        return result;
    }

    public GDictionary EnqueueUpfront(string unitType, int durationTicks, int goldCost, int ironCost)
    {
        var definition = new BarracksTrainingDefinition(
            unitType,
            durationTicks,
            new[]
            {
                new BarracksCostStage(0, goldCost, ironCost),
            });
        var result = runtime.TryEnqueue(definition, resources);
        if (result.Accepted)
        {
            EmitSignal(SignalName.QueueEnqueued, result.QueueLength, result.GoldAfter, result.IronAfter);
        }

        return new GDictionary
        {
            ["accepted"] = result.Accepted,
            ["reason"] = result.Reason,
            ["job_id"] = result.JobId,
            ["queue_length"] = result.QueueLength,
            ["gold"] = result.GoldAfter,
            ["iron"] = result.IronAfter,
            ["deducted_gold"] = result.DeductedGold,
            ["deducted_iron"] = result.DeductedIron,
        };
    }

    public GDictionary EnqueueStaged(string unitType, int durationTicks, int triggerTick, int goldCost, int ironCost)
    {
        var definition = new BarracksTrainingDefinition(
            unitType,
            durationTicks,
            new[]
            {
                new BarracksCostStage(triggerTick, goldCost, ironCost),
            });
        var result = runtime.TryEnqueue(definition, resources);
        if (result.Accepted)
        {
            EmitSignal(SignalName.QueueEnqueued, result.QueueLength, result.GoldAfter, result.IronAfter);
        }

        return new GDictionary
        {
            ["accepted"] = result.Accepted,
            ["reason"] = result.Reason,
            ["job_id"] = result.JobId,
            ["queue_length"] = result.QueueLength,
            ["gold"] = result.GoldAfter,
            ["iron"] = result.IronAfter,
            ["deducted_gold"] = result.DeductedGold,
            ["deducted_iron"] = result.DeductedIron,
        };
    }

    public GDictionary CancelAt(int index)
    {
        var result = runtime.TryCancelAt(index, resources);
        if (result.Accepted)
        {
            EmitSignal(SignalName.QueueCancelled, result.QueueLength, result.GoldAfter, result.IronAfter);
        }

        return new GDictionary
        {
            ["accepted"] = result.Accepted,
            ["reason"] = result.Reason,
            ["queue_length"] = result.QueueLength,
            ["gold"] = result.GoldAfter,
            ["iron"] = result.IronAfter,
            ["refunded_gold"] = result.RefundedGold,
            ["refunded_iron"] = result.RefundedIron,
        };
    }

    public GDictionary Tick(int ticks)
    {
        if (ticks <= 0)
        {
            var noop = runtime.Advance(ticks, resources);
            return new GDictionary
            {
                ["ticks_applied"] = noop.TicksApplied,
                ["queue_length"] = noop.QueueLength,
                ["gold"] = noop.GoldAfter,
                ["iron"] = noop.IronAfter,
                ["completed_units"] = ToArray(noop.CompletedUnits),
            };
        }

        var applied = 0;
        var completed = new List<string>();
        for (var index = 0; index < ticks; index++)
        {
            var step = runtime.Advance(1, resources);
            applied += step.TicksApplied;
            foreach (var unitType in step.CompletedUnits)
            {
                completed.Add(unitType);
                EmitSignal(SignalName.QueueCompleted, unitType, step.QueueLength, step.GoldAfter, step.IronAfter);
            }

            if (step.TicksApplied == 0)
            {
                break;
            }
        }

        return new GDictionary
        {
            ["ticks_applied"] = applied,
            ["queue_length"] = runtime.Count,
            ["gold"] = resources.Gold,
            ["iron"] = resources.Iron,
            ["completed_units"] = ToArray(completed),
        };
    }

    public GDictionary SetTechDeterministicSeedForTest(long seed)
    {
        deterministicSeed = seed;
        return new GDictionary
        {
            ["ok"] = true,
            ["seed"] = seed,
        };
    }

    public GDictionary TryApplyTechModifierForTest(string techId, string unitType, string stat, double multiplierDelta)
    {
        if (!IsSupportedUnit(unitType))
        {
            return Reject("unit_not_supported");
        }

        if (!TryNormalizeStatKey(stat, out var normalizedStat))
        {
            return Reject("stat_not_supported");
        }

        if (multiplierDelta <= 0.0d || multiplierDelta > 2.0d)
        {
            return Reject("modifier_out_of_range");
        }

        statMultipliers[normalizedStat] = multiplierDelta;

        return new GDictionary
        {
            ["accepted"] = true,
            ["tech_id"] = techId ?? string.Empty,
            ["unit_type"] = unitType ?? string.Empty,
            ["stat"] = normalizedStat,
            ["multiplier"] = multiplierDelta,
        };
    }

    public GDictionary GetTrainingMultipliersForTest(string unitType)
    {
        if (!IsSupportedUnit(unitType))
        {
            return new GDictionary();
        }

        return BuildTrainingMultipliers();
    }

    public GDictionary PreviewTrainedUnitStatsForTest(string unitType)
    {
        if (!IsSupportedUnit(unitType))
        {
            return new GDictionary();
        }

        var attackMultiplier = GetMultiplier("attack");
        var damageMultiplier = GetMultiplier("damage");
        var attackSpeedMultiplier = GetMultiplier("attack_speed");
        var productionSpeedMultiplier = GetMultiplier("production_speed");
        var rangeMultiplier = GetMultiplier("range");
        var hpMultiplier = GetMultiplier("hp");
        var costMultiplier = GetMultiplier("cost");

        var attack = Scale("attack", attackMultiplier);
        var damage = Scale("damage", damageMultiplier);
        var hp = Scale("hp", hpMultiplier);
        var attackSpeed = Scale("attack_speed", attackSpeedMultiplier);
        var productionSpeed = Scale("production_speed", productionSpeedMultiplier);
        var range = Scale("range", rangeMultiplier);
        var cost = Scale("cost", costMultiplier);

        return new GDictionary
        {
            ["attack"] = attack,
            ["damage"] = damage,
            ["attack_speed"] = attackSpeed,
            ["attackspeed"] = attackSpeed,
            ["atk_speed"] = attackSpeed,
            ["production_speed"] = productionSpeed,
            ["training_speed"] = productionSpeed,
            ["train_speed"] = productionSpeed,
            ["range"] = range,
            ["hp"] = hp,
            ["hit_points"] = hp,
            ["health"] = hp,
            ["cost"] = cost,
            ["gold_cost"] = cost,
            ["training_cost"] = cost,
        };
    }

    public GDictionary GetBarracksTechSnapshotForTest(string unitType)
    {
        if (!IsSupportedUnit(unitType))
        {
            return new GDictionary();
        }

        var snapshot = BuildTrainingMultipliers();
        snapshot["deterministic_seed"] = deterministicSeed;
        return snapshot;
    }

    private static System.Collections.Generic.Dictionary<string, double> CreateDefaultStatMultipliers()
    {
        return new System.Collections.Generic.Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["attack"] = 1.0d,
            ["damage"] = 1.0d,
            ["attack_speed"] = 1.0d,
            ["production_speed"] = 1.0d,
            ["range"] = 1.0d,
            ["hp"] = 1.0d,
            ["cost"] = 1.0d,
        };
    }

    private static bool IsSupportedUnit(string unitType)
    {
        return string.Equals(unitType?.Trim(), DefaultUnitType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryNormalizeStatKey(string stat, out string normalizedStat)
    {
        var key = stat?.Trim().ToLowerInvariant();
        normalizedStat = key ?? string.Empty;

        return key switch
        {
            "attack" => true,
            "damage" => true,
            "attack_speed" => true,
            "attackspeed" => SetNormalized("attack_speed", out normalizedStat),
            "atk_speed" => SetNormalized("attack_speed", out normalizedStat),
            "production_speed" => true,
            "training_speed" => SetNormalized("production_speed", out normalizedStat),
            "train_speed" => SetNormalized("production_speed", out normalizedStat),
            "range" => true,
            "hp" => true,
            "hit_points" => SetNormalized("hp", out normalizedStat),
            "health" => SetNormalized("hp", out normalizedStat),
            "cost" => true,
            "gold_cost" => SetNormalized("cost", out normalizedStat),
            "training_cost" => SetNormalized("cost", out normalizedStat),
            _ => false,
        };
    }

    private static bool SetNormalized(string value, out string normalizedStat)
    {
        normalizedStat = value;
        return true;
    }

    private static GDictionary Reject(string reason)
    {
        return new GDictionary
        {
            ["accepted"] = false,
            ["reason"] = reason,
        };
    }

    private double GetMultiplier(string stat)
    {
        return statMultipliers.TryGetValue(stat, out var multiplier) ? multiplier : 1.0d;
    }

    private static double Scale(string stat, double multiplier)
    {
        return BaselineStats.TryGetValue(stat, out var baseline)
            ? baseline * multiplier
            : multiplier;
    }

    private GDictionary BuildTrainingMultipliers()
    {
        var attackMultiplier = GetMultiplier("attack");
        var damageMultiplier = GetMultiplier("damage");
        var attackSpeedMultiplier = GetMultiplier("attack_speed");
        var productionSpeedMultiplier = GetMultiplier("production_speed");
        var rangeMultiplier = GetMultiplier("range");
        var hpMultiplier = GetMultiplier("hp");
        var costMultiplier = GetMultiplier("cost");

        return new GDictionary
        {
            ["attack_multiplier"] = attackMultiplier,
            ["attack"] = attackMultiplier,
            ["damage_multiplier"] = damageMultiplier,
            ["damage"] = damageMultiplier,
            ["attack_speed_multiplier"] = attackSpeedMultiplier,
            ["attack_speed"] = attackSpeedMultiplier,
            ["attackspeed_multiplier"] = attackSpeedMultiplier,
            ["attackspeed"] = attackSpeedMultiplier,
            ["production_speed_multiplier"] = productionSpeedMultiplier,
            ["production_speed"] = productionSpeedMultiplier,
            ["training_speed_multiplier"] = productionSpeedMultiplier,
            ["training_speed"] = productionSpeedMultiplier,
            ["range_multiplier"] = rangeMultiplier,
            ["range"] = rangeMultiplier,
            ["hp_multiplier"] = hpMultiplier,
            ["hp"] = hpMultiplier,
            ["health_multiplier"] = hpMultiplier,
            ["health"] = hpMultiplier,
            ["cost_multiplier"] = costMultiplier,
            ["cost"] = costMultiplier,
            ["gold_cost_multiplier"] = costMultiplier,
            ["gold_cost"] = costMultiplier,
            ["training_cost_multiplier"] = costMultiplier,
            ["training_cost"] = costMultiplier,
        };
    }

    private static GArray ToArray(IEnumerable<string> items)
    {
        var array = new GArray();
        foreach (var item in items)
        {
            array.Add(item);
        }

        return array;
    }
}
