using Godot;
using Game.Godot.Adapters;
using System;
using System.Text.Json;

namespace Game.Godot.Scripts.UI;

public partial class MainMenu : Control
{
    private static readonly JsonDocumentOptions EventJsonOptions = new()
    {
        MaxDepth = 16,
    };

    private const string BootstrapReadyOverrideEnv = "LASTKING_BOOT_READY_OVERRIDE";
    private Button _btnPlay = default!;
    private Button _btnContinue = default!;
    private Button _btnSettings = default!;
    private Button _btnQuit = default!;
    private Button _btnRetryBootstrap = default!;
    private Button _btnDismissGate = default!;
    private PanelContainer _continueGateDialog = default!;
    private Label _gateMessageLabel = default!;
    private Label _bootStatusLabel = default!;
    private Label _exportStatusLabel = default!;
    private bool _bootstrapReady;
    private string _bootstrapNotReadyReason = string.Empty;
    private readonly Script _localizationScript = GD.Load<Script>("res://Game.Godot/Scripts/Localization/LocalizationManager.gd");
    private GodotObject? _i18n;
    private string _currentLocale = "en-US";

    public override void _Ready()
    {
        _btnPlay = GetNode<Button>("VBox/BtnPlay");
        _btnContinue = GetNode<Button>("VBox/BtnContinue");
        _btnSettings = GetNode<Button>("VBox/BtnSettings");
        _btnQuit = GetNode<Button>("VBox/BtnQuit");
        _btnRetryBootstrap = GetNode<Button>("ContinueGateDialog/VBox/BtnRetryBootstrap");
        _btnDismissGate = GetNode<Button>("ContinueGateDialog/VBox/BtnDismissGate");
        _continueGateDialog = GetNode<PanelContainer>("ContinueGateDialog");
        _gateMessageLabel = GetNode<Label>("ContinueGateDialog/VBox/GateMessageLabel");
        _bootStatusLabel = GetNode<Label>("BootStatusPanel/VBox/BootStatusLabel");
        _exportStatusLabel = GetNode<Label>("BootStatusPanel/VBox/ExportStatusLabel");
        SetupLocalization();
        RefreshBootstrapStatus();
        _continueGateDialog.Visible = false;

        _btnPlay.Pressed += OnPlayPressed;
        _btnPlay.GuiInput += OnPlayGuiInput;
        _btnContinue.Pressed += OnContinuePressed;
        _btnSettings.Pressed += OnSettingsPressed;
        _btnQuit.Pressed += OnQuitPressed;
        _btnRetryBootstrap.Pressed += OnRetryBootstrapPressed;
        _btnDismissGate.Pressed += OnDismissGatePressed;
    }

    public override void _Process(double delta)
    {
        _ = delta;
        var locale = NormalizeLocale(TranslationServer.GetLocale());
        if (string.Equals(locale, _currentLocale, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _currentLocale = locale;
        _i18n?.Call("switch_locale", _currentLocale);
        ApplyLocalizedStaticTexts();
        UpdateBootStatus(_bootstrapReady, _bootstrapReady, _bootstrapNotReadyReason);
    }

    private void OnPlayGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.Pressed)
        {
            GD.Print($"[MainMenu] BtnPlay gui_input pressed: button={mouse.ButtonIndex}, position={mouse.Position}");
        }
    }

    public void ShowMenu() => Visible = true;
    public void HideMenu() => Visible = false;

