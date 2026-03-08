using Game.Core.Contracts.Lastking;

namespace Game.Core.Services;

/// <summary>
/// Result of applying a balance configuration payload.
/// </summary>
public sealed record ConfigLoadResult(
    bool Accepted,
    string Source,
    string SourcePath,
    string ConfigHash,
    BalanceSnapshot Snapshot,
    IReadOnlyList<string> ReasonCodes
)
{
    public ConfigLoaded ToEvent(DateTimeOffset loadedAt)
    {
        return new ConfigLoaded(
            RunId: "local",
            ConfigVersion: "v1",
            ConfigHash: ConfigHash,
            SourcePath: SourcePath,
            LoadedAt: loadedAt);
    }
}
