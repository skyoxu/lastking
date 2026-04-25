using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
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
    private Label _feedbackLabel = default!;
    private Control _feedbackLayer = default!;
    private PanelContainer _errorDialog = default!;
    private Label _errorMessageLabel = default!;
    private Button _dismissButton = default!;
    private PanelContainer _configAuditPanel = default!;
    private Label _configAuditSummaryLabel = default!;
    private Button _configAuditRefreshButton = default!;
    private PanelContainer _migrationStatusDialog = default!;
    private Label _migrationStatusLabel = default!;
    private Button _migrationRetryButton = default!;
    private PanelContainer _reportMetadataPanel = default!;
    private Label _reportMetadataLabel = default!;
    private Button _pauseButton = default!;
    private Button _oneXButton = default!;
    private Button _twoXButton = default!;
    private string _activeFeedbackCode = string.Empty;
    private string _activeFeedbackMessageKey = string.Empty;
    private bool _hasPendingErrorDialog;
    private float _feedbackHideAtMs;
    private const float DefaultFeedbackTimeoutSeconds = 1.5f;
    private static readonly HashSet<string> InvalidPlacementCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "invalid_target",
        "invalid_input",
        "tile_occupied",
        "blocked_tile",
        "invalid_terrain",
        "build_invalid_tile",
    };

    private static readonly HashSet<string> BlockedActionCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "run_continue_blocked",
        "insufficient_resources",
        "cooldown_active",
        "chapter_locked",
    };

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
        _feedbackLayer = GetNode<Control>("FeedbackLayer");
        _feedbackLabel = GetNode<Label>("FeedbackLayer/FeedbackLabel");
        _errorDialog = GetNode<PanelContainer>("FeedbackLayer/ErrorDialog");
        _errorMessageLabel = GetNode<Label>("FeedbackLayer/ErrorDialog/VBox/ErrorMessageLabel");
        _dismissButton = GetNode<Button>("FeedbackLayer/ErrorDialog/VBox/DismissButton");
        _configAuditPanel = GetNode<PanelContainer>("FeedbackLayer/ConfigAuditPanel");
        _configAuditSummaryLabel = GetNode<Label>("FeedbackLayer/ConfigAuditPanel/VBox/AuditSummaryLabel");
        _configAuditRefreshButton = GetNode<Button>("FeedbackLayer/ConfigAuditPanel/VBox/RefreshButton");
        _migrationStatusDialog = GetNode<PanelContainer>("FeedbackLayer/MigrationStatusDialog");
        _migrationStatusLabel = GetNode<Label>("FeedbackLayer/MigrationStatusDialog/VBox/MigrationStatusLabel");
        _migrationRetryButton = GetNode<Button>("FeedbackLayer/MigrationStatusDialog/VBox/RetryButton");
        _reportMetadataPanel = GetNode<PanelContainer>("FeedbackLayer/ReportMetadataPanel");
        _reportMetadataLabel = GetNode<Label>("FeedbackLayer/ReportMetadataPanel/VBox/ReportMetadataLabel");
        _pauseButton = GetNode<Button>("TopBar/HBox/SpeedControls/PauseButton");
        _oneXButton = GetNode<Button>("TopBar/HBox/SpeedControls/OneXButton");
        _twoXButton = GetNode<Button>("TopBar/HBox/SpeedControls/TwoXButton");
        RenderDay();
        RenderCycleRemaining();
        _health.Text = "HP: 0";
        _pauseButton.Pressed += OnPausePressed;
        _oneXButton.Pressed += OnOneXPressed;
        _twoXButton.Pressed += OnTwoXPressed;
        _dismissButton.Pressed += OnDismissFeedbackPressed;
        _feedbackLabel.Visible = false;
        _feedbackLabel.Text = string.Empty;
        _errorDialog.Visible = false;
        _errorMessageLabel.Text = string.Empty;
        _configAuditPanel.Visible = true;
        _migrationStatusDialog.Visible = true;
        _reportMetadataPanel.Visible = true;
        _configAuditSummaryLabel.Text = "Config: n/a | Schema: n/a | Fallback: n/a";
        _migrationStatusLabel.Text = "Migration: n/a";
        _reportMetadataLabel.Text = "Metadata: n/a";
        _activeFeedbackCode = string.Empty;
        _activeFeedbackMessageKey = string.Empty;
        _hasPendingErrorDialog = false;
        _feedbackHideAtMs = 0f;

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
            UpdateFeedbackVisibility();
            return;
        }

        _phaseElapsedSeconds = Math.Max(0d, _phaseElapsedSeconds + delta);
        RenderCycleRemaining();
        UpdateFeedbackVisibility();
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
            type != "player.health.changed" &&
            type != EventTypes.LastkingRewardOffered &&
            type != EventTypes.RunStateTransitioned &&
            type != EventTypes.LastkingUiFeedbackRaised &&
            type != EventTypes.SaveMigrationFailed &&
            type != EventTypes.SaveWriteFailed)
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

            if (type == EventTypes.LastkingRewardOffered)
            {
                HandleRewardOfferedEvent(doc.RootElement);
            }

            if (type == EventTypes.RunStateTransitioned)
            {
                HandleRunStateTransitionedEvent(doc.RootElement);
            }

            if (type == EventTypes.LastkingUiFeedbackRaised ||
                type == EventTypes.SaveMigrationFailed ||
                type == EventTypes.SaveWriteFailed)
            {
                HandleUiFeedbackEvent(type, doc.RootElement);
            }
        }
        catch
        {
        }
    }

    private void HandleUiFeedbackEvent(string type, JsonElement payload)
    {
        var code = ReadString(payload, "Code", "code", "reason_code") ?? string.Empty;
        var messageKey = ReadString(payload, "MessageKey", "message_key") ?? string.Empty;
        var details = ReadString(payload, "Details", "details") ?? string.Empty;

        var hasSaveFailure = type == EventTypes.SaveMigrationFailed || type == EventTypes.SaveWriteFailed;
        var isErrorDialog = hasSaveFailure || IsMigrationOrLoadFailure(code, messageKey);
        if (isErrorDialog)
        {
            ShowPersistentErrorDialog(code, messageKey, details);
            return;
        }

        if (IsBlockedAction(code, messageKey))
        {
            ShowTemporaryFeedback(messageKey, details, code, priority: 2);
            return;
        }

        if (IsInvalidPlacement(code, messageKey))
        {
            ShowTemporaryFeedback(messageKey, details, code, priority: 1);
            return;
        }

        if (IsRuntimeOutcomeMessage(messageKey, code))
        {
            ShowTemporaryFeedback(messageKey, details, code, priority: 2);
        }
    }

    private void HandleRewardOfferedEvent(JsonElement payload)
    {
        var optionA = ReadString(payload, "option_a", "OptionA");
        var optionB = ReadString(payload, "option_b", "OptionB");
        var optionC = ReadString(payload, "option_c", "OptionC");
        var details = string.Join(", ", new[] { optionA, optionB, optionC }.Where(value => !string.IsNullOrWhiteSpace(value)));
        ShowTemporaryFeedback("ui.reward.offer.presented", details, code: "reward_offered", priority: 1);
    }

    private void HandleRunStateTransitionedEvent(JsonElement payload)
    {
        var outcome = ReadString(payload, "outcome", "Outcome") ?? string.Empty;
        var day = ReadInt(payload, "day", "Day");
        var details = day.HasValue ? $"day={day.Value}" : string.Empty;
        if (string.Equals(outcome, "win", StringComparison.OrdinalIgnoreCase))
        {
            ShowTemporaryFeedback("ui.run.win.day15", details, code: "run_win", priority: 2);
            return;
        }

        if (string.Equals(outcome, "loss", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outcome, "lose", StringComparison.OrdinalIgnoreCase))
        {
            ShowTemporaryFeedback("ui.run.lose.castle_fall", details, code: "run_lose", priority: 2);
        }
    }

    private void ShowTemporaryFeedback(string messageKey, string details, string code, int priority)
    {
        if (_hasPendingErrorDialog)
        {
            return;
        }

        var currentPriority = ResolveFeedbackPriority(_activeFeedbackCode, _activeFeedbackMessageKey);
        if (priority < currentPriority)
        {
            return;
        }

        _activeFeedbackCode = code;
        _activeFeedbackMessageKey = messageKey;
        _feedbackLabel.Text = BuildFeedbackDisplayText(messageKey, details, code);
        _feedbackLabel.Visible = !string.IsNullOrWhiteSpace(_feedbackLabel.Text);
        _feedbackHideAtMs = Time.GetTicksMsec() + (DefaultFeedbackTimeoutSeconds * 1000f);
    }

    private void ShowPersistentErrorDialog(string code, string messageKey, string details)
    {
        _hasPendingErrorDialog = true;
        _activeFeedbackCode = code;
        _activeFeedbackMessageKey = messageKey;
        _feedbackLabel.Visible = false;
        _feedbackLabel.Text = string.Empty;
        _feedbackHideAtMs = 0f;
        _errorDialog.Visible = true;
        _errorMessageLabel.Text = BuildFeedbackDisplayText(messageKey, details, code);
    }

    private void OnDismissFeedbackPressed()
    {
        _errorDialog.Visible = false;
        _errorMessageLabel.Text = string.Empty;
        _hasPendingErrorDialog = false;
    }

    private void UpdateFeedbackVisibility()
    {
        if (_hasPendingErrorDialog)
        {
            _feedbackLabel.Visible = false;
            return;
        }

        if (!_feedbackLabel.Visible)
        {
            return;
        }

        if (_feedbackHideAtMs <= 0f)
        {
            return;
        }

        if (Time.GetTicksMsec() >= _feedbackHideAtMs)
        {
            _feedbackLabel.Visible = false;
            _feedbackLabel.Text = string.Empty;
            _activeFeedbackCode = string.Empty;
            _activeFeedbackMessageKey = string.Empty;
            _feedbackHideAtMs = 0f;
        }
    }

    private static int ResolveFeedbackPriority(string code, string messageKey)
    {
        if (IsMigrationOrLoadFailure(code, messageKey))
        {
            return 3;
        }

        if (IsBlockedAction(code, messageKey))
        {
            return 2;
        }

        if (IsInvalidPlacement(code, messageKey))
        {
            return 1;
        }

        return 0;
    }

    private static bool IsInvalidPlacement(string code, string messageKey)
    {
        return InvalidPlacementCodes.Contains(code) ||
               messageKey.StartsWith("ui.invalid_action.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBlockedAction(string code, string messageKey)
    {
        return BlockedActionCodes.Contains(code) ||
               messageKey.StartsWith("ui.blocked_action.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRuntimeOutcomeMessage(string messageKey, string code)
    {
        return messageKey.StartsWith("ui.run.win.", StringComparison.OrdinalIgnoreCase) ||
               messageKey.StartsWith("ui.run.lose.", StringComparison.OrdinalIgnoreCase) ||
               code.StartsWith("run.win.", StringComparison.OrdinalIgnoreCase) ||
               code.StartsWith("run.lose.", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "run_win", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "run_lose", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMigrationOrLoadFailure(string code, string messageKey)
    {
        return code.Contains("migration", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("load", StringComparison.OrdinalIgnoreCase) ||
               messageKey.StartsWith("ui.migration_failure.", StringComparison.OrdinalIgnoreCase) ||
               messageKey.StartsWith("ui.load_failure.", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildFeedbackDisplayText(string messageKey, string details, string fallbackCode)
    {
        var keyText = messageKey switch
        {
            _ when messageKey.StartsWith("ui.invalid_action.", StringComparison.OrdinalIgnoreCase) => "Invalid action.",
            _ when messageKey.StartsWith("ui.blocked_action.", StringComparison.OrdinalIgnoreCase) => "Action blocked.",
            _ when messageKey.StartsWith("ui.load_failure.", StringComparison.OrdinalIgnoreCase) => "Load failed.",
            _ when messageKey.StartsWith("ui.migration_failure.", StringComparison.OrdinalIgnoreCase) => "Migration failed.",
            _ when messageKey.StartsWith("ui.reward.offer.", StringComparison.OrdinalIgnoreCase) => "Reward offered.",
            _ when messageKey.StartsWith("ui.run.win.", StringComparison.OrdinalIgnoreCase) => "Victory!",
            _ when messageKey.StartsWith("ui.run.lose.", StringComparison.OrdinalIgnoreCase) => "Defeat.",
            _ => string.Empty,
        };

        var detailText = details?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(keyText))
        {
            keyText = "Feedback";
        }

        if (string.IsNullOrWhiteSpace(detailText))
        {
            if (string.IsNullOrWhiteSpace(fallbackCode))
            {
                return keyText;
            }

            return $"{keyText} ({fallbackCode.Replace('_', ' ')})";
        }

        return $"{keyText} {detailText}";
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

    private static string? ReadString(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!element.TryGetProperty(key, out var valueElement))
            {
                continue;
            }

            if (valueElement.ValueKind == JsonValueKind.String)
            {
                return valueElement.GetString();
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

    public void ApplyConfigAuditView(global::Godot.Collections.Dictionary payload)
    {
        var activeConfig = ReadDictionaryString(payload, "active_config", "activeConfig", "config_id");
        var schemaStatus = ReadDictionaryString(payload, "schema_status", "schemaStatus");
        var fallbackPolicy = ReadDictionaryString(payload, "fallback_policy", "fallbackPolicy");
        var migrationStatus = ReadDictionaryString(payload, "migration_status", "migrationStatus");
        var reasonCode = ReadDictionaryString(payload, "reason_code", "reasonCode");
        var reportMetadata = ReadDictionaryString(payload, "report_metadata", "reportMetadata");

        _configAuditSummaryLabel.Text = $"Config: {activeConfig} | Schema: {schemaStatus} | Fallback: {fallbackPolicy}";
        _migrationStatusLabel.Text = string.IsNullOrWhiteSpace(reasonCode)
            ? $"Migration: {migrationStatus}"
            : $"Migration: {migrationStatus} ({reasonCode})";
        _reportMetadataLabel.Text = $"Metadata: {reportMetadata}";

        _configAuditPanel.Visible = true;
        _migrationStatusDialog.Visible = true;
        _reportMetadataPanel.Visible = true;
        _configAuditRefreshButton.Disabled = false;
        _migrationRetryButton.Disabled = false;
    }

    private static string ReadDictionaryString(global::Godot.Collections.Dictionary payload, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!payload.ContainsKey(key))
            {
                continue;
            }

            var value = payload[key];
            if (value.VariantType == Variant.Type.String)
            {
                var text = value.AsString().Trim();
                if (text.Length > 0)
                {
                    return text;
                }
            }

            if (value.VariantType != Variant.Type.Nil)
            {
                var text = value.ToString().Trim();
                if (text.Length > 0)
                {
                    return text;
                }
            }
        }

        return "n/a";
    }
}
