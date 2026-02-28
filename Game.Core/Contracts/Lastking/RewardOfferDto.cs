namespace Game.Core.Contracts.Lastking;

/// <summary>
/// DTO for three-option reward payload shown after a qualified night.
/// </summary>
/// <remarks>
/// Overlay ref: docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// </remarks>
public sealed record RewardOfferDto(
    int DayNumber,
    bool IsEliteNight,
    bool IsBossNight,
    string OptionA,
    string OptionB,
    string OptionC
);
