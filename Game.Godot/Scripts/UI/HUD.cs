using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Contracts;
using Game.Core.Contracts.Lastking;
using Game.Godot.Adapters;
using Game.Core.Services;
using Godot;

namespace Game.Godot.Scripts.UI;

public partial class HUD : Control
{
    private static readonly JsonDocumentOptions EventJsonOptions = new() { MaxDepth = 16 };
    private const double DefaultDayDurationSeconds = 240d;
    private const double DefaultNightDurationSeconds = 120d;
    private readonly HudAfterActionComposer _afterActionComposer = new();

    private EventBusAdapter? _bus;
    private Label _day = default!;
    private Label _cycleRemaining = default!;
    private Label _health = default!;
    private Label _feedbackLabel = default!;
    private Control _feedbackLayer = default!;
    private PanelContainer _pressurePanel = default!;
    private Label _pressureLabel = default!;
    private PanelContainer _cameraControlOverlay = default!;
    private Label _cameraStatusLabel = default!;
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
    private PanelContainer _outcomePanel = default!;
    private Label _outcomeLabel = default!;
    private PanelContainer _runtimePromptPanel = default!;
    private Label _runtimePromptLabel = default!;
    private PanelContainer _resourcePanel = default!;
    private Label _resourceSummaryLabel = default!;
    private PanelContainer _buildPanel = default!;
    private Label _buildSummaryLabel = default!;
    private PanelContainer _progressionPanel = default!;
    private Label _progressionSummaryLabel = default!;
    private Button _pauseButton = default!;
    private Button _oneXButton = default!;
    private Button _twoXButton = default!;
    private GodotObject? _i18n;
    private string _localizedLocale = "en-US";
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
    private CastleHpChanged? _lastCastleHpChanged;
    private WaveSpawned? _lastWaveSpawned;
    private ResourcesChanged? _lastResourcesChanged;
    private TaxCollected? _lastTaxCollected;
    private TechApplied? _lastTechApplied;
    private RewardOffered? _lastRewardOffered;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.WhenPaused;
        _day = GetNode<Label>("TopBar/HBox/DayLabel");
        _cycleRemaining = GetNode<Label>("TopBar/HBox/CycleRemainingLabel");
        _health = GetNode<Label>("TopBar/HBox/HealthLabel");
        _feedbackLayer = GetNode<Control>("FeedbackLayer");
        _feedbackLabel = GetNode<Label>("FeedbackLayer/FeedbackLabel");
        _pressurePanel = GetNode<PanelContainer>("FeedbackLayer/PressurePanel");
        _pressureLabel = GetNode<Label>("FeedbackLayer/PressurePanel/VBox/PressureLabel");
        _cameraControlOverlay = GetNode<PanelContainer>("FeedbackLayer/CameraControlOverlay");
        _cameraStatusLabel = GetNode<Label>("FeedbackLayer/CameraControlOverlay/VBox/CameraStatusLabel");
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
        _outcomePanel = GetNode<PanelContainer>("FeedbackLayer/OutcomePanel");
        _outcomeLabel = GetNode<Label>("FeedbackLayer/OutcomePanel/VBox/OutcomeLabel");
        _runtimePromptPanel = GetNode<PanelContainer>("FeedbackLayer/RuntimePromptPanel");
        _runtimePromptLabel = GetNode<Label>("FeedbackLayer/RuntimePromptPanel/VBox/RuntimePromptLabel");
        _resourcePanel = GetNode<PanelContainer>("FeedbackLayer/ResourcePanel");
        _resourceSummaryLabel = GetNode<Label>("FeedbackLayer/ResourcePanel/VBox/ResourceSummaryLabel");
        _buildPanel = GetNode<PanelContainer>("FeedbackLayer/BuildPanel");
        _buildSummaryLabel = GetNode<Label>("FeedbackLayer/BuildPanel/VBox/BuildSummaryLabel");
        _progressionPanel = GetNode<PanelContainer>("FeedbackLayer/ProgressionPanel");
        _progressionSummaryLabel = GetNode<Label>("FeedbackLayer/ProgressionPanel/VBox/ProgressionSummaryLabel");
        _pauseButton = GetNode<Button>("TopBar/HBox/SpeedControls/PauseButton");
        _oneXButton = GetNode<Button>("TopBar/HBox/SpeedControls/OneXButton");
        _twoXButton = GetNode<Button>("TopBar/HBox/SpeedControls/TwoXButton");
        SetupLocalization();
        RenderDay();
        RenderCycleRemaining();
        _health.Text = $"{T("hud.hp")}: 0";
        _pauseButton.Pressed += OnPausePressed;
        _oneXButton.Pressed += OnOneXPressed;
        _twoXButton.Pressed += OnTwoXPressed;
        _dismissButton.Pressed += OnDismissFeedbackPressed;
        _feedbackLabel.Visible = false;
        _feedbackLabel.Text = string.Empty;
        _pressurePanel.Visible = true;
        _pressureLabel.Text = $"{T("hud.pressure")}: n/a";
        _cameraControlOverlay.Visible = true;
        _cameraStatusLabel.Text = $"{T("hud.camera")}: {T("hud.camera.idle")}";
        _errorDialog.Visible = false;
        _errorMessageLabel.Text = string.Empty;
        _configAuditPanel.Visible = true;
        _migrationStatusDialog.Visible = true;
        _reportMetadataPanel.Visible = true;
        _outcomePanel.Visible = true;
        _runtimePromptPanel.Visible = true;
        _resourcePanel.Visible = true;
        _buildPanel.Visible = true;
        _progressionPanel.Visible = true;
        _configAuditSummaryLabel.Text = $"{T("hud.config")}: n/a | {T("hud.schema")}: n/a | {T("hud.fallback")}: n/a";
        _migrationStatusLabel.Text = $"{T("hud.migration")}: n/a";
        _reportMetadataLabel.Text = $"{T("hud.metadata")}: n/a";
        _outcomeLabel.Text = $"{T("hud.outcome")}: n/a";
        _runtimePromptLabel.Text = $"{T("hud.prompt")}: n/a";
        _resourceSummaryLabel.Text = $"{T("hud.resources")}: gold=n/a iron=n/a pop=n/a";
        _buildSummaryLabel.Text = $"{T("hud.build")}: tax=n/a total_gold=n/a";
        _progressionSummaryLabel.Text = $"{T("hud.progression")}: tech=n/a reward=n/a";
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
        var locale = NormalizeLocale(TranslationServer.GetLocale());
        if (!string.Equals(locale, _localizedLocale, StringComparison.OrdinalIgnoreCase))
        {
            _localizedLocale = locale;
            _i18n?.Call("switch_locale", _localizedLocale);
            ApplyLocalizedStaticTexts();
            RenderDay();
            RenderCycleRemaining();
        }

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
        if (_bus == null || !GodotObject.IsInstanceValid(_bus))
        {
            return;
        }

