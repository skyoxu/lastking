using System;
using System.Collections.Generic;

namespace Game.Core.Services.Building;

public sealed class BuildingUpgradeRepairRuntime
{
    private readonly Dictionary<int, int> upgradeCosts = new()
    {
        [1] = 100,
        [2] = 200,
        [3] = 300,
        [4] = 400,
    };

    private int upgradeTicksRemaining;
    private int repairTicksRemaining;
    private int remainingRepairCost;
    private int activeDeducted;
    private int activeConsumed;
    private string activeBuildingId = string.Empty;

    public BuildingUpgradeRepairRuntime(
        int level = 1,
        int maxLevel = 5,
        int maxHp = 100,
        int currentHp = 40,
        int gold = 2000,
        int buildCost = 1000,
        BuildingOperationState state = BuildingOperationState.Idle,
        int upgradeTicksRequired = 2,
        int repairTicksRequired = 2)
    {
        Level = level;
        MaxLevel = maxLevel;
        MaxHp = maxHp;
        CurrentHp = currentHp;
        Gold = gold;
        BuildCost = buildCost;
        State = state;
        UpgradeTicksRequired = Math.Max(1, upgradeTicksRequired);
        RepairTicksRequired = Math.Max(1, repairTicksRequired);
    }

    public int Level { get; private set; }

    public int MaxLevel { get; }

    public int MaxHp { get; }

    public int CurrentHp { get; private set; }

    public int Gold { get; private set; }

    public int BuildCost { get; }

    public BuildingOperationState State { get; private set; }

    public int UpgradeTicksRequired { get; }

    public int RepairTicksRequired { get; }

    public int ElapsedTicks { get; private set; }

    public int TotalGoldDeducted { get; private set; }

    public int RepairCompletedEvents { get; private set; }

    public int TotalDeducted => activeDeducted;

    public int TotalConsumed => activeConsumed;

    public IReadOnlyList<string> Timeline => timeline;

    public IReadOnlyList<string> OperationLog => operationLog;

    private readonly List<string> timeline = new();
    private readonly List<string> operationLog = new();

    public void OverrideUpgradeCost(int level, int cost)
    {
        upgradeCosts[level] = Math.Max(0, cost);
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        CurrentHp = Math.Max(0, CurrentHp - amount);
    }

    public bool TryStartUpgrade()
    {
        return TryStartUpgrade(string.Empty);
    }

    public bool TryStartUpgrade(string buildingId)
    {
        if (State != BuildingOperationState.Idle)
        {
            timeline.Add(buildingId.Length == 0 ? "upgrade:refused.concurrent" : $"upgrade:refused.concurrent:{buildingId}");
            return false;
        }

        if (Level >= MaxLevel)
        {
            timeline.Add(buildingId.Length == 0 ? "upgrade:refused.max-level" : $"upgrade:refused.max-level:{buildingId}");
            return false;
        }

        var cost = upgradeCosts.GetValueOrDefault(Level, 0);
        if (Gold < cost)
        {
            timeline.Add(buildingId.Length == 0 ? "upgrade:refused.insufficient-gold" : $"upgrade:refused.insufficient-gold:{buildingId}");
            return false;
        }

        Gold -= cost;
        State = BuildingOperationState.Upgrading;
        upgradeTicksRemaining = UpgradeTicksRequired;
        ElapsedTicks = 0;
        activeDeducted = cost;
        activeConsumed = 0;
        activeBuildingId = buildingId;
        operationLog.Add("upgrade:start");
        timeline.Add(buildingId.Length == 0 ? "upgrade:start" : $"upgrade:start:{buildingId}");
        return true;
    }

    public bool TryStartRepair()
    {
        return TryStartRepair(string.Empty);
    }

    public bool TryStartRepair(string buildingId)
    {
        if (State != BuildingOperationState.Idle)
        {
            timeline.Add(buildingId.Length == 0 ? "repair:refused.concurrent" : $"repair:refused.concurrent:{buildingId}");
            return false;
        }

        if (CurrentHp >= MaxHp)
        {
            timeline.Add(buildingId.Length == 0 ? "repair:refused.full-hp" : $"repair:refused.full-hp:{buildingId}");
            return false;
        }

        var totalCost = BuildCost / 2;
        if (Gold < totalCost)
        {
            timeline.Add(buildingId.Length == 0 ? "repair:refused.insufficient-gold" : $"repair:refused.insufficient-gold:{buildingId}");
            return false;
        }

        Gold -= totalCost;
        TotalGoldDeducted += totalCost;
        State = BuildingOperationState.Repairing;
        repairTicksRemaining = RepairTicksRequired;
        remainingRepairCost = totalCost;
        ElapsedTicks = 0;
        activeDeducted = totalCost;
        activeConsumed = 0;
        activeBuildingId = buildingId;
        operationLog.Add("repair:start");
        timeline.Add(buildingId.Length == 0 ? "repair:start" : $"repair:start:{buildingId}");
        return true;
    }

