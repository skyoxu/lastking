namespace Game.Core.Services;

public sealed record UIFeedbackEvent(
    string Category,
    string ReasonCode,
    string MessageKey,
    string Severity,
    string Details,
    int RepeatCount);

public sealed class UIFeedbackPipeline
{
    private readonly List<UIFeedbackEvent> events = [];

    public IReadOnlyList<UIFeedbackEvent> Events => events;

    public void ReportInvalidPlacement(string reasonCode)
    {
        AppendOrIncrement(
            category: "invalid_placement",
            reasonCode: reasonCode,
            messageKey: $"ui.invalid_action.{reasonCode}",
            severity: "warning",
            details: $"placement_rejected={reasonCode}");
    }

    public void ReportBlockedAction(string reasonCode, string blocker)
    {
        AppendOrIncrement(
            category: "blocked_action",
            reasonCode: reasonCode,
            messageKey: $"ui.blocked_action.{reasonCode}",
            severity: "warning",
            details: $"blocked_by={blocker};reason={reasonCode}");
    }

    public void ReportLoadFailure(string reasonCode, string slotId)
    {
        AppendOrIncrement(
            category: "load_failure",
            reasonCode: reasonCode,
            messageKey: $"ui.load_failure.{reasonCode}",
            severity: "error",
            details: $"slot={slotId};reason={reasonCode}");
    }

    public void ReportMigrationFailure(string reasonCode, string slotId)
    {
        AppendOrIncrement(
            category: "migration_failure",
            reasonCode: reasonCode,
            messageKey: $"ui.migration_failure.{reasonCode}",
            severity: "error",
            details: $"slot={slotId};reason={reasonCode}");
    }

    private void AppendOrIncrement(
        string category,
        string reasonCode,
        string messageKey,
        string severity,
        string details)
    {
        for (var index = 0; index < events.Count; index++)
        {
            var current = events[index];
            if (!string.Equals(current.Category, category, StringComparison.Ordinal) ||
                !string.Equals(current.ReasonCode, reasonCode, StringComparison.Ordinal))
            {
                continue;
            }

            events[index] = current with
            {
                RepeatCount = current.RepeatCount + 1,
                Details = string.IsNullOrWhiteSpace(details) ? current.Details : details,
            };
            return;
        }

        events.Add(new UIFeedbackEvent(
            Category: category,
            ReasonCode: reasonCode,
            MessageKey: messageKey,
            Severity: severity,
            Details: details,
            RepeatCount: 1));
    }
}
