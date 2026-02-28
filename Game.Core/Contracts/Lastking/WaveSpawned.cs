namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.wave.spawned.
/// Emitted when a wave spawn batch is committed for a lane.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0031, ADR-0032.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record WaveSpawned(
    string RunId,
    int DayNumber,
    int NightNumber,
    string LaneId,
    int SpawnCount,
    int WaveBudget,
    System.DateTimeOffset SpawnedAt
)
{
    public const string EventType = EventTypes.LastkingWaveSpawned;
}
