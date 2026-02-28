namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.reward.offered.
/// Emitted when a nightly reward choice is presented.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0033.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record RewardOffered(
    string RunId,
    int DayNumber,
    bool IsEliteNight,
    bool IsBossNight,
    string OptionA,
    string OptionB,
    string OptionC,
    System.DateTimeOffset OfferedAt
)
{
    public const string EventType = EventTypes.LastkingRewardOffered;
}
