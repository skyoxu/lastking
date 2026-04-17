using Game.Core.Domain;

namespace Game.Core.State;

public record SaveMetadata(DateTime CreatedAt, DateTime UpdatedAt, string Version, string Checksum);

public sealed record DayNightRuntimeSnapshot(
    int Seed,
    int Day,
    DayNightPhase Phase,
    double PhaseElapsedSeconds,
    long Tick,
    int CheckpointCount,
    bool TerminalRaised,
    DayNightPhase TerminalFromPhase
);

public record SaveData(
    string Id,
    GameState State,
    GameConfig Config,
    SaveMetadata Metadata,
    string? Screenshot = null,
    string? Title = null,
    DayNightRuntimeSnapshot? DayNightRuntime = null
);

public record GameStateManagerOptions(
    string StorageKey = "guild-manager-game",
    int MaxSaves = 10,
    TimeSpan AutoSaveInterval = default,
    bool EnableCompression = false,
    EndOfGameHandling EndOfGameHandling = EndOfGameHandling.Pause
)
{
    public static GameStateManagerOptions Default => new(
        StorageKey: "guild-manager-game",
        MaxSaves: 10,
        AutoSaveInterval: TimeSpan.FromSeconds(30),
        EnableCompression: false,
        EndOfGameHandling: EndOfGameHandling.Pause
    );
}
