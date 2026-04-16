using System;
using Game.Core.Services;
using Godot;
using GArray = Godot.Collections.Array;
using GDictionary = Godot.Collections.Dictionary;

namespace Game.Godot.Scripts.Runtime;

public partial class GameManager : Node
{
    public static GameManager? Instance { get; private set; }

    private RuntimeSpeedController controller = new("godot-runtime");
    private RuntimeCountdownScheduler scheduler = new();
    private double gameplayProgress;
    private long effectiveTick;

    public override void _Ready()
    {
        if (Instance != null && !ReferenceEquals(Instance, this))
        {
            QueueFree();
            return;
        }

        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
        ApplyEngineTimeScale();
    }

    public override void _ExitTree()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public void ResetRuntimeForTest()
    {
        controller = new RuntimeSpeedController("godot-runtime");
        scheduler = new RuntimeCountdownScheduler();
        gameplayProgress = 0d;
        effectiveTick = 0;
        ApplyEngineTimeScale();
    }

    public GDictionary SetPause()
    {
        var state = controller.Pause("ui.pause", NextTick());
        ApplyEngineTimeScale();
        return ToDictionary(state);
    }

    public GDictionary SetOneX()
    {
        var state = controller.SetOneX("ui.1x", NextTick());
        ApplyEngineTimeScale();
        return ToDictionary(state);
    }

    public GDictionary SetTwoX()
    {
        var state = controller.SetTwoX("ui.2x", NextTick());
        ApplyEngineTimeScale();
        return ToDictionary(state);
    }

    public GDictionary TogglePause()
    {
        var state = controller.Current.IsPaused
            ? controller.Resume("ui.resume", NextTick())
            : controller.Pause("ui.pause", NextTick());
        ApplyEngineTimeScale();
        return ToDictionary(state);
    }

    public GDictionary GetSpeedState()
    {
        return ToDictionary(controller.Current);
    }

    public double GetGameplayProgress()
    {
        return gameplayProgress;
    }

    public void EnqueueCountdown(string timerId, double remainingSeconds)
    {
        scheduler.Enqueue(timerId, remainingSeconds);
    }

    public double GetCountdownRemaining(string timerId)
    {
        return scheduler.GetRemainingSeconds(timerId);
    }

    public GArray GetRuntimeSpeedTimeline()
    {
        var result = new GArray();
        foreach (var entry in controller.Timeline)
        {
            result.Add(new GDictionary
            {
                ["event_type"] = entry.EventType,
                ["before_scale_percent"] = entry.Before.EffectiveScalePercent,
                ["after_scale_percent"] = entry.After.EffectiveScalePercent,
                ["effective_tick"] = entry.EffectiveTick,
                ["source"] = entry.Source,
            });
        }

        return result;
    }

    public bool ValidateRuntimeSpeedTimelineGate()
    {
        return RuntimeSpeedTimelineGate.Validate(controller.Timeline).Accepted;
    }

    public GDictionary SimulateRuntimeStep(double wallClockSeconds)
    {
        gameplayProgress += controller.AdvanceGameplayProgress(wallClockSeconds);
        var countdown = scheduler.Advance(wallClockSeconds, controller.Current);
        return new GDictionary
        {
            ["progress"] = gameplayProgress,
            ["completed"] = ToArray(countdown.CompletedTimerIds),
        };
    }

    private long NextTick()
    {
        effectiveTick += 1;
        return effectiveTick;
    }

    private void ApplyEngineTimeScale()
    {
        Engine.TimeScale = controller.Current.EffectiveScalePercent / 100d;
        GetTree().Paused = controller.Current.IsPaused;
    }

    private static GDictionary ToDictionary(RuntimeSpeedState state)
    {
        return new GDictionary
        {
            ["mode"] = state.Mode.ToString(),
            ["scale_percent"] = state.EffectiveScalePercent,
            ["is_paused"] = state.IsPaused,
            ["effective_tick"] = state.EffectiveTick,
            ["source"] = state.Source,
        };
    }

    private static GArray ToArray(System.Collections.Generic.IEnumerable<string> items)
    {
        var result = new GArray();
        foreach (var item in items)
        {
            result.Add(item);
        }

        return result;
    }
}