        try
        {
            var callable = new Callable(this, nameof(OnDomainEventEmitted));
            if (_bus.IsConnected(EventBusAdapter.SignalName.DomainEventEmitted, callable))
            {
                _bus.Disconnect(EventBusAdapter.SignalName.DomainEventEmitted, callable);
            }
        }
        catch (ObjectDisposedException)
        {
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
            type != EventTypes.LastkingWaveSpawned &&
            type != EventTypes.LastkingCameraScrolled &&
            type != EventTypes.LastkingRewardOffered &&
            type != EventTypes.LastkingResourcesChanged &&
            type != EventTypes.LastkingTaxCollected &&
            type != EventTypes.LastkingTechApplied &&
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
                _health.Text = $"{T("hud.hp")}: {hp.Value}";
                UpdatePressureLabelFromHp(hp.Value);
                var hpRunId = ReadString(doc.RootElement, "RunId", "run_id") ?? "runtime";
                var hpDay = ReadInt(doc.RootElement, "DayNumber", "day", "Day") ?? _currentDay;
                var previousHp = ReadInt(doc.RootElement, "previous_hp", "PreviousHp") ?? hp.Value;
                _lastCastleHpChanged = new CastleHpChanged(
                    hpRunId,
                    hpDay,
                    previousHp,
                    hp.Value,
                    DateTimeOffset.UtcNow);
            }