    private void Publish(string type, string source, string dataJson = "")
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        var payload = string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson;
        var bus = GetNodeOrNull<EventBusAdapter>("/root/EventBus");
        bus?.PublishSimple(type, source, payload);
    }

    private void OnPlayPressed()
    {
        if (!_bootstrapReady)
        {
            Publish("ui.menu.start_degraded", "ui", $"{{\"reason\":\"{_bootstrapNotReadyReason}\"}}");
        }

        Publish("ui.menu.start", "ui");
    }

    private void OnContinuePressed()
    {
        if (!_bootstrapReady)
        {
            ShowContinueGate(BuildBootstrapNotReadyMessage());
            Publish("ui.menu.continue_blocked", "ui", $"{{\"reason\":\"{_bootstrapNotReadyReason}\"}}");
            return;
        }

        var (canContinue, continueReason) = HasContinueSnapshot();
        if (!canContinue)
        {
            ShowContinueGate(TranslateKey("menu.continue.missing_state"));
            Publish("ui.menu.continue_blocked", "ui", $"{{\"reason\":\"{continueReason}\"}}");
            return;
        }

        Publish("ui.menu.continue", "ui");
        HideMenu();
    }

    private void OnSettingsPressed()
    {
        Publish("ui.menu.settings", "ui");
    }

    private void OnQuitPressed()
    {
        Publish("ui.menu.quit", "ui");
        GetTree().Quit();
    }

    private void OnRetryBootstrapPressed()
    {
        RefreshBootstrapStatus();
        if (_bootstrapReady)
        {
            _continueGateDialog.Visible = false;
        }
        else
        {
            ShowContinueGate(BuildBootstrapNotReadyMessage());
        }

        Publish("ui.menu.bootstrap_retry", "ui");
    }

    private void OnDismissGatePressed()
    {
        _continueGateDialog.Visible = false;
    }

    private void ShowContinueGate(string reason)
    {
        _gateMessageLabel.Text = string.IsNullOrWhiteSpace(reason)
            ? TranslateKey("menu.continue.unavailable")
            : reason;
        _continueGateDialog.Visible = true;
    }

    private void UpdateBootStatus(bool isReady, bool isExportReady, string reason)
    {
        _bootStatusLabel.Text = isReady ? TranslateKey("menu.boot.ready") : TranslateKey("menu.boot.not_ready");
        _exportStatusLabel.Text = isExportReady ? TranslateKey("menu.export.ready") : TranslateKey("menu.export.pending");
        if (!isReady && !string.IsNullOrWhiteSpace(reason))
        {
            _bootStatusLabel.Text = $"{TranslateKey("menu.boot.not_ready")} ({reason})";
        }
    }

    private void RefreshBootstrapStatus()
    {
        var (isReady, isExportReady, reason) = EvaluateBootstrapStatus();
        _bootstrapReady = isReady;
        _bootstrapNotReadyReason = reason;
        UpdateBootStatus(isReady, isExportReady, reason);
    }

    private static (bool isReady, bool isExportReady, string reason) EvaluateBootstrapStatus()
    {
        var overrideValue = (global::System.Environment.GetEnvironmentVariable(BootstrapReadyOverrideEnv) ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
        if (overrideValue == "ready")
        {
            return (true, true, string.Empty);
        }

        if (overrideValue == "not-ready")
        {
            return (false, false, "bootstrap_override_not_ready");
        }

        var versionInfo = Engine.GetVersionInfo();
        var major = ReadVersionPart(versionInfo, "major");
        var minor = ReadVersionPart(versionInfo, "minor");
        if (major != 4 || minor != 5)
        {
            return (false, false, "unsupported_engine_version");
        }

        var hasDotNetSdk = !string.IsNullOrWhiteSpace(global::System.Environment.GetEnvironmentVariable("DOTNET_ROOT")) ||
                           !string.IsNullOrWhiteSpace(global::System.Environment.GetEnvironmentVariable("DOTNET_SDK_VERSION"));
        if (!hasDotNetSdk)
        {
            return (false, false, "missing_dotnet_sdk");
        }

        return (true, true, string.Empty);
    }

    private static int ReadVersionPart(global::Godot.Collections.Dictionary versionInfo, string key)
    {
        if (!versionInfo.ContainsKey(key))
        {
            return -1;
        }

        var value = versionInfo[key];
        if (value.VariantType == Variant.Type.Int)
        {
            return value.AsInt32();
        }

        if (value.VariantType == Variant.Type.Float)
        {
            return (int)value.AsDouble();
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : -1;
    }

    private string BuildBootstrapNotReadyMessage()
    {
        return string.IsNullOrWhiteSpace(_bootstrapNotReadyReason)
            ? TranslateKey("menu.startup.not_ready")
            : $"{TranslateKey("menu.startup.not_ready")}: {_bootstrapNotReadyReason}";
    }

    private (bool canContinue, string reason) HasContinueSnapshot()
    {
        var runtimeFolder = ProjectSettings.GlobalizePath("user://");
        if (string.IsNullOrWhiteSpace(runtimeFolder))
        {
            return (false, "missing_runtime_folder");
        }

        var continuePath = System.IO.Path.Combine(runtimeFolder, "continue_state.json");
        if (!System.IO.File.Exists(continuePath))
        {
            return (false, "missing_continue_state");
        }

        try
        {
            var content = System.IO.File.ReadAllText(continuePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return (false, "empty_continue_state");
            }

            using var parsed = JsonDocument.Parse(content, EventJsonOptions);
            return parsed.RootElement.ValueKind == JsonValueKind.Object
                ? (true, string.Empty)
                : (false, "invalid_continue_state_root");
        }
        catch
        {
            return (false, "invalid_continue_state");
        }
    }

    private void SetupLocalization()
    {
        if (_localizationScript == null)
        {
            return;
        }

        _i18n = (GodotObject)_localizationScript.Call("new");
        _i18n?.Call("configure_locale_resource", "en-US", "res://Game.Godot/Localization/en-US.json");
        _i18n?.Call("configure_locale_resource", "zh-CN", "res://Game.Godot/Localization/zh-CN.json");
        _currentLocale = NormalizeLocale(TranslationServer.GetLocale());
        _i18n?.Call("switch_locale", _currentLocale);
        ApplyLocalizedStaticTexts();
    }

    private void ApplyLocalizedStaticTexts()
    {
        _btnPlay.Text = TranslateKey("menu.play");
        _btnContinue.Text = TranslateKey("menu.continue");
        _btnSettings.Text = TranslateKey("menu.settings");
        _btnQuit.Text = TranslateKey("menu.quit");
        _btnRetryBootstrap.Text = TranslateKey("menu.retry_bootstrap");
        _btnDismissGate.Text = TranslateKey("menu.dismiss");
    }

    private string TranslateKey(string key)
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

        if (normalized == "en" || normalized.StartsWith("en"))
        {
            return "en-US";
        }

        return "en-US";
    }
}

