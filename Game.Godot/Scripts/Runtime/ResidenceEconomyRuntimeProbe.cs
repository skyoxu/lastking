using System.Collections.Generic;
using System.Linq;
using Game.Core.Domain.Building;
using Game.Core.Services.Building;
using Game.Core.State.Building;
using Game.Core.Services;
using Godot;

namespace Game.Godot.Scripts.Runtime;

public partial class ResidenceEconomyRuntimeProbe : Node
{
    private readonly List<int> _taxEventSeconds = new();
    private ResourceManager _resourceManager = new();
    private Timer? _taxTimer;
    private bool _residenceBuilt;
    private int _residenceBuiltCount;
    private int _tickSequenceSeconds;
    private double _fractionalSeconds;
    private BuildingPlacementState _placementState = new(width: 32, height: 32, resources: 1000);
    private readonly List<int> _residenceLevels = new();

    [Export]
    public int TaxPerTick { get; set; } = 7;

    [Export]
    public int ResidenceCount { get; set; } = 1;

    [Export]
    public int PopulationCapDelta { get; set; } = 3;

    [Export]
    public int TaxPerTickLevel2 { get; set; } = 11;

    [Export]
    public string NegativeGoldPolicy { get; set; } = ResidenceTaxRuntimePolicy.NegativeGoldPolicyAllowDebt;

    public int Gold => _resourceManager.Gold;

    public int PopulationCap => _resourceManager.PopulationCap;

    public bool IsTaxScheduleRunning => _residenceBuilt;

    public Timer? TaxTimer => _taxTimer;

    public override void _Ready()
    {
        EnsureTimer();
    }

    public void EnsureReadyForTest()
    {
        EnsureTimer();
    }

    public void SetBaselineForTest(int gold, int iron, int populationCap)
    {
        var snapshotJson = $"{{\"gold\":{gold},\"iron\":{iron},\"populationCap\":{populationCap}}}";
        _ = _resourceManager.TryImportSnapshot(snapshotJson);
        _residenceBuilt = false;
        _residenceBuiltCount = 0;
        _tickSequenceSeconds = 0;
        _fractionalSeconds = 0.0;
        _taxEventSeconds.Clear();
        _residenceLevels.Clear();
        _placementState = new BuildingPlacementState(width: 32, height: 32, resources: 1000);
    }

    public bool TryPlaceResidenceAt(int x, int y, bool blockTargetCell = false, int level = 1)
    {
        var origin = new GridPoint(x, y);
        if (blockTargetCell)
        {
            var preview = new BuildingPlacementService().Preview(BuildingTypeIds.Residence, origin);
            if (preview.Cells.Count > 0)
            {
                _placementState.Grid.Blocked.Add(preview.Cells[^1]);
            }
        }

        var placementService = new BuildingSubtypePlacementService(
            resourceManager: _resourceManager,
            residencePopulationCapDelta: PopulationCapDelta);
        var outcome = placementService.TryPlace(BuildingTypeIds.Residence, origin, _placementState);
        if (!outcome.IsAccepted)
        {
            return false;
        }

        _residenceBuilt = true;
        _residenceBuiltCount += 1;
        _residenceLevels.Add(level);
        return true;
    }

    public void ApplyPlacementResult(bool accepted)
    {
        if (!accepted)
        {
            return;
        }

        _ = TryPlaceResidenceAt(2 + _residenceBuiltCount, 2 + _residenceBuiltCount);
    }

    public void PlaceResidenceWithLevelForTest(int level)
    {
        _ = TryPlaceResidenceAt(2 + _residenceBuiltCount, 2 + _residenceBuiltCount, false, level);
    }

    public void AdvanceSeconds(int seconds)
    {
        AdvanceSimulation(seconds, 1.0);
    }

    public void AdvanceSimulation(double seconds, double frameStep)
    {
        if (seconds <= 0.0 || frameStep <= 0.0)
        {
            return;
        }

        var remaining = seconds;
        while (remaining > 0.0)
        {
            var step = Mathf.Min((float)frameStep, (float)remaining);
            remaining -= step;
            _fractionalSeconds += step;
            while (_fractionalSeconds >= 1.0)
            {
                _fractionalSeconds -= 1.0;
                _tickSequenceSeconds += 1;
                if (_tickSequenceSeconds % ResidenceTaxRuntimePolicy.ResidenceTaxCadenceSeconds == 0)
                {
                    OnTaxTimerTimeout();
                }
            }
        }
    }

    public void TriggerTimeoutForTest()
    {
        _tickSequenceSeconds += ResidenceTaxRuntimePolicy.ResidenceTaxCadenceSeconds;
        OnTaxTimerTimeout();
    }

    private void EnsureTimer()
    {
        if (_taxTimer is not null)
        {
            return;
        }

        _taxTimer = new Timer
        {
            Name = "ResidenceTaxTimer",
            WaitTime = ResidenceTaxRuntimePolicy.ResidenceTaxCadenceSeconds,
            OneShot = false,
            Autostart = false,
        };

        _taxTimer.Timeout += OnTaxTimerTimeout;
        AddChild(_taxTimer);
    }

    private void OnTaxTimerTimeout()
    {
        if (!_residenceBuilt)
        {
            return;
        }

        var totalTaxPerTick = ResolveTotalTaxPerTick();
        if (totalTaxPerTick <= 0)
        {
            return;
        }

        var trace = ResidenceTaxRuntimePolicy.SettleTaxTick(
            tickSequence: _tickSequenceSeconds,
            currentGold: _resourceManager.Gold,
            residenceCount: 1,
            taxPerResidence: totalTaxPerTick,
            negativeGoldPolicy: NegativeGoldPolicy);

        if (trace.Reason != "tax_applied")
        {
            return;
        }

        _ = _resourceManager.TryAdd(trace.GoldDelta, 0, 0, "residence_tax_tick");
        _taxEventSeconds.Add(_tickSequenceSeconds);
    }

    private int ResolveTotalTaxPerTick()
    {
        if (_residenceLevels.Count == 0)
        {
            return 0;
        }

        return _residenceLevels.Sum(ResolveTaxPerTickForLevel);
    }

    private int ResolveTaxPerTickForLevel(int level)
    {
        if (level == 2)
        {
            return TaxPerTickLevel2;
        }

        return TaxPerTick;
    }
}