            if (type == EventTypes.LastkingRewardOffered)
            {
                HandleRewardOfferedEvent(doc.RootElement);
            }

            if (type == EventTypes.LastkingResourcesChanged)
            {
                HandleResourcesChangedEvent(doc.RootElement);
            }

            if (type == EventTypes.LastkingTaxCollected)
            {
                HandleTaxCollectedEvent(doc.RootElement);
            }

            if (type == EventTypes.LastkingTechApplied)
            {
                HandleTechAppliedEvent(doc.RootElement);
            }

            if (type == EventTypes.RunStateTransitioned)
            {
                HandleRunStateTransitionedEvent(doc.RootElement);
            }

            if (type == EventTypes.LastkingWaveSpawned)
            {
                HandleWaveSpawnedEvent(doc.RootElement);
            }

            if (type == EventTypes.LastkingCameraScrolled)
            {
                HandleCameraScrolledEvent(doc.RootElement);
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
        RenderRuntimePrompt(messageKey, details, code);

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
        _lastRewardOffered = new RewardOffered(
            ReadString(payload, "RunId", "run_id") ?? "runtime",
            ReadInt(payload, "DayNumber", "day", "Day") ?? _currentDay,
            ReadBool(payload, "is_elite_night", "IsEliteNight"),
            ReadBool(payload, "is_boss_night", "IsBossNight"),
            ReadString(payload, "option_a", "OptionA") ?? string.Empty,
            ReadString(payload, "option_b", "OptionB") ?? string.Empty,
            ReadString(payload, "option_c", "OptionC") ?? string.Empty,
            DateTimeOffset.UtcNow);
        var optionA = ReadString(payload, "option_a", "OptionA");
        var optionB = ReadString(payload, "option_b", "OptionB");
        var optionC = ReadString(payload, "option_c", "OptionC");
        var details = string.Join(", ", new[] { optionA, optionB, optionC }.Where(value => !string.IsNullOrWhiteSpace(value)));
        ShowTemporaryFeedback("ui.reward.offer.presented", details, code: "reward_offered", priority: 1);
        _progressionSummaryLabel.Text = string.IsNullOrWhiteSpace(details)
            ? "Progression: tech=n/a reward=offered"
            : $"Progression: tech=n/a reward={details}";
    }

    private void HandleRunStateTransitionedEvent(JsonElement payload)
    {
        var outcome = ReadString(payload, "outcome", "Outcome") ?? string.Empty;
        var day = ReadInt(payload, "day", "Day");
        var isTerminalOutcome =
            string.Equals(outcome, "win", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outcome, "loss", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(outcome, "lose", StringComparison.OrdinalIgnoreCase);
        var isNonTerminalOutcome = !isTerminalOutcome;
        if (isNonTerminalOutcome)
        {
            _outcomeLabel.Text = $"{T("hud.outcome")}: n/a";
            _runtimePromptLabel.Text = $"{T("hud.prompt")}: n/a";
            return;
        }

        var presentation = _afterActionComposer.Compose(new HudAfterActionInputs(
            Outcome: outcome,
            DayNumber: day,
            CastleHp: _lastCastleHpChanged,
            Wave: _lastWaveSpawned,
            Resources: _lastResourcesChanged,
            Tax: _lastTaxCollected,
            Tech: _lastTechApplied,
            Reward: _lastRewardOffered));
        _outcomeLabel.Text = presentation.OutcomeText;
        _runtimePromptLabel.Text = presentation.PromptText;
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
            return;
        }
    }

