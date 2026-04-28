using Game.Core.Services;
using Godot;

namespace Game.Godot.Adapters.Config;

public partial class ConfigRuntimeBridge : Node
{
    private readonly ConfigManager manager = new();

    public global::Godot.Collections.Dictionary LoadInitialFromJson(string json, string sourcePath)
    {
        var result = manager.LoadInitialFromJson(json ?? string.Empty, sourcePath ?? "res://Config/unknown.json");
        return ToDictionary(result.Source, result.Accepted, result.ReasonCodes.Count > 0 ? result.ReasonCodes[0] : string.Empty);
    }

    public global::Godot.Collections.Dictionary ReloadFromJson(string json, string sourcePath)
    {
        var result = manager.ReloadFromJson(json ?? string.Empty, sourcePath ?? "res://Config/unknown.json");
        return ToDictionary(result.Source, result.Accepted, result.ReasonCodes.Count > 0 ? result.ReasonCodes[0] : string.Empty);
    }

    public global::Godot.Collections.Dictionary CurrentSnapshot()
    {
        var snapshot = manager.Snapshot;
        return new global::Godot.Collections.Dictionary
        {
            ["day1_budget"] = snapshot.Day1Budget,
            ["daily_growth"] = (double)snapshot.DailyGrowth,
            ["spawn_cadence_seconds"] = snapshot.SpawnCadenceSeconds,
            ["boss_count"] = snapshot.BossCount,
        };
    }

    private static global::Godot.Collections.Dictionary ToDictionary(string source, bool accepted, string reasonCode)
    {
        return new global::Godot.Collections.Dictionary
        {
            ["source"] = source,
            ["accepted"] = accepted,
            ["reason_code"] = reasonCode,
        };
    }
}
