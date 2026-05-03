using Godot;
using System;
using System.Linq;
using System.Text.Json;
using Game.Godot.Adapters;
using Game.Godot.Scripts.Audio;

namespace Game.Godot.Scripts.UI;

public partial class SettingsPanel : Control
{
    private static readonly string[] SupportedLocales = { "en-US", "zh-CN" };
    private static readonly string[] LegacyLocales = { "en", "zh" };
    private static readonly JsonDocumentOptions SettingsJsonOptions = new() { MaxDepth = 32 };

    private HSlider _musicVolume = default!;
    private HSlider _sfxVolume = default!;
    private OptionButton _graphics = default!;
    private OptionButton _language = default!;
    private Button _save = default!;
    private Button _load = default!;
    private Button _close = default!;
    private Label _autosavePathLabel = default!;
    private Label _saveStatusLabel = default!;
    private Label _summaryLocaleLabel = default!;
    private Label _summaryAchievementsLabel = default!;
    private Label _summaryPerfLabel = default!;
    private Label _summaryUpdatedLabel = default!;
    private Label _savePanelTitleLabel = default!;
    private Label _runSummaryTitleLabel = default!;
    private Label _volLabel = default!;
    private Label _sfxLabel = default!;
    private Label _graphicsLabel = default!;
    private Label _langLabel = default!;
    private GodotObject? _i18n;
    private string _localizedLocale = "en-US";

    private const string UserId = "default";
    private const string ConfigPath = "user://settings.cfg";
    private const string ConfigSection = "settings";
    private const string MusicVolumeKey = "music_volume";
    private const string SfxVolumeKey = "sfx_volume";
    private const string LegacyMusicVolumeDbKey = "music_volume_db";
    private const string LegacySfxVolumeDbKey = "sfx_volume_db";
    private const string AutoSaveSlotPath = "user://autosave.save";
    private const string AchievementSnapshotPath = "user://logs/tmp/task27-achievements.json";
    private const string PerfSnapshotPath = "user://logs/perf/perf.json";

    public override void _Ready()
    {
        _musicVolume = GetNode<HSlider>("VBox/VolRow/VolSlider");
        _sfxVolume = GetNode<HSlider>("VBox/SfxRow/SfxSlider");
        _graphics = GetNode<OptionButton>("VBox/GraphicsRow/GraphicsOpt");
        _language = GetNode<OptionButton>("VBox/LangRow/LangOpt");
        _save = GetNode<Button>("VBox/Buttons/SaveBtn");
        _load = GetNode<Button>("VBox/Buttons/LoadBtn");
        _close = GetNode<Button>("VBox/Buttons/CloseBtn");
        _autosavePathLabel = GetNode<Label>("VBox/SavePanel/VBox/AutosavePathLabel");
        _saveStatusLabel = GetNode<Label>("VBox/SavePanel/VBox/SaveStatusLabel");
        _summaryLocaleLabel = GetNode<Label>("VBox/RunSummaryPanel/VBox/SummaryLocaleLabel");
        _summaryAchievementsLabel = GetNode<Label>("VBox/RunSummaryPanel/VBox/SummaryAchievementsLabel");
        _summaryPerfLabel = GetNode<Label>("VBox/RunSummaryPanel/VBox/SummaryPerfLabel");
        _summaryUpdatedLabel = GetNode<Label>("VBox/RunSummaryPanel/VBox/SummaryUpdatedLabel");
        _savePanelTitleLabel = GetNode<Label>("VBox/SavePanel/VBox/TitleLabel");
        _runSummaryTitleLabel = GetNode<Label>("VBox/RunSummaryPanel/VBox/TitleLabel");
        _volLabel = GetNode<Label>("VBox/VolRow/VolLabel");
        _sfxLabel = GetNode<Label>("VBox/SfxRow/SfxLabel");
        _graphicsLabel = GetNode<Label>("VBox/GraphicsRow/GraphicsLabel");
        _langLabel = GetNode<Label>("VBox/LangRow/LangLabel");

        SetupLocalization();
        _save.Pressed += OnSave;
        _load.Pressed += OnLoad;
        _close.Pressed += () => Visible = false;

        if (_graphics.ItemCount == 0)
        {
            _graphics.AddItem("low");
            _graphics.AddItem("medium");
            _graphics.AddItem("high");
            _graphics.Selected = 1;
        }
        if (_language.ItemCount == 0)
        {
            _language.AddItem("en");
            _language.AddItem("zh");
            _language.Selected = 0;
        }

        // Realtime apply handlers
        _musicVolume.ValueChanged += OnMusicVolumeChanged;
        _sfxVolume.ValueChanged += OnSfxVolumeChanged;
        _graphics.ItemSelected += OnGraphicsChanged;
        _language.ItemSelected += OnLanguageChanged;

        _autosavePathLabel.Text = $"{T("settings.autosave_slot")}: {AutoSaveSlotPath}";
        _saveStatusLabel.Text = $"{T("settings.save_status")}: {T("settings.idle")}";
        RefreshMetaSummary("panel_ready");
        ApplyLocalizedStaticTexts();
        OnLoad();

        Visible = false;
    }

