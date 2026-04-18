using Godot;

namespace Game.Godot.Scripts.Audio;

public partial class AudioManager : Node
{
    private AudioStreamPlayer _musicPlayer = default!;
    private AudioStreamPlayer _sfxPlayer = default!;

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
        _musicPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(volume, 0f, 1f));
    }

    public void SetSfxVolume(float volume)
    {
        _sfxPlayer.VolumeDb = Mathf.LinearToDb(Mathf.Clamp(volume, 0f, 1f));
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