    private void HandleWaveSpawnedEvent(JsonElement payload)
    {
        var count = ReadInt(payload, "count", "Count", "spawn_count", "SpawnCount");
        var day = ReadInt(payload, "day", "Day");
        _lastWaveSpawned = new WaveSpawned(
            ReadString(payload, "RunId", "run_id") ?? "runtime",
            day ?? _currentDay,
            ReadInt(payload, "NightNumber", "night", "Night") ?? 0,
            ReadString(payload, "LaneId", "lane_id", "lane") ?? "unknown",
            count ?? 0,
            ReadInt(payload, "WaveBudget", "wave_budget", "budget") ?? 0,
            DateTimeOffset.UtcNow);
        if (count.HasValue && day.HasValue)
        {
            _pressureLabel.Text = $"{T("hud.pressure")}: day={day.Value} spawned={count.Value}";
            return;
        }

        if (count.HasValue)
        {
            _pressureLabel.Text = $"{T("hud.pressure")}: spawned={count.Value}";
        }
    }

    private void HandleCameraScrolledEvent(JsonElement payload)
    {
        var dx = ReadInt(payload, "dx", "Dx", "delta_x", "DeltaX");
        var dy = ReadInt(payload, "dy", "Dy", "delta_y", "DeltaY");
        if (dx.HasValue && dy.HasValue)
        {
            _cameraStatusLabel.Text = $"{T("hud.camera")}: dx={dx.Value} dy={dy.Value}";
            return;
        }

        var mode = ReadString(payload, "mode", "Mode");
        if (!string.IsNullOrWhiteSpace(mode))
        {
            _cameraStatusLabel.Text = $"{T("hud.camera")}: {mode}";
        }
    }

    private void HandleResourcesChangedEvent(JsonElement payload)
    {
        var gold = ReadInt(payload, "gold", "Gold");
        var iron = ReadInt(payload, "iron", "Iron");
        var popCap = ReadInt(payload, "population_cap", "PopulationCap");
        _lastResourcesChanged = new ResourcesChanged(
            ReadString(payload, "RunId", "run_id") ?? "runtime",
            ReadInt(payload, "DayNumber", "day", "Day") ?? _currentDay,
            gold ?? 0,
            iron ?? 0,
            popCap ?? 0,
            DateTimeOffset.UtcNow);
        if (gold.HasValue || iron.HasValue || popCap.HasValue)
        {
            _resourceSummaryLabel.Text =
                $"{T("hud.resources")}: gold={DisplayInt(gold)} iron={DisplayInt(iron)} pop={DisplayInt(popCap)}";
        }
    }

    private void HandleTaxCollectedEvent(JsonElement payload)
    {
        var taxDelta = ReadInt(payload, "gold_delta", "GoldDelta");
        var totalGold = ReadInt(payload, "total_gold", "TotalGold", "gold_after", "GoldAfter");
        var residenceId = ReadString(payload, "residence_id", "ResidenceId");
        _lastTaxCollected = new TaxCollected(
            ReadString(payload, "RunId", "run_id") ?? "runtime",
            ReadInt(payload, "DayNumber", "day", "Day") ?? _currentDay,
            residenceId ?? string.Empty,
            taxDelta ?? 0,
            totalGold ?? 0,
            DateTimeOffset.UtcNow);
        var details = !string.IsNullOrWhiteSpace(residenceId) ? $"residence={residenceId}" : "residence=n/a";
        _buildSummaryLabel.Text =
            $"{T("hud.build")}: tax={DisplayInt(taxDelta)} total_gold={DisplayInt(totalGold)} {details}";
    }

