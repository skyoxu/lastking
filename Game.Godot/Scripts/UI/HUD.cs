using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Contracts;
using Game.Core.Contracts.Lastking;
using Game.Godot.Adapters;
using Game.Godot.Scripts.Runtime;
using Game.Core.Services;
using Godot;
using GDictionary = Godot.Collections.Dictionary;

namespace Game.Godot.Scripts.UI;

public partial class HUD : Control
{
    private const string BattleActionRequestedSignal = "BattleActionRequested";

    [Signal]
    public delegate void BattleActionRequestedEventHandler(string actionCode);

    private static readonly JsonDocumentOptions EventJsonOptions = new() { MaxDepth = 16 };
    private const double DefaultDayDurationSeconds = 240d;
    private const double DefaultNightDurationSeconds = 120d;
    private const ulong WaveCooldownPulseDurationMs = 1500;
    private readonly HudAfterActionComposer _afterActionComposer = new();
    private readonly RuntimePressureStateMapper _pressureStateMapper = new();

    private EventBusAdapter? _bus;
    private Control _topBar = default!;
    private Label _day = default!;
    private Label _cycleRemaining = default!;
    private Label _health = default!;
    private Label _speedStateLabel = default!;
    private PanelContainer _bottomBar = default!;
    private Label _combatCountsLabel = default!;
    private Label _moraleLabel = default!;
    private Control _buildAction = default!;
    private Label _buildStatusLabel = default!;
    private Control _buildCooldownMask = default!;
    private Control _waveAction = default!;
    private Label _waveStatusLabel = default!;
    private Control _waveCooldownMask = default!;
    private Control _exchangeAction = default!;
    private Label _exchangeStatusLabel = default!;
    private Control _exchangeCooldownMask = default!;
    private Control _cleanupAction = default!;
    private Label _cleanupStatusLabel = default!;
    private Control _cleanupCooldownMask = default!;
    private Control _finishAction = default!;
    private Label _finishStatusLabel = default!;
    private Control _finishCooldownMask = default!;
    private Control _backAction = default!;
    private Label _backStatusLabel = default!;
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
    private string _currentPressureState = "n/a";
    private float _waveCooldownMaskAlpha = 0f;
    private ulong _waveCooldownHideAtMs;
    private bool _battleHudActive;
    private static readonly Vector2 FormalTopBarPosition = new(8f, 0f);
    private static readonly Vector2 FormalTopBarSize = new(1584f, 80f);
    private static readonly Vector2 FormalBottomBarPosition = new(8f, 704f);
    private static readonly Vector2 FormalBottomBarSize = new(1584f, 196f);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Pass;
        if (!HasSignal(BattleActionRequestedSignal))
        {
            AddUserSignal(BattleActionRequestedSignal);
        }
        _topBar = GetNode<Control>("TopBar");
        _day = GetNode<Label>("TopBar/HBox/DayLabel");
        _cycleRemaining = GetNode<Label>("TopBar/HBox/CycleRemainingLabel");
        _health = GetNode<Label>("TopBar/HBox/HealthLabel");
        _speedStateLabel = GetNode<Label>("TopBar/HBox/SpeedStateLabel");
        _bottomBar = GetNode<PanelContainer>("CombatHud/BottomBar");
        _combatCountsLabel = GetNode<Label>("CombatHud/BottomBar/VBox/CombatCountsLabel");
        _moraleLabel = GetNode<Label>("CombatHud/BottomBar/VBox/MoraleLabel");
        _buildAction = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/BuildAction");
        _buildStatusLabel = GetNode<Label>("CombatHud/BottomBar/VBox/Actions/BuildAction/Frame/Content/Status");
        _buildCooldownMask = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/BuildAction/CooldownMask");
        _waveAction = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/WaveAction");
        _waveStatusLabel = GetNode<Label>("CombatHud/BottomBar/VBox/Actions/WaveAction/Frame/Content/Status");
        _waveCooldownMask = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/WaveAction/CooldownMask");
        _exchangeAction = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/ExchangeAction");
        _exchangeStatusLabel = GetNode<Label>("CombatHud/BottomBar/VBox/Actions/ExchangeAction/Frame/Content/Status");
        _exchangeCooldownMask = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/ExchangeAction/CooldownMask");
        _cleanupAction = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/CleanupAction");
        _cleanupStatusLabel = GetNode<Label>("CombatHud/BottomBar/VBox/Actions/CleanupAction/Frame/Content/Status");
        _cleanupCooldownMask = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/CleanupAction/CooldownMask");
        _finishAction = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/FinishAction");
        _finishStatusLabel = GetNode<Label>("CombatHud/BottomBar/VBox/Actions/FinishAction/Frame/Content/Status");
        _finishCooldownMask = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/FinishAction/CooldownMask");
        _backAction = GetNode<Control>("CombatHud/BottomBar/VBox/Actions/BackAction");
        _backStatusLabel = GetNode<Label>("CombatHud/BottomBar/VBox/Actions/BackAction/Frame/Content/Status");
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
        _bottomBar.Visible = true;
        _combatCountsLabel.Text = $"{T("hud.enemies")}: 0";
        _moraleLabel.Text = $"{T("hud.castle")}: 0/100";
        _speedStateLabel.Text = $"{T("hud.speed_state")}: {T("hud.speed_1x")}";
        _pauseButton.Pressed += OnPausePressed;
        _oneXButton.Pressed += OnOneXPressed;
        _twoXButton.Pressed += OnTwoXPressed;
        _dismissButton.Pressed += OnDismissFeedbackPressed;
        _buildAction.GuiInput += (@event) => OnBattleActionGuiInput(@event, "build");
        _waveAction.GuiInput += (@event) => OnBattleActionGuiInput(@event, "wave");
        _exchangeAction.GuiInput += (@event) => OnBattleActionGuiInput(@event, "exchange");
        _cleanupAction.GuiInput += (@event) => OnBattleActionGuiInput(@event, "cleanup");
        _finishAction.GuiInput += (@event) => OnBattleActionGuiInput(@event, "finish");
        _backAction.GuiInput += (@event) => OnBattleActionGuiInput(@event, "back");
        _feedbackLabel.Visible = false;
        _feedbackLabel.Text = string.Empty;
        _feedbackLayer.Visible = true;
        _feedbackLayer.MouseFilter = MouseFilterEnum.Ignore;
        _pressurePanel.Visible = false;
        _pressureLabel.Text = $"{T("hud.pressure")}: n/a";
        _cameraControlOverlay.Visible = false;
        _cameraStatusLabel.Text = $"{T("hud.camera")}: {T("hud.camera.idle")}";
        _errorDialog.Visible = false;
        _errorMessageLabel.Text = string.Empty;
        _configAuditPanel.Visible = false;
        _migrationStatusDialog.Visible = false;
        _reportMetadataPanel.Visible = false;
        _outcomePanel.Visible = false;
        _runtimePromptPanel.Visible = false;
        _resourcePanel.Visible = false;
        _buildPanel.Visible = false;
        _progressionPanel.Visible = false;
        _configAuditSummaryLabel.Text = $"{T("hud.config")}: n/a | {T("hud.schema")}: n/a | {T("hud.fallback")}: n/a";
        _migrationStatusLabel.Text = $"{T("hud.migration")}: n/a";
        _reportMetadataLabel.Text = $"{T("hud.metadata")}: n/a";
        _outcomeLabel.Text = $"{T("hud.outcome")}: n/a";
        _runtimePromptLabel.Text = $"{T("hud.prompt")}: n/a";
        _resourceSummaryLabel.Text = $"{T("hud.resources")}: gold=n/a iron=n/a pop=n/a";
        _buildSummaryLabel.Text = $"{T("hud.build")}: tax=n/a total_gold=n/a";
        _progressionSummaryLabel.Text = $"{T("hud.progression")}: tech=n/a reward=n/a";
        _backStatusLabel.Text = "Menu";
        ApplyActionAvailability(
            buildAvailable: true,
            waveAvailable: true,
            exchangeAvailable: false,
            cleanupAvailable: false,
            finishAvailable: false);
        SetCooldownMask(_buildCooldownMask, false, 0f);
        SetCooldownMask(_waveCooldownMask, false, 0f);
        SetCooldownMask(_exchangeCooldownMask, false, 0f);
        SetCooldownMask(_cleanupCooldownMask, false, 0f);
        SetCooldownMask(_finishCooldownMask, false, 0f);
        _activeFeedbackCode = string.Empty;
        _activeFeedbackMessageKey = string.Empty;
        _hasPendingErrorDialog = false;
        _feedbackHideAtMs = 0f;
        SetBattleHudActive(Visible);