    public void AdvanceRepairTick()
    {
        if (State != BuildingOperationState.Repairing)
        {
            return;
        }

        AdvanceTick();
    }

    public void AdvanceTick()
    {
        if (State == BuildingOperationState.Idle)
        {
            return;
        }

        ElapsedTicks++;
        if (State == BuildingOperationState.Upgrading)
        {
            upgradeTicksRemaining--;
            activeConsumed = Math.Min(activeDeducted, (int)Math.Ceiling(activeDeducted * (double)ElapsedTicks / UpgradeTicksRequired));
            if (activeBuildingId.Length == 0)
            {
                timeline.Add("upgrade:progress");
            }
            else
            {
                timeline.Add($"upgrade:progress:{activeBuildingId}:{ElapsedTicks}/{UpgradeTicksRequired}");
            }

            if (upgradeTicksRemaining <= 0)
            {
                Level = Math.Min(Level + 1, MaxLevel);
                CurrentHp = MaxHp;
                if (activeBuildingId.Length == 0)
                {
                    timeline.Add("upgrade:completed");
                }
                else
                {
                    timeline.Add($"upgrade:completed:{activeBuildingId}");
                }

                ResetToIdle();
            }

            return;
        }

        repairTicksRemaining--;
        var missingHp = MaxHp - CurrentHp;
        if (missingHp <= 0 || remainingRepairCost <= 0)
        {
            CompleteRepair();
            return;
        }

        var hpStep = missingHp <= 65 ? missingHp : (int)Math.Ceiling(missingHp / 2.0);
        var hpGain = Math.Min(hpStep, missingHp);
        CurrentHp += hpGain;

        var totalRepairCost = BuildCost / 2;
        var rawCost = (int)Math.Ceiling(totalRepairCost * (double)hpGain / MaxHp);
        var spentThisStep = Math.Min(rawCost, remainingRepairCost);
        if (CurrentHp >= MaxHp || repairTicksRemaining <= 0)
        {
            spentThisStep = remainingRepairCost;
        }

        remainingRepairCost = Math.Max(0, remainingRepairCost - spentThisStep);
        activeConsumed = activeDeducted - remainingRepairCost;
        if (activeBuildingId.Length == 0)
        {
            timeline.Add("repair:progress");
        }
        else
        {
            timeline.Add($"repair:progress:{activeBuildingId}:{ElapsedTicks}/{RepairTicksRequired}");
        }

        if (CurrentHp >= MaxHp || repairTicksRemaining <= 0 || remainingRepairCost <= 0)
        {
            CompleteRepair();
        }
    }

    public int CancelActiveOperation()
    {
        return StopWithRefund();
    }

    public int InterruptActiveOperation()
    {
        return StopWithRefund();
    }

    private int StopWithRefund()
    {
        if (State == BuildingOperationState.Idle)
        {
            return 0;
        }

        var refund = activeDeducted - activeConsumed;
        refund = Math.Max(0, Math.Min(refund, activeDeducted));
        Gold += refund;
        ResetToIdle();
        return refund;
    }

    private void CompleteRepair()
    {
        CurrentHp = MaxHp;
        RepairCompletedEvents++;
        operationLog.Add("repair:completed");
        if (activeBuildingId.Length == 0)
        {
            timeline.Add("repair:completed");
        }
        else
        {
            timeline.Add($"repair:completed:{activeBuildingId}");
        }

        activeConsumed = activeDeducted;
        remainingRepairCost = 0;
        ResetToIdle();
    }

    private void ResetToIdle()
    {
        State = BuildingOperationState.Idle;
        activeBuildingId = string.Empty;
        ElapsedTicks = 0;
        upgradeTicksRemaining = 0;
        repairTicksRemaining = 0;
    }
}