    private void HandleTechAppliedEvent(JsonElement payload)
    {
        var techId = ReadString(payload, "tech_id", "TechId");
        var statKey = ReadString(payload, "stat_key", "StatKey");
        var previous = ReadInt(payload, "previous_value", "PreviousValue");
        var current = ReadInt(payload, "current_value", "CurrentValue");
        _lastTechApplied = new TechApplied(
            ReadString(payload, "RunId", "run_id") ?? "runtime",
            techId ?? string.Empty,
            statKey ?? string.Empty,
            previous ?? 0,
            current ?? 0,
            DateTimeOffset.UtcNow);
        var techText = !string.IsNullOrWhiteSpace(techId) ? techId : "n/a";
        var statText = !string.IsNullOrWhiteSpace(statKey) ? statKey : "n/a";
        _progressionSummaryLabel.Text =
            $"{T("hud.progression")}: tech={techText}:{statText} {DisplayInt(previous)}->{DisplayInt(current)} reward=n/a";
    }

    private void RenderRuntimePrompt(string messageKey, string details, string fallbackCode)
    {
        var text = BuildFeedbackDisplayText(messageKey, details, fallbackCode);
        _runtimePromptLabel.Text = string.IsNullOrWhiteSpace(text)
            ? $"{T("hud.prompt")}: n/a"
            : $"{T("hud.prompt")}: {text}";
    }