        _bus = GetNodeOrNull<EventBusAdapter>("/root/EventBus");
        if (_bus != null)
        {
            _bus.Connect(EventBusAdapter.SignalName.DomainEventEmitted, new Callable(this, nameof(OnDomainEventEmitted)));
        }
    }

    public override void _Process(double delta)
    {
        SyncBattleHudActiveState();
        ApplyFormalBattleHudBands();
        var locale = NormalizeLocale(TranslationServer.GetLocale());
        if (!string.Equals(locale, _localizedLocale, StringComparison.OrdinalIgnoreCase))
        {
            _localizedLocale = locale;
            _i18n?.Call("switch_locale", _localizedLocale);
            ApplyLocalizedStaticTexts();
            RenderDay();
            RenderCycleRemaining();
        }

        RefreshSpeedControlsFromRuntime();
        RefreshBottomBarFromRuntime();
        TickCooldownMasks(Math.Max(0d, delta));

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

    public void AdvanceUiFrameForTest(double delta)
    {
        if (_waveCooldownHideAtMs > 0)
        {
            var nowMs = Time.GetTicksMsec();
            var remainingMs = _waveCooldownHideAtMs > nowMs
                ? _waveCooldownHideAtMs - nowMs
                : 0UL;
            var stepMs = Math.Max(1UL, (ulong)Math.Round(Math.Max(0d, delta) * 1000d));
            remainingMs = remainingMs > stepMs
                ? remainingMs - stepMs
                : 0UL;
            _waveCooldownHideAtMs = remainingMs > 0
                ? nowMs + remainingMs
                : 0UL;
        }
        TickCooldownMasks(Math.Max(0d, delta));
        UpdateFeedbackVisibility();
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

        _waveCooldownMaskAlpha = 0f;
        _waveCooldownHideAtMs = 0;
        SetCooldownMask(_waveCooldownMask, false, 0f);
        _waveAction.Modulate = new Color(1f, 1f, 1f, 1f);
        RefreshBottomBarFromRuntime();
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
        _waveCooldownMaskAlpha = 1f;
        _waveCooldownHideAtMs = Time.GetTicksMsec() + WaveCooldownPulseDurationMs;
        RefreshBottomBarFromRuntime();
        _waveAction.Modulate = new Color(1f, 1f, 1f, 0.5f);
        SetCooldownMask(_waveCooldownMask, true, _waveCooldownMaskAlpha);
        RenderPressureSummary();
    }

    private void HandleCameraScrolledEvent(JsonElement payload)
    {
        var dx = ReadInt(payload, "dx", "Dx", "delta_x", "DeltaX");
        var dy = ReadInt(payload, "dy", "Dy", "delta_y", "DeltaY");
        if (dx.HasValue && dy.HasValue)
        {
            _cameraControlOverlay.Visible = !IsBattleMapHudContext();
            _cameraStatusLabel.Text = $"{T("hud.camera")}: dx={dx.Value} dy={dy.Value}";
            return;
        }

        var mode = ReadString(payload, "mode", "Mode");
        if (!string.IsNullOrWhiteSpace(mode))
        {
            _cameraControlOverlay.Visible = !IsBattleMapHudContext();
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
        }
        RefreshBottomBarFromRuntime();
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
        RefreshBottomBarFromRuntime();
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
        RefreshBottomBarFromRuntime();
    }

    private void RenderRuntimePrompt(string messageKey, string details, string fallbackCode)
    {
        _runtimePromptLabel.Text = $"{T("hud.prompt")}: n/a";
        HideLegacyFeedbackPanelsForBattleMap();
    }

    private void UpdatePressureLabelFromHp(int hp)
    {
        _currentPressureState = _pressureStateMapper.MapCastleHp(hp);
        _pressurePanel.Visible = !IsBattleMapHudContext();
        _pressureLabel.Text = $"{T("hud.pressure")}: {_currentPressureState} (hp={hp})";
        RefreshBottomBarFromRuntime();
    }

    private void RenderPressureSummary()
    {
        if (_lastCastleHpChanged is null)
        {
            _pressurePanel.Visible = false;
            _pressureLabel.Text = $"{T("hud.pressure")}: n/a";
            return;
        }

        _pressurePanel.Visible = !IsBattleMapHudContext();
        _pressureLabel.Text = $"{T("hud.pressure")}: {_currentPressureState} (hp={_lastCastleHpChanged.CurrentHp})";
        RefreshBottomBarFromRuntime();
    }

    private void RefreshBottomBarFromRuntime()
    {
        HideLegacyFeedbackPanelsForBattleMap();
        var runtimeSummary = TryGetActiveBattleSummary();
        var operationController = TryGetActiveOperationController();
        var current = ReadSummaryInt(runtimeSummary, "enemy_units_spawned", _lastWaveSpawned?.SpawnCount ?? 0);
        _combatCountsLabel.Text = $"{T("hud.enemies")}: {current}";
        var runtimeCastleHp = ReadSummaryInt(runtimeSummary, "castle_hp", _lastCastleHpChanged?.CurrentHp ?? 0);
        _moraleLabel.Text = $"{T("hud.castle")}: {Math.Clamp(runtimeCastleHp, 0, 100)}/100";

        var phaseStatuses = TryGetBattleHudPhaseStatuses(operationController);
        var buildAvailable = ReadDictionaryBool(phaseStatuses, "build_available", true);
        var waveAvailable = ReadDictionaryBool(phaseStatuses, "wave_available", true);
        var exchangeAvailable = ReadDictionaryBool(phaseStatuses, "exchange_available", false);
        var cleanupAvailable = ReadDictionaryBool(phaseStatuses, "cleanup_available", false);
        var finishAvailable = ReadDictionaryBool(phaseStatuses, "finish_available", false);
        ApplyActionAvailability(
            buildAvailable,
            waveAvailable,
            exchangeAvailable,
            cleanupAvailable,
            finishAvailable);

        _buildStatusLabel.Text = ReadDictionaryString(phaseStatuses, "build_status", "Ready");
        _waveStatusLabel.Text = _waveCooldownHideAtMs > 0
            ? "Cooldown"
            : ReadDictionaryString(phaseStatuses, "wave_status", "Ready");
        _exchangeStatusLabel.Text = ReadDictionaryString(phaseStatuses, "exchange_status", "Locked");
        _cleanupStatusLabel.Text = ReadDictionaryString(phaseStatuses, "cleanup_status", "Locked");
        _finishStatusLabel.Text = ReadDictionaryString(phaseStatuses, "finish_status", "Locked");
    }

    private void ApplyActionAvailability(bool buildAvailable, bool waveAvailable, bool exchangeAvailable, bool cleanupAvailable, bool finishAvailable)
    {
        _buildAction.Modulate = new Color(1f, 1f, 1f, buildAvailable ? 1f : 0.5f);
        _waveAction.Modulate = new Color(1f, 1f, 1f, waveAvailable ? 1f : 0.5f);
        _exchangeAction.Modulate = new Color(1f, 1f, 1f, exchangeAvailable ? 1f : 0.5f);
        _cleanupAction.Modulate = new Color(1f, 1f, 1f, cleanupAvailable ? 1f : 0.5f);
        _finishAction.Modulate = new Color(1f, 1f, 1f, finishAvailable ? 1f : 0.5f);
    }

    private static int ReadSummaryInt(GDictionary summary, string key, int fallback)
    {
        if (summary.Count == 0 || !summary.ContainsKey(key))
        {
            return fallback;
        }

        var value = summary[key];
        return value.VariantType switch
        {
            Variant.Type.Int => value.AsInt32(),
            Variant.Type.Float => (int)value.AsDouble(),
            Variant.Type.String when int.TryParse(value.AsString(), out var parsed) => parsed,
            _ => fallback,
        };
    }

    private Node? ResolveActiveBattleScreen()
    {
        var main = ResolveMainRoot();
        var screenRoot = main?.GetNodeOrNull<Node>("RuntimeUi/ScreenRoot");
        if (screenRoot != null && screenRoot.GetChildCount() > 0)
        {
            var screenFromMain = screenRoot.GetChild(0);
            if (screenFromMain != null && string.Equals(screenFromMain.Name, "BattleMapScreen", StringComparison.Ordinal))
            {
                return screenFromMain;
            }
        }

        Node? current = this;
        while (current != null)
        {
            if (string.Equals(current.Name, "BattleMapScreen", StringComparison.Ordinal))
            {
                return current;
            }

            current = current.GetParent();
        }

        return null;
    }

    private GDictionary TryGetActiveBattleSummary()
    {
        var screen = ResolveActiveBattleScreen();
        if (screen == null)
        {
            return new GDictionary();
        }

        var bridge = screen.GetNodeOrNull<Node>("CombatExperienceRuntimeBridge");
        if (bridge == null || !bridge.HasMethod("GetSummary"))
        {
            return new GDictionary();
        }

        var summary = bridge.Call("GetSummary");
        return summary.VariantType == Variant.Type.Dictionary
            ? summary.AsGodotDictionary()
            : new GDictionary();
    }

    private Node? TryGetActiveOperationController()
    {
        var screen = ResolveActiveBattleScreen();
        if (screen == null)
        {
            return null;
        }

        return screen.GetNodeOrNull<Node>("OperationController");
    }

    private static bool ReadControllerFlag(Node? controller, string methodName)
    {
        if (controller == null || !controller.HasMethod(methodName))
        {
            return false;
        }

        var value = controller.Call(methodName);
        return value.VariantType == Variant.Type.Bool && value.AsBool();
    }

    private static GDictionary TryGetBattleHudPhaseStatuses(Node? controller)
    {
        if (controller == null || !controller.HasMethod("get_hud_phase_statuses"))
        {
            return new GDictionary();
        }

        var value = controller.Call("get_hud_phase_statuses");
        return value.VariantType == Variant.Type.Dictionary
            ? value.AsGodotDictionary()
            : new GDictionary();
    }

    private static string ReadDictionaryString(GDictionary payload, string key, string fallback)
    {
        if (payload.Count == 0 || !payload.ContainsKey(key))
        {
            return fallback;
        }

        var value = payload[key];
        return value.VariantType switch
        {
            Variant.Type.String => string.IsNullOrWhiteSpace(value.AsString()) ? fallback : value.AsString(),
            Variant.Type.Nil => fallback,
            _ => string.IsNullOrWhiteSpace(value.ToString()) ? fallback : value.ToString(),
        };
    }

    private static bool ReadDictionaryBool(GDictionary payload, string key, bool fallback)
    {
        if (payload.Count == 0 || !payload.ContainsKey(key))
        {
            return fallback;
        }

        var value = payload[key];
        return value.VariantType switch
        {
            Variant.Type.Bool => value.AsBool(),
            Variant.Type.String when bool.TryParse(value.AsString(), out var parsed) => parsed,
            _ => fallback,
        };
    }

    private Node? ResolveMainRoot()
    {
        Node? current = this;
        while (current != null)
        {
            if (string.Equals(current.Name, "Main", StringComparison.Ordinal))
            {
                return current;
            }

            current = current.GetParent();
        }

        return null;
    }

    private void OnBattleActionGuiInput(InputEvent @event, string actionCode)
    {
        if (!IsBattleActionInput(@event))
        {
            return;
        }

        RequestBattleAction(actionCode);
    }

    public void RequestBattleAction(string actionCode)
    {
        if (!_battleHudActive)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(actionCode))
        {
            return;
        }

        EmitSignal(BattleActionRequestedSignal, actionCode);
        TryRouteBattleActionThroughActiveCoordinator(actionCode);
    }

    private void TryRouteBattleActionThroughActiveCoordinator(string actionCode)
    {
        var screen = ResolveActiveBattleScreen();
        if (screen == null)
        {
            return;
        }

        var coordinator = screen.GetNodeOrNull<Node>("HudCoordinator");
        if (coordinator == null || !coordinator.HasMethod("route_action"))
        {
            return;
        }

        coordinator.Call("route_action", actionCode);
    }

    public void SetBattleHudActive(bool active)
    {
        ApplyBattleHudActiveState(active);
    }

    private void SyncBattleHudActiveState()
    {
        var shouldBeActive = ShouldBattleHudBeActive();
        if (_battleHudActive == shouldBeActive
            && Visible == shouldBeActive
            && _topBar.Visible == shouldBeActive
            && _bottomBar.Visible == shouldBeActive
            && _feedbackLayer.Visible == shouldBeActive)
        {
            return;
        }

        ApplyBattleHudActiveState(shouldBeActive);
    }

    private bool ShouldBattleHudBeActive()
    {
        var owningBattleScreen = ResolveOwningBattleScreen();
        var main = ResolveMainRoot();
        if (main != null)
        {
            var globalHud = main.GetNodeOrNull<Control>("RuntimeUi/HUD");
            if (globalHud != null && ReferenceEquals(this, globalHud))
            {
                return ResolveActiveBattleScreen() != null;
            }

            if (owningBattleScreen != null)
            {
                return globalHud == null;
            }

            return false;
        }

        if (owningBattleScreen != null)
        {
            return true;
        }

        return true;
    }

    private Node? ResolveOwningBattleScreen()
    {
        Node? current = this;
        while (current != null)
        {
            if (string.Equals(current.Name, "BattleMapScreen", StringComparison.Ordinal))
            {
                return current;
            }

            current = current.GetParent();
        }

        return null;
    }

    private bool IsBattleMapHudContext()
    {
        return ResolveActiveBattleScreen() != null;
    }

    private void HideLegacyFeedbackPanelsForBattleMap()
    {
        if (!IsBattleMapHudContext())
        {
            return;
        }

        _pressurePanel.Visible = false;
        _cameraControlOverlay.Visible = false;
        _configAuditPanel.Visible = false;
        _migrationStatusDialog.Visible = false;
        _reportMetadataPanel.Visible = false;
        _outcomePanel.Visible = false;
        _runtimePromptPanel.Visible = false;
        _resourcePanel.Visible = false;
        _buildPanel.Visible = false;
        _progressionPanel.Visible = false;
    }

    private void ApplyBattleHudActiveState(bool active)
    {
        _battleHudActive = active;
        Visible = active;
        _topBar.Visible = active;
        _bottomBar.Visible = active;
        _feedbackLayer.Visible = active;
        MouseFilter = active ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
    }

    private void ApplyFormalBattleHudBands()
    {
        if (!_battleHudActive || !IsBattleMapHudContext())
        {
            return;
        }

        _topBar.Position = FormalTopBarPosition;
        _topBar.Size = FormalTopBarSize;
        _topBar.CustomMinimumSize = FormalTopBarSize;

        _bottomBar.Position = FormalBottomBarPosition;
        _bottomBar.Size = FormalBottomBarSize;
        _bottomBar.CustomMinimumSize = FormalBottomBarSize;
    }

    private static bool IsBattleActionInput(InputEvent @event)
    {
        return @event switch
        {
            InputEventMouseButton mouse => mouse.Pressed && mouse.ButtonIndex == MouseButton.Left,
            InputEventScreenTouch touch => touch.Pressed,
            _ => false,
        };
    }

    private void TickCooldownMasks(double delta)
    {
        if (_waveCooldownHideAtMs == 0)
        {
            _waveCooldownMaskAlpha = 0f;
            SetCooldownMask(_waveCooldownMask, false, 0f);
            _waveAction.Modulate = new Color(1f, 1f, 1f, 1f);
            return;
        }

        var nowMs = Time.GetTicksMsec();
        if (nowMs >= _waveCooldownHideAtMs)
        {
            _waveCooldownHideAtMs = 0;
            _waveCooldownMaskAlpha = 0f;
            SetCooldownMask(_waveCooldownMask, false, 0f);
            _waveAction.Modulate = new Color(1f, 1f, 1f, 1f);
            return;
        }

        var remainingMs = _waveCooldownHideAtMs - nowMs;
        _waveCooldownMaskAlpha = remainingMs / (float)WaveCooldownPulseDurationMs;
        SetCooldownMask(_waveCooldownMask, _waveCooldownMaskAlpha > 0f, _waveCooldownMaskAlpha);
        _waveAction.Modulate = new Color(1f, 1f, 1f, _waveCooldownMaskAlpha > 0f ? 0.5f : 1f);
    }

    private static void SetCooldownMask(Control mask, bool visible, float alpha)
    {
        mask.Visible = visible;
        if (mask is ColorRect rect)
        {
            rect.Color = new Color(0f, 0f, 0f, Math.Clamp(alpha, 0f, 1f) * 0.45f);
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
        ResolveGameManager()?.Call("SetPause");
        RefreshSpeedControlsFromRuntime();
    }

    private void OnOneXPressed()
    {
        ResolveGameManager()?.Call("SetOneX");
        RefreshSpeedControlsFromRuntime();
    }

    private void OnTwoXPressed()
    {
        ResolveGameManager()?.Call("SetTwoX");
        RefreshSpeedControlsFromRuntime();
    }

    private GameManager? ResolveGameManager()
    {
        return GetNodeOrNull<GameManager>("/root/GameManager") ?? GameManager.Instance;
    }

    private void RefreshSpeedControlsFromRuntime()
    {
        var manager = ResolveGameManager();
        if (manager == null)
        {
            _pauseButton.Disabled = true;
            _oneXButton.Disabled = true;
            _twoXButton.Disabled = true;
            _pauseButton.Text = T("hud.pause");
            _oneXButton.Text = T("hud.speed_1x");
            _twoXButton.Text = T("hud.speed_2x");
            return;
        }

        _pauseButton.Disabled = false;
        _oneXButton.Disabled = false;
        _twoXButton.Disabled = false;

        var state = manager.GetSpeedState();
        var isPaused = state.ContainsKey("is_paused") && state["is_paused"].AsBool();
        var scalePercent = state.ContainsKey("scale_percent") ? state["scale_percent"].AsInt32() : 100;

        _pauseButton.Text = isPaused ? T("hud.speed_paused") : T("hud.pause");
        _oneXButton.Text = !isPaused && scalePercent == 100 ? T("hud.speed_1x_active") : T("hud.speed_1x");
        _twoXButton.Text = !isPaused && scalePercent == 200 ? T("hud.speed_2x_active") : T("hud.speed_2x");
        _speedStateLabel.Text = isPaused
            ? $"{T("hud.speed_state")}: {T("hud.speed_paused")}"
            : scalePercent == 200
                ? $"{T("hud.speed_state")}: {T("hud.speed_2x")}"
                : $"{T("hud.speed_state")}: {T("hud.speed_1x")}";
        _pauseButton.Modulate = isPaused ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.72f);
        _oneXButton.Modulate = !isPaused && scalePercent == 100 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.72f);
        _twoXButton.Modulate = !isPaused && scalePercent == 200 ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.72f);
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

        _configAuditPanel.Visible = false;
        _migrationStatusDialog.Visible = false;
        _reportMetadataPanel.Visible = false;
        _configAuditRefreshButton.Disabled = true;
        _migrationRetryButton.Disabled = true;
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
