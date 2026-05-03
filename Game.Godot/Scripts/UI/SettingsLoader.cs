using Godot;
using System;
using Game.Godot.Adapters;

namespace Game.Godot.Scripts.UI;

public partial class SettingsLoader : Node
{
    private const string UserId = "default";
    private const string ConfigPath = "user://settings.cfg";
    private const string ConfigSection = "settings";
    private const string MusicVolumeKey = "music_volume";
    private const string SfxVolumeKey = "sfx_volume";
    private const string LegacyMusicVolumeDbKey = "music_volume_db";
    private const string LegacySfxVolumeDbKey = "sfx_volume_db";

    public override void _Ready()
    {
        try
        {
            if (TryLoadFromConfig(out var musicVolume, out var sfxVolume, out var gfx, out var lang))
            {
                ApplyMusicVolume(musicVolume);
                ApplySfxVolume(sfxVolume);
                ApplyLanguage(lang);
                ApplyGraphicsQuality(gfx);
                return;
            }

            var db = GetNodeOrNull<SqliteDataStore>("/root/SqlDb");
            if (db == null) return;
            var rows = db.Query("SELECT audio_volume, graphics_quality, language FROM settings WHERE user_id=@0;", UserId);
            if (rows.Count == 0) return;
            var r = rows[0];
            if (r.TryGetValue("audio_volume", out var v) && v != null)
            {
                var vol = (float)Convert.ToSingle(v);
                ApplyMusicVolume(vol);
                ApplySfxVolume(vol);
            }
            if (r.TryGetValue("graphics_quality", out var g) && g != null)
            {
                ApplyGraphicsQuality(g.ToString() ?? "medium");
            }
            if (r.TryGetValue("language", out var l) && l != null)
            {
                ApplyLanguage(l.ToString() ?? "");
            }
        }
        catch { }
    }

    private static bool TryLoadFromConfig(out float musicVolume, out float sfxVolume, out string gfx, out string lang)
    {
        musicVolume = 0.5f;
        sfxVolume = 0.5f;
        gfx = "medium";
        lang = "en-US";
        var cfg = new ConfigFile();
        if (cfg.Load(ConfigPath) != Error.Ok)
        {
            return false;
        }

        Variant mv = cfg.GetValue(
            ConfigSection,
            MusicVolumeKey,
            cfg.GetValue(
                ConfigSection,
                LegacyMusicVolumeDbKey,
                cfg.GetValue(ConfigSection, "music_volume", 0.5f)));
        Variant sv = cfg.GetValue(
            ConfigSection,
            SfxVolumeKey,
            cfg.GetValue(
                ConfigSection,
                LegacySfxVolumeDbKey,
                cfg.GetValue(ConfigSection, "audio_volume", 0.5f)));
        Variant g = cfg.GetValue(ConfigSection, "gfx", "medium");
        Variant l = cfg.GetValue(ConfigSection, "lang", "en-US");
        musicVolume = mv.VariantType == Variant.Type.Nil ? 0.5f : (float)mv.AsDouble();
        sfxVolume = sv.VariantType == Variant.Type.Nil ? 0.5f : (float)sv.AsDouble();
        gfx = g.VariantType == Variant.Type.Nil ? "medium" : g.AsString();
        lang = NormalizeLocaleOrDefault(l.VariantType == Variant.Type.Nil ? "en-US" : l.AsString());
        return true;
    }

    private void ApplyMusicVolume(float vol)
    {
        int bus = AudioServer.GetBusIndex("Music");
        if (bus < 0) bus = AudioServer.GetBusIndex("Master");
        if (bus >= 0) AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(Mathf.Clamp(vol, 0, 1)));
    }

    private void ApplySfxVolume(float vol)
    {
        int bus = AudioServer.GetBusIndex("SFX");
        if (bus < 0) bus = AudioServer.GetBusIndex("Master");
        if (bus >= 0) AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(Mathf.Clamp(vol,0,1)));
    }

    private void ApplyLanguage(string lang)
    {
        if (!string.IsNullOrEmpty(lang)) TranslationServer.SetLocale(NormalizeLocaleOrDefault(lang));
    }

    private void ApplyGraphicsQuality(string q)
    {
        q = (q ?? "medium").ToLowerInvariant();
        try { DisplayServer.WindowSetVsyncMode(q == "low" ? DisplayServer.VSyncMode.Disabled : DisplayServer.VSyncMode.Enabled); } catch { }
        var vp = GetViewport();
        if (vp != null)
        {
            int msaa = q == "low" ? 0 : q == "medium" ? 1 : 2;
            try { vp.Set("msaa_2d", msaa); } catch { }
            try { vp.Set("msaa_3d", msaa); } catch { }
        }
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

        return locale;
    }
}
