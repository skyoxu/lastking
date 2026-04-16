namespace Game.Core.Services;

public sealed record RuntimeCountdownAdvanceResult(
    double GameplaySecondsApplied,
    IReadOnlyList<string> CompletedTimerIds);

public sealed record RuntimeCountdownTimer(
    string TimerId,
    double RemainingSeconds);

public sealed class RuntimeCountdownScheduler
{
    private readonly List<RuntimeCountdownTimer> timers = [];

    public IReadOnlyList<RuntimeCountdownTimer> PendingTimers => timers;

    public void Enqueue(string timerId, double remainingSeconds)
    {
        if (string.IsNullOrWhiteSpace(timerId))
        {
            throw new ArgumentException("Timer id is required.", nameof(timerId));
        }

        timers.Add(new RuntimeCountdownTimer(timerId, Math.Max(0d, remainingSeconds)));
    }

    public double GetRemainingSeconds(string timerId)
    {
        var timer = timers.FirstOrDefault(item => string.Equals(item.TimerId, timerId, StringComparison.Ordinal));
        return timer?.RemainingSeconds ?? 0d;
    }

    public RuntimeCountdownAdvanceResult Advance(double wallClockSeconds, RuntimeSpeedState speedState)
    {
        if (wallClockSeconds <= 0d || speedState.EffectiveScalePercent <= 0)
        {
            return new RuntimeCountdownAdvanceResult(0d, []);
        }

        var gameplaySeconds = wallClockSeconds * speedState.EffectiveScalePercent / 100d;
        var completed = new List<string>();
        for (var index = 0; index < timers.Count; index++)
        {
            var timer = timers[index];
            var remaining = Math.Max(0d, timer.RemainingSeconds - gameplaySeconds);
            timers[index] = timer with { RemainingSeconds = remaining };
        }

        for (var index = 0; index < timers.Count;)
        {
            if (timers[index].RemainingSeconds <= 0d)
            {
                completed.Add(timers[index].TimerId);
                timers.RemoveAt(index);
                continue;
            }

            index += 1;
        }

        return new RuntimeCountdownAdvanceResult(gameplaySeconds, completed);
    }
}
