namespace Game.Core.Services;

using Game.Core.Contracts;

public sealed record RuntimeSpeedTimelineGateResult(
    bool Accepted,
    string Reason);

public static class RuntimeSpeedTimelineGate
{
    public static RuntimeSpeedTimelineGateResult Validate(IReadOnlyList<RuntimeSpeedTimelineEntry> timeline)
    {
        if (timeline.Count == 0)
        {
            return new RuntimeSpeedTimelineGateResult(Accepted: false, Reason: "missing_evidence");
        }

        var previousTick = long.MinValue;
        foreach (var entry in timeline)
        {
            if (string.IsNullOrWhiteSpace(entry.Source) || string.IsNullOrWhiteSpace(entry.After.Source))
            {
                return new RuntimeSpeedTimelineGateResult(Accepted: false, Reason: "invalid_source");
            }

            if (!string.Equals(entry.EventType, EventTypes.LastkingTimeScaleChanged, StringComparison.Ordinal))
            {
                return new RuntimeSpeedTimelineGateResult(Accepted: false, Reason: "invalid_event_type");
            }

            if (!RuntimeSpeedController.AllowedScalePercents.Contains(entry.After.EffectiveScalePercent))
            {
                return new RuntimeSpeedTimelineGateResult(Accepted: false, Reason: "invalid_scale");
            }

            if (entry.EffectiveTick != entry.After.EffectiveTick ||
                !string.Equals(entry.Source, entry.After.Source, StringComparison.Ordinal) ||
                !string.Equals(entry.RunId, entry.After.RunId, StringComparison.Ordinal) ||
                !string.Equals(entry.RunId, entry.Before.RunId, StringComparison.Ordinal))
            {
                return new RuntimeSpeedTimelineGateResult(Accepted: false, Reason: "state_mismatch");
            }

            if (entry.EffectiveTick < previousTick)
            {
                return new RuntimeSpeedTimelineGateResult(Accepted: false, Reason: "non_monotonic_tick");
            }

            previousTick = entry.EffectiveTick;
        }

        return new RuntimeSpeedTimelineGateResult(Accepted: true, Reason: "accepted");
    }
}
