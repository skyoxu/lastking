namespace Game.Core.Contracts.Lastking;

/// <summary>
/// DTO for UI feedback payload routing.
/// </summary>
public sealed record UiFeedbackDto(
    string Code,
    string MessageKey,
    string Severity,
    string Details
);