    private void UpdatePressureLabelFromHp(int hp)
    {
        if (hp <= 20)
        {
            _pressureLabel.Text = $"{T("hud.pressure")}: critical (hp={hp})";
            return;
        }

        if (hp <= 60)
        {
            _pressureLabel.Text = $"{T("hud.pressure")}: high (hp={hp})";
            return;
        }

        _pressureLabel.Text = $"{T("hud.pressure")}: stable (hp={hp})";
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
        _feedbackLabel.Text = TranslateFeedbackText(BuildFeedbackDisplayText(messageKey, details, code));
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
        _errorMessageLabel.Text = TranslateFeedbackText(BuildFeedbackDisplayText(messageKey, details, code));
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
               messageKey.StartsWith("ui.blocked_action.", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(messageKey, "ui.combat.target_path_blocked_fallback", StringComparison.OrdinalIgnoreCase);
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

    private string BuildFeedbackDisplayText(string messageKey, string details, string fallbackCode)
    {
        var keyText = messageKey switch
        {
            _ when messageKey.StartsWith("ui.invalid_action.", StringComparison.OrdinalIgnoreCase) => "hud.feedback.invalid_action",
            _ when messageKey.StartsWith("ui.blocked_action.", StringComparison.OrdinalIgnoreCase) => "hud.feedback.blocked_action",
            _ when messageKey.StartsWith("ui.load_failure.", StringComparison.OrdinalIgnoreCase) => "hud.feedback.load_failed",
            _ when messageKey.StartsWith("ui.migration_failure.", StringComparison.OrdinalIgnoreCase) => "hud.feedback.migration_failed",
            _ when messageKey.StartsWith("ui.reward.offer.", StringComparison.OrdinalIgnoreCase) => "hud.feedback.reward_offered",
            _ when messageKey.StartsWith("ui.run.win.", StringComparison.OrdinalIgnoreCase) => "hud.feedback.victory",
            _ when messageKey.StartsWith("ui.run.lose.", StringComparison.OrdinalIgnoreCase) => "hud.feedback.defeat",
            _ => string.Empty,
        };
        keyText = T(keyText);

        var detailText = details?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(keyText))
        {
            keyText = "hud.feedback.generic";
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

    private static string DisplayInt(int? value)
    {
        return value.HasValue ? value.Value.ToString() : "n/a";
    }

    private string TranslateFeedbackText(string textOrKey)
    {
        if (string.IsNullOrWhiteSpace(textOrKey))
        {
            return textOrKey;
        }

        if (textOrKey.StartsWith("hud.feedback.", StringComparison.OrdinalIgnoreCase))
        {
            return T(textOrKey);
        }

        var sep = textOrKey.IndexOf(' ');
        if (sep <= 0)
        {
            return textOrKey;
        }

        var left = textOrKey.Substring(0, sep);
        var right = textOrKey.Substring(sep + 1);
        if (left.StartsWith("hud.feedback.", StringComparison.OrdinalIgnoreCase))
        {
            return $"{T(left)} {right}";
        }

        return textOrKey;
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

    private static bool ReadBool(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!element.TryGetProperty(key, out var valueElement))
            {
                continue;
            }

            if (valueElement.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (valueElement.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (valueElement.ValueKind == JsonValueKind.String &&
                bool.TryParse(valueElement.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return false;
    }

    private void RenderDay()
    {
        _day.Text = $"{T("hud.day")}: {_currentDay}";
    }

    private void RenderCycleRemaining()
    {
        var remaining = Math.Max(0d, _phaseDurationSeconds - _phaseElapsedSeconds);
        _cycleRemaining.Text = $"{T("hud.cycle_remaining")}: {remaining:0.0}s";
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
        _health.Text = $"{T("hud.hp")}: {hp}";
    }

    public void ApplyConfigAuditView(global::Godot.Collections.Dictionary payload)
    {
        var activeConfig = ReadDictionaryString(payload, "active_config", "activeConfig", "config_id");
        var schemaStatus = ReadDictionaryString(payload, "schema_status", "schemaStatus");
        var fallbackPolicy = ReadDictionaryString(payload, "fallback_policy", "fallbackPolicy");
        var migrationStatus = ReadDictionaryString(payload, "migration_status", "migrationStatus");
        var reasonCode = ReadDictionaryString(payload, "reason_code", "reasonCode");
        var reportMetadata = ReadDictionaryString(payload, "report_metadata", "reportMetadata");

        _configAuditSummaryLabel.Text = $"{T("hud.config")}: {activeConfig} | {T("hud.schema")}: {schemaStatus} | {T("hud.fallback")}: {fallbackPolicy}";
        _migrationStatusLabel.Text = string.IsNullOrWhiteSpace(reasonCode)
            ? $"{T("hud.migration")}: {migrationStatus}"
            : $"{T("hud.migration")}: {migrationStatus} ({reasonCode})";
        _reportMetadataLabel.Text = $"{T("hud.metadata")}: {reportMetadata}";

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

    private void SetupLocalization()
    {
        var script = GD.Load<Script>("res://Game.Godot/Scripts/Localization/LocalizationManager.gd");
        if (script == null)
        {
            return;
        }

        _i18n = (GodotObject)script.Call("new");
        _i18n?.Call("configure_locale_resource", "en-US", "res://Game.Godot/Localization/en-US.json");
        _i18n?.Call("configure_locale_resource", "zh-CN", "res://Game.Godot/Localization/zh-CN.json");
        _localizedLocale = NormalizeLocale(TranslationServer.GetLocale());
        _i18n?.Call("switch_locale", _localizedLocale);
        ApplyLocalizedStaticTexts();
    }

    private void ApplyLocalizedStaticTexts()
    {
        _pauseButton.Text = T("hud.pause");
        _oneXButton.Text = T("hud.speed_1x");
        _twoXButton.Text = T("hud.speed_2x");
        _dismissButton.Text = T("hud.dismiss");
        _configAuditRefreshButton.Text = T("hud.refresh_audit");
        _migrationRetryButton.Text = T("hud.retry_migration");
    }

    private string T(string key)
    {
        if (_i18n == null || string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        var value = _i18n.Call("translate", key).AsString();
        return string.IsNullOrWhiteSpace(value) ? key : value;
    }

    private static string NormalizeLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return "en-US";
        }

        var normalized = locale.Trim().ToLowerInvariant();
        if (normalized == "zh" || normalized.StartsWith("zh"))
        {
            return "zh-CN";
        }

        return "en-US";
    }
}
