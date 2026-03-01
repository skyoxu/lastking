namespace Game.Core.Contracts.Lastking;

/// <summary>
/// Domain event: core.lastking.ui_feedback.raised.
/// Emitted when gameplay submits a user-facing feedback message.
/// </summary>
/// <remarks>
/// ADR refs: ADR-0004, ADR-0020, ADR-0010.
/// Overlay refs:
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Contracts-T2.md
/// - docs/architecture/overlays/PRD-lastking-T2/08/08-Feature-Slice-T2-Core-Loop.md
/// </remarks>
public sealed record UiFeedbackRaised(
    string RunId,
    string Code,
    string MessageKey,
    string Severity,
    string Details,
    System.DateTimeOffset RaisedAt
)
{
    public const string EventType = EventTypes.LastkingUiFeedbackRaised;
}
