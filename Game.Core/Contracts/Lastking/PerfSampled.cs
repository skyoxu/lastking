namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.perf.sampled.
/// Emitted when performance sampling window is completed.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0015.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record PerfSampled(
    string RunId,
    string SceneId,
    int AverageFps,
    int Low1PercentFps,
    int SampleCount,
    System.DateTimeOffset SampledAt
)
{
    public const string EventType = EventTypes.LastkingPerfSampled;
}
