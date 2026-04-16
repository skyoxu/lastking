namespace Game.Core.Services;

public sealed record RuntimeSpeedInput(
    RuntimeSpeedMode Mode,
    string Source,
    long EffectiveTick);

public sealed record RuntimeSpeedReplayResult(
    IReadOnlyList<string> TimelineAnchors,
    IReadOnlyList<string> CompletedCountdownOrder,
    IReadOnlyList<string> ProgressMilestones,
    RuntimeSpeedState FinalState);

public static class RuntimeSpeedReplay
{
    public static RuntimeSpeedReplayResult Run(string runId, IReadOnlyList<RuntimeSpeedInput> inputs)
    {
        var controller = new RuntimeSpeedController(runId);
        var scheduler = new RuntimeCountdownScheduler();
        scheduler.Enqueue("alpha", 1.5d);
        scheduler.Enqueue("beta", 3.5d);

        var completedCountdownOrder = new List<string>();
        var progressMilestones = new List<string>();
        var gameplayProgress = 0d;

        foreach (var input in inputs)
        {
            switch (input.Mode)
            {
                case RuntimeSpeedMode.Pause:
                    controller.Pause(input.Source, input.EffectiveTick);
                    break;
                case RuntimeSpeedMode.TwoX:
                    controller.SetTwoX(input.Source, input.EffectiveTick);
                    break;
                default:
                    controller.SetOneX(input.Source, input.EffectiveTick);
                    break;
            }

            var gameplayDelta = controller.AdvanceGameplayProgress(1d);
            gameplayProgress += gameplayDelta;
            progressMilestones.Add($"{input.EffectiveTick}:{gameplayProgress:0.###}");

            var countdownAdvance = scheduler.Advance(1d, controller.Current);
            completedCountdownOrder.AddRange(countdownAdvance.CompletedTimerIds);
        }

        var anchors = controller.Timeline
            .Select(entry => $"{entry.EffectiveTick}:{entry.Source}:{entry.After.EffectiveScalePercent}")
            .ToArray();
        return new RuntimeSpeedReplayResult(
            anchors,
            completedCountdownOrder.ToArray(),
            progressMilestones.ToArray(),
            controller.Current);
    }
}