    public override void _Process(double delta)
    {
        _ = delta;
        var locale = NormalizeLocaleOrDefault(TranslationServer.GetLocale());
        if (string.Equals(locale, _localizedLocale, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _localizedLocale = locale;
        _i18n?.Call("switch_locale", _localizedLocale);
        ApplyLocalizedStaticTexts();
        RefreshMetaSummary("locale_changed");
    }

    private SqliteDataStore? Db() => GetNodeOrNull<SqliteDataStore>("/root/SqlDb");

    private void SaveToConfig(float musicVolume, float sfxVolume, string gfx, string lang)
    {
        lang = NormalizeLocaleOrDefault(lang);
        var cfg = new ConfigFile();
        // Load existing to preserve unrelated keys
        cfg.Load(ConfigPath);
        cfg.SetValue(ConfigSection, MusicVolumeKey, musicVolume);
        cfg.SetValue(ConfigSection, SfxVolumeKey, sfxVolume);
        cfg.SetValue(ConfigSection, LegacyMusicVolumeDbKey, musicVolume);
        cfg.SetValue(ConfigSection, LegacySfxVolumeDbKey, sfxVolume);
        cfg.SetValue(ConfigSection, nameof(gfx), gfx ?? "medium");
        cfg.SetValue(ConfigSection, nameof(lang), lang ?? "en-US");
        var err = cfg.Save(ConfigPath);
        if (err != Error.Ok)
        {
            GD.PushWarning($"SettingsPanel: failed to save ConfigFile: {err}");
        }
    }

    private bool TryLoadFromConfig(out float musicVolume, out float sfxVolume, out string gfx, out string lang)
    {
        musicVolume = 0.5f;
        sfxVolume = 0.5f;
        gfx = "medium";
        lang = "en-US";
        var cfg = new ConfigFile();
        var err = cfg.Load(ConfigPath);
        if (err != Error.Ok)
        {
            return false;
        }
        try
        {
            Variant mv = cfg.GetValue(
                ConfigSection,
                MusicVolumeKey,
                cfg.GetValue(
                    ConfigSection,
                    LegacyMusicVolumeDbKey,
                    cfg.GetValue(ConfigSection, nameof(musicVolume), 0.5f)));
            Variant sv = cfg.GetValue(
                ConfigSection,
                SfxVolumeKey,
                cfg.GetValue(
                    ConfigSection,
                    LegacySfxVolumeDbKey,
                    cfg.GetValue(ConfigSection, "audio_volume", 0.5f)));
            Variant g = cfg.GetValue(ConfigSection, nameof(gfx), "medium");
            Variant l = cfg.GetValue(ConfigSection, nameof(lang), "en-US");
            musicVolume = mv.VariantType == Variant.Type.Nil ? 0.5f : (float)mv.AsDouble();
            sfxVolume = sv.VariantType == Variant.Type.Nil ? 0.5f : (float)sv.AsDouble();
            gfx = g.VariantType == Variant.Type.Nil ? "medium" : g.AsString();
            lang = NormalizeLocaleOrDefault(l.VariantType == Variant.Type.Nil ? "en-US" : l.AsString());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void MigrateFromDbIfConfigMissing()
    {
        // If config already exists, do nothing
        var cfgProbe = new ConfigFile();
        if (cfgProbe.Load(ConfigPath) == Error.Ok)
            return;

        // Attempt read from DB once and save to config
        var db = Db();
        if (db == null)
            return;
        var rows = db.Query("SELECT audio_volume, graphics_quality, language FROM settings WHERE user_id=@0;", UserId);
        if (rows.Count == 0) return;
        var r = rows[0];
        float musicVolume = 0.5f;
        float sfxVolume = 0.5f;
        string gfx = "medium";
        string lang = "en-US";
        if (r.TryGetValue("audio_volume", out var v) && v != null)
        {
            musicVolume = Convert.ToSingle(v);
            sfxVolume = Convert.ToSingle(v);
        }
        if (r.TryGetValue("graphics_quality", out var g) && g != null)
            gfx = g.ToString() ?? "medium";
        if (r.TryGetValue("language", out var l) && l != null)
            lang = NormalizeLocaleOrDefault(l.ToString() ?? "en-US");
        SaveToConfig(musicVolume, sfxVolume, gfx, lang);
    }

    private void OnSave()
    {
        var musicVolume = Mathf.Clamp((float)_musicVolume.Value, 0, 1);
        var sfxVolume = Mathf.Clamp((float)_sfxVolume.Value, 0, 1);
        var gfx = _graphics.GetItemText(_graphics.Selected);
        var lang = _language.GetItemText(_language.Selected);
        // SSoT to ConfigFile
        SaveToConfig(musicVolume, sfxVolume, gfx, lang);

        // Apply immediately
        ApplyMusicVolume(musicVolume);
        ApplySfxVolume(sfxVolume);
        ApplyLanguage(lang);
        RefreshMetaSummary("saved");
        _saveStatusLabel.Text = $"{T("settings.save_status")}: {T("settings.saved_to")} {AutoSaveSlotPath}";
    }

    private void OnLoad()
    {
        // Prefer ConfigFile; migrate once from DB if missing
        float musicVolume;
        float sfxVolume;
        string gfx;
        string lang;
        if (!TryLoadFromConfig(out musicVolume, out sfxVolume, out gfx, out lang))
        {
            MigrateFromDbIfConfigMissing();
            if (!TryLoadFromConfig(out musicVolume, out sfxVolume, out gfx, out lang))
            {
                RefreshMetaSummary("load_no_config");
                _saveStatusLabel.Text = $"{T("settings.save_status")}: {T("settings.no_config_at")} {AutoSaveSlotPath}";
                return;
            }
        }
        _musicVolume.Value = musicVolume;
        _sfxVolume.Value = sfxVolume;
        ApplyMusicVolume(musicVolume);
        ApplySfxVolume(sfxVolume);
        // graphics selection
        if (!string.IsNullOrEmpty(gfx))
        {
            for (int i = 0; i < _graphics.ItemCount; i++)
            {
                if (_graphics.GetItemText(i).Equals(gfx, StringComparison.OrdinalIgnoreCase))
                { _graphics.Selected = i; break; }
            }
        }
        // language
        if (!string.IsNullOrEmpty(lang))
        {
            var normalized = NormalizeLocaleOrDefault(lang);
            for (int i = 0; i < _language.ItemCount; i++)
            {
                var itemText = _language.GetItemText(i);
                var itemNormalized = NormalizeLocaleOrDefault(itemText);
                if (itemText.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                    || itemNormalized.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                {
                    _language.Selected = i;
                    break;
                }
            }
            ApplyLanguage(normalized);
        }
        RefreshMetaSummary("loaded");
        _saveStatusLabel.Text = $"{T("settings.save_status")}: {T("settings.loaded_from")} {AutoSaveSlotPath}";
    }

    public void ShowPanel()
    {
        Visible = true;
        RefreshMetaSummary("show_panel");
    }

    private void OnMusicVolumeChanged(double value)
    {
        var vol = Mathf.Clamp((float)value, 0, 1);
        ApplyMusicVolume(vol);
        SaveCurrentSelections();
    }

    private void OnSfxVolumeChanged(double value)
    {
        var vol = Mathf.Clamp((float)value, 0, 1);
        ApplySfxVolume(vol);
        SaveCurrentSelections();
    }

    private void OnGraphicsChanged(long index)
    {
        var gfx = _graphics.GetItemText((int)index);
        ApplyGraphicsQuality(gfx);
        SaveCurrentSelections();
    }

    private void OnLanguageChanged(long index)
    {
        var lang = _language.GetItemText((int)index);
        ApplyLanguage(lang);
        SaveCurrentSelections();
    }

    private void SaveCurrentSelections()
    {
        var musicVolume = Mathf.Clamp((float)_musicVolume.Value, 0, 1);
        var sfxVolume = Mathf.Clamp((float)_sfxVolume.Value, 0, 1);
        var gfx = _graphics.GetItemText(_graphics.Selected);
        var lang = _language.GetItemText(_language.Selected);
        SaveToConfig(musicVolume, sfxVolume, gfx, lang);
    }

    private AudioManager? GetAudioManager() => GetNodeOrNull<AudioManager>("/root/Main/AudioManager");

    private static void ApplyBusVolume(string preferredBus, float vol)
    {
        int bus = AudioServer.GetBusIndex(preferredBus);
        if (bus < 0)
        {
            bus = AudioServer.GetBusIndex("Master");
        }
        if (bus >= 0)
        {
            AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(Mathf.Clamp(vol, 0, 1)));
        }
    }

    private void ApplyMusicVolume(float vol)
    {
        var audioManager = GetAudioManager();
        if (audioManager != null)
        {
            audioManager.SetMusicVolume(Mathf.Clamp(vol, 0, 1));
            return;
        }
        ApplyBusVolume("Music", vol);
    }

    private void ApplySfxVolume(float vol)
    {
        var audioManager = GetAudioManager();
        if (audioManager != null)
        {
            audioManager.SetSfxVolume(Mathf.Clamp(vol, 0, 1));
            return;
        }
        ApplyBusVolume("SFX", vol);
    }

    private void ApplyLanguage(string lang)
    {
        var normalized = NormalizeLocaleOrDefault(lang);
        if (!IsSupportedLocale(normalized))
        {
            return;
        }

        TranslationServer.SetLocale(normalized);
        _i18n?.Call("switch_locale", normalized);
        _localizedLocale = normalized;
        ApplyLocalizedStaticTexts();
        _summaryLocaleLabel.Text = $"{T("settings.locale")}: {normalized}";
    }

    private static bool IsSupportedLocale(string locale)
    {
        for (var i = 0; i < SupportedLocales.Length; i++)
        {
            if (SupportedLocales[i].Equals(locale, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeLocaleOrDefault(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return "en-US";
        }

        if (locale.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return "en-US";
        }

        if (locale.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            return "zh-CN";
        }

        for (var i = 0; i < SupportedLocales.Length; i++)
        {
            if (SupportedLocales[i].Equals(locale, StringComparison.OrdinalIgnoreCase))
            {
                return SupportedLocales[i];
            }
        }

        for (var i = 0; i < LegacyLocales.Length; i++)
        {
            if (LegacyLocales[i].Equals(locale, StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeLocaleOrDefault(LegacyLocales[i]);
            }
        }

        return locale;
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
        _localizedLocale = NormalizeLocaleOrDefault(TranslationServer.GetLocale());
        _i18n?.Call("switch_locale", _localizedLocale);
    }

    private string T(string key)
    {
        if (_i18n == null || string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        var v = _i18n.Call("translate", key).AsString();
        return string.IsNullOrWhiteSpace(v) ? key : v;
    }

    private void ApplyLocalizedStaticTexts()
    {
        _savePanelTitleLabel.Text = T("settings.save_panel");
        _runSummaryTitleLabel.Text = T("settings.run_summary");
        _volLabel.Text = T("settings.music_volume");
        _sfxLabel.Text = T("settings.sfx_volume");
        _graphicsLabel.Text = T("settings.graphics");
        _langLabel.Text = T("settings.language");
        _save.Text = T("settings.save");
        _load.Text = T("settings.load");
        _close.Text = T("settings.close");
    }

    private void ApplyGraphicsQuality(string quality)
    {
        // Map: low -> no vsync, no MSAA; medium -> vsync on, 2x; high -> vsync on, 4x/8x
        var q = (quality ?? "medium").ToLowerInvariant();
        try
        {
            if (q == "low")
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            else
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
        }
        catch { /* not critical */ }

        var vp = GetViewport();
        if (vp != null)
        {
            int msaa = 0; // disabled
            if (q == "medium") msaa = 1; // 2x
            else if (q == "high") msaa = 2; // 4x (use 8x if needed: 3)
            // Set via dynamic property names to avoid API differences
            try { vp.Set("msaa_2d", msaa); } catch { }
            try { vp.Set("msaa_3d", msaa); } catch { }
        }
    }

    private void RefreshMetaSummary(string source)
    {
        _summaryLocaleLabel.Text = $"{T("settings.locale")}: {TranslationServer.GetLocale()}";
        _summaryAchievementsLabel.Text = $"{T("settings.achievements")}: {ReadAchievementsSummary()}";
        _summaryPerfLabel.Text = $"{T("settings.performance")}: {ReadPerformanceSummary()}";
        _summaryUpdatedLabel.Text = $"{T("settings.summary_updated")}: {source}";
    }

    private static string ReadAchievementsSummary()
    {
        try
        {
            if (!FileAccess.FileExists(AchievementSnapshotPath))
            {
                return "no_snapshot";
            }

            using var file = FileAccess.Open(AchievementSnapshotPath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            using var doc = JsonDocument.Parse(json, SettingsJsonOptions);
            if (doc.RootElement.TryGetProperty("unlock_ids", out var unlockIds) && unlockIds.ValueKind == JsonValueKind.Array)
            {
                var ids = unlockIds.EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToArray();
                if (ids.Length > 0)
                {
                    return $"unlocked={ids.Length}";
                }
            }
            return "snapshot_loaded";
        }
        catch
        {
            return "snapshot_invalid";
        }
    }

    private static string ReadPerformanceSummary()
    {
        try
        {
            if (!FileAccess.FileExists(PerfSnapshotPath))
            {
                return "no_perf_log";
            }

            using var file = FileAccess.Open(PerfSnapshotPath, FileAccess.ModeFlags.Read);
            var json = file.GetAsText();
            using var doc = JsonDocument.Parse(json, SettingsJsonOptions);
            var root = doc.RootElement;
            if (root.TryGetProperty("p95_ms", out var p95Ms))
            {
                return $"p95_ms={p95Ms.GetDouble():F2}";
            }
            if (root.TryGetProperty("avg_ms", out var avgMs))
            {
                return $"avg_ms={avgMs.GetDouble():F2}";
            }
            return "perf_loaded";
        }
        catch
        {
            return "perf_invalid";
        }
    }
}
