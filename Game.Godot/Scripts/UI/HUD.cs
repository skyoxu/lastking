using System;
using System.Text.Json;
using Game.Core.Contracts;
using Game.Godot.Adapters;
using Godot;

namespace Game.Godot.Scripts.UI;

public partial class HUD : Control
{
    private static readonly JsonDocumentOptions EventJsonOptions = new() { MaxDepth = 16 };
    private const double DefaultDayDurationSeconds = 240d;
    private const double DefaultNightDurationSeconds = 120d;

    private EventBusAdapter? _bus;
    private Label _day = default!;
    private Label _cycleRemaining = default!;
    private Label _health = default!;
    private Button _pauseButton = default!;
    private Button _oneXButton = default!;
    private Button _twoXButton = default!;

    private int _currentDay = 1;
    private double _phaseDurationSeconds = DefaultDayDurationSeconds;
    private double _phaseElapsedSeconds;
    private bool _phaseCountdownEnabled = true;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;
        _day = GetNode<Label>("TopBar/HBox/DayLabel");
        _cycleRemaining = GetNode<Label>("TopBar/HBox/CycleRemainingLabel");
        _health = GetNode<Label>("TopBar/HBox/HealthLabel");
        _pauseButton = GetNode<Button>("TopBar/HBox/SpeedControls/PauseButton");
        _oneXButton = GetNode<Button>("TopBar/HBox/SpeedControls/OneXButton");
        _twoXButton = GetNode<Button>("TopBar/HBox/SpeedControls/TwoXButton");
        RenderDay();
        RenderCycleRemaining();
        _health.Text = "HP: 0";
        _pauseButton.Pressed += OnPausePressed;
        _oneXButton.Pressed += OnOneXPressed;
        _twoXButton.Pressed += OnTwoXPressed;

        _bus = GetNodeOrNull<EventBusAdapter>("/root/EventBus");
        if (_bus != null)
        {
            _bus.Connect(EventBusAdapter.SignalName.DomainEventEmitted, new Callable(this, nameof(OnDomainEventEmitted)));
        }
    }

    public override void _Process(double delta)
    {
        if (!_phaseCountdownEnabled || delta <= 0d)
        {
            return;
        }

        _phaseElapsedSeconds = Math.Max(0d, _phaseElapsedSeconds + delta);
        RenderCycleRemaining();
    }

    public override void _ExitTree()
    {
        if (_bus != null)
        {
            var callable = new Callable(this, nameof(OnDomainEventEmitted));
            if (_bus.IsConnected(EventBusAdapter.SignalName.DomainEventEmitted, callable))
            {
                _bus.Disconnect(EventBusAdapter.SignalName.DomainEventEmitted, callable);
            }
        }
    }

    private void OnDomainEventEmitted(
        string type,
        string source,
        string dataJson,
        string id,
        string specVersion,
        string dataContentType,
        string timestampIso)
    {
        if (source.Length == 0 || dataJson.Length > 2048)
        {
            return;
        }

        if (type == EventTypes.LastkingDayStarted || type == EventTypes.LastkingNightStarted)
        {
            try
            {
                using var doc = JsonDocument.Parse(dataJson, EventJsonOptions);
                var day = ReadInt(doc.RootElement, "day", "Day", "day_number", "DayNumber");
                if (day.HasValue)
                {
                    _currentDay = Math.Clamp(day.Value, 1, 15);
                    RenderDay();
                }

                _phaseDurationSeconds = type == EventTypes.LastkingNightStarted
                    ? DefaultNightDurationSeconds
                    : DefaultDayDurationSeconds;
                _phaseElapsedSeconds = 0d;
                _phaseCountdownEnabled = true;
                RenderCycleRemaining();
            }
            catch
            {
            }

            return;
        }

        if (type != EventTypes.LastkingCastleHpChanged &&
            type != EventTypes.HealthUpdated &&
            type != "player.health.changed")
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(dataJson, EventJsonOptions);
            var hp = ReadInt(doc.RootElement, "current_hp", "CurrentHp", "value", "health");
            if (hp.HasValue)
            {
                _health.Text = $"HP: {hp.Value}";
            }
        }
        catch
        {
        }
    }

    private void OnPausePressed()
    {
        GetNodeOrNull<Node>("/root/GameManager")?.Call("SetPause");
    }

    private void OnOneXPressed()
    {
        GetNodeOrNull<Node>("/root/GameManager")?.Call("SetOneX");
    }

    private void OnTwoXPressed()
    {
        GetNodeOrNull<Node>("/root/GameManager")?.Call("SetTwoX");
    }

    private static int? ReadInt(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!element.TryGetProperty(key, out var valueElement))
            {
                continue;
            }

            if (valueElement.ValueKind == JsonValueKind.Number && valueElement.TryGetInt32(out var numberValue))
            {
                return numberValue;
            }

            if (valueElement.ValueKind == JsonValueKind.String && int.TryParse(valueElement.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private void RenderDay()
    {
        _day.Text = $"Day: {_currentDay}";
    }

    private void RenderCycleRemaining()
    {
        var remaining = Math.Max(0d, _phaseDurationSeconds - _phaseElapsedSeconds);
        _cycleRemaining.Text = $"Cycle Remaining: {remaining:0.0}s";
    }

    public void SetDay(int day)
    {
        _currentDay = Math.Clamp(day, 1, 15);
        RenderDay();
    }

    public void SetCycleRemainingSeconds(double seconds)
    {
        _phaseCountdownEnabled = false;
        _phaseElapsedSeconds = 0d;
        _phaseDurationSeconds = Math.Max(0d, seconds);
        RenderCycleRemaining();
    }

    public void SetHealth(int hp)
    {
        _health.Text = $"HP: {hp}";
    }
}
