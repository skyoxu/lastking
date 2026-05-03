using Godot;

namespace Game.Godot.Scripts.Audio;

public partial class AudioManager : Node
{
    private AudioStreamPlayer? _musicPlayer;
    private AudioStreamPlayer? _sfxPlayer;

    private const string ConfigPath = "user://settings.cfg";
    private const string ConfigSection = "settings";
    private const string MusicVolumeKey = "music_volume";
    private const string SfxVolumeKey = "sfx_volume";
    private const string LegacyMusicVolumeDbKey = "music_volume_db";
    private const string LegacySfxVolumeDbKey = "sfx_volume_db";

    public override void _Ready()
    {
        _musicPlayer = GetNode<AudioStreamPlayer>("MusicPlayer");
        _sfxPlayer = GetNode<AudioStreamPlayer>("SfxPlayer");
        var (musicVolume, sfxVolume) = LoadSettings();
        SetMusicVolume(musicVolume);
        SetSfxVolume(sfxVolume);
    }

    public void SetMusicVolume(float volume)
    {
        var db = Mathf.LinearToDb(Mathf.Clamp(volume, 0f, 1f));
        if (_musicPlayer != null)
        {
            _musicPlayer.VolumeDb = db;
            return;
        }

        var bus = AudioServer.GetBusIndex("Music");
        if (bus < 0) bus = AudioServer.GetBusIndex("Master");
        if (bus >= 0) AudioServer.SetBusVolumeDb(bus, db);
    }

    public void SetSfxVolume(float volume)
    {
        var db = Mathf.LinearToDb(Mathf.Clamp(volume, 0f, 1f));
        if (_sfxPlayer != null)
        {
            _sfxPlayer.VolumeDb = db;
            return;
        }

        var bus = AudioServer.GetBusIndex("SFX");
        if (bus < 0) bus = AudioServer.GetBusIndex("Master");
        if (bus >= 0) AudioServer.SetBusVolumeDb(bus, db);
    }

    private static (float musicVolume, float sfxVolume) LoadSettings()
    {
        var cfg = new ConfigFile();
        var err = cfg.Load(ConfigPath);
        if (err != Error.Ok)
        {
            return (0.5f, 0.5f);
        }

        var musicVolume = GetFloat(cfg, MusicVolumeKey, GetFloat(cfg, LegacyMusicVolumeDbKey, 0.5f));
        var sfxVolume = GetFloat(cfg, SfxVolumeKey, GetFloat(cfg, LegacySfxVolumeDbKey, musicVolume));
        return (musicVolume, sfxVolume);
    }

    private static float GetFloat(ConfigFile cfg, string key, float fallback)
    {
        Variant v = cfg.GetValue(ConfigSection, key, fallback);
        return v.VariantType == Variant.Type.Nil ? fallback : (float)v.AsDouble();
    }
}

