using Game.Core.Contracts.Interfaces;
using Game.Core.Contracts.Lastking;

namespace Game.Core.Services;

public enum RuntimeSpeedMode
{
    Pause = 0,
    OneX = 1,
    TwoX = 2,
}

public sealed record RuntimeSpeedState(
    string RunId,
    RuntimeSpeedMode Mode,
    int EffectiveScalePercent,
    bool IsPaused,
    long EffectiveTick,
    string Source);

public sealed record RuntimeSpeedTimelineEntry(
    string RunId,
    RuntimeSpeedState Before,
    RuntimeSpeedState After,
    long EffectiveTick,
    string Source,
    string EventType);

public sealed class RuntimeSpeedController : ITimeScaleController
{
    public static IReadOnlyList<int> AllowedScalePercents { get; } = [0, 100, 200];

    private readonly List<RuntimeSpeedTimelineEntry> timeline = [];
    private RuntimeSpeedMode lastNonZeroMode = RuntimeSpeedMode.OneX;

    public RuntimeSpeedController(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("Run id is required.", nameof(runId));
        }

        Current = new RuntimeSpeedState(runId, RuntimeSpeedMode.OneX, 100, IsPaused: false, EffectiveTick: 0, Source: "initial");
        OwnerKey = $"{nameof(RuntimeSpeedController)}:{runId}";
    }

    public RuntimeSpeedState Current { get; private set; }

    public string OwnerKey { get; }

    public IReadOnlyList<RuntimeSpeedTimelineEntry> Timeline => timeline;

    public RuntimeSpeedState Pause(string source, long effectiveTick)
    {
        if (!Current.IsPaused)
        {
            lastNonZeroMode = Current.Mode is RuntimeSpeedMode.TwoX ? RuntimeSpeedMode.TwoX : RuntimeSpeedMode.OneX;
        }

        return Apply(RuntimeSpeedMode.Pause, source, effectiveTick);
    }

    public RuntimeSpeedState Resume(string source, long effectiveTick)
    {
        return Apply(lastNonZeroMode, source, effectiveTick);
    }

    public RuntimeSpeedState SetOneX(string source, long effectiveTick)
    {
        return Apply(RuntimeSpeedMode.OneX, source, effectiveTick);
    }

    public RuntimeSpeedState SetTwoX(string source, long effectiveTick)
    {
        return Apply(RuntimeSpeedMode.TwoX, source, effectiveTick);
    }

    public double AdvanceGameplayProgress(double wallClockSeconds)
    {
        if (wallClockSeconds <= 0d)
        {
            return 0d;
        }

        return wallClockSeconds * Current.EffectiveScalePercent / 100d;
    }

    public TimeScaleStateDto SetScale(string runId, int currentScalePercent, bool isPaused)
    {
        if (!string.Equals(runId, Current.RunId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Run id does not match this controller.", nameof(runId));
        }

        if (isPaused || currentScalePercent <= 0)
        {
            Pause("contract.set_scale", Current.EffectiveTick + 1);
        }
        else if (currentScalePercent >= 200)
        {
            SetTwoX("contract.set_scale", Current.EffectiveTick + 1);
        }
        else
        {
            SetOneX("contract.set_scale", Current.EffectiveTick + 1);
        }

        return new TimeScaleStateDto(Current.RunId, Current.EffectiveScalePercent, Current.IsPaused, DateTimeOffset.UtcNow);
    }

    private RuntimeSpeedState Apply(RuntimeSpeedMode mode, string source, long effectiveTick)
    {
        var before = Current;
        var after = new RuntimeSpeedState(
            before.RunId,
            mode,
            ToScalePercent(mode),
            mode is RuntimeSpeedMode.Pause,
            effectiveTick,
            string.IsNullOrWhiteSpace(source) ? "unknown" : source);

        if (!after.IsPaused)
        {
            lastNonZeroMode = after.Mode;
        }

        Current = after;
        timeline.Add(new RuntimeSpeedTimelineEntry(
            after.RunId,
            before,
            after,
            effectiveTick,
            after.Source,
            TimeScaleChanged.EventType));

        return after;
    }

    private static int ToScalePercent(RuntimeSpeedMode mode)
    {
        return mode switch
        {
            RuntimeSpeedMode.Pause => 0,
            RuntimeSpeedMode.OneX => 100,
            RuntimeSpeedMode.TwoX => 200,
            _ => 100,
        };
    }
}
