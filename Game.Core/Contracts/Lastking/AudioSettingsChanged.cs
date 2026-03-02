namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.audio_settings.changed.
/// Emitted when launch-scoped music or sfx settings are changed.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0010.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record AudioSettingsChanged(
    string RunId,
    int MusicVolumePercent,
    int SfxVolumePercent,
    System.DateTimeOffset ChangedAt
)
{
    public const string EventType = EventTypes.LastkingAudioSettingsChanged;
}
