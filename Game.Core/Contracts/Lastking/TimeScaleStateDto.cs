namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Snapshot DTO for runtime speed state.
/// </summary>
public sealed record TimeScaleStateDto(
    string RunId,
    int CurrentScalePercent,
    bool IsPaused,
    System.DateTimeOffset UpdatedAt
);
