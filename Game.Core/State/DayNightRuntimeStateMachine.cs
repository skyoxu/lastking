namespace Game.Core.State;

public enum DayNightPhase
{
    Day,
    Night,
    Terminal
}

public sealed record DayNightCycleConfig(int DayDurationSeconds = 240, int NightDurationSeconds = 120, int MaxDay = 15)
{
    public static DayNightCycleConfig Default { get; } = new();
}

public sealed record DayNightCheckpoint(int Day, DayNightPhase From, DayNightPhase To, long Tick, int RandomToken);
public sealed record DayNightTerminal(int Day, long Tick);

public sealed class DayNightRuntimeStateMachine
{
    private readonly DayNightCycleConfig _config;
    private readonly Random _random;
    private DayNightPhase _phase = DayNightPhase.Day;
    private int _day = 1;
    private bool _terminalRaised;
    private double _phaseElapsedSeconds;
    private long _tick;
    private int _checkpointCount;

    public DayNightRuntimeStateMachine(int seed, DayNightCycleConfig? config = null)
    {
        _config = config ?? DayNightCycleConfig.Default;
        _random = new Random(seed);
    }

    public DayNightPhase CurrentPhase => _phase;
    public int CurrentDay => _day;
    public bool IsTerminal => _phase == DayNightPhase.Terminal;
    public double PhaseElapsedSeconds => _phaseElapsedSeconds;
    public long Tick => _tick;
    public int CheckpointCount => _checkpointCount;

    public event Action<DayNightCheckpoint>? OnCheckpoint;
    public event Action<DayNightTerminal>? OnTerminal;

    public bool ForceTerminal()
    {
        if (IsTerminal)
        {
            return false;
        }

        TransitionTo(DayNightPhase.Terminal);
        RaiseTerminal();
        _phaseElapsedSeconds = 0d;
        return true;
    }

    public bool RequestTransition(DayNightPhase requestedPhase)
    {
        if (IsTerminal || requestedPhase == DayNightPhase.Terminal || requestedPhase == _phase)
        {
            return false;
        }

        if (_phase == DayNightPhase.Day && requestedPhase == DayNightPhase.Night)
        {
            if (_phaseElapsedSeconds < _config.DayDurationSeconds)
            {
                return false;
            }

            var overflow = _phaseElapsedSeconds - _config.DayDurationSeconds;
            TransitionTo(DayNightPhase.Night);
            _phaseElapsedSeconds = overflow;
            return true;
        }

        if (_phase == DayNightPhase.Night && requestedPhase == DayNightPhase.Day)
        {
            if (_phaseElapsedSeconds < _config.NightDurationSeconds || _day >= _config.MaxDay)
            {
                return false;
            }

            var overflow = _phaseElapsedSeconds - _config.NightDurationSeconds;
            _day += 1;
            TransitionTo(DayNightPhase.Day);
            _phaseElapsedSeconds = overflow;
            return true;
        }

        return false;
    }

    public void Update(double deltaSeconds, bool isActiveUpdate = true)
    {
        if (IsTerminal || !isActiveUpdate || deltaSeconds <= 0d)
        {
            return;
        }

        _tick++;
        _phaseElapsedSeconds += deltaSeconds;

        if (_phase == DayNightPhase.Day)
        {
            ProcessDay();
            return;
        }

        ProcessNight();
    }

    private void ProcessDay()
    {
        if (_phaseElapsedSeconds < _config.DayDurationSeconds)
        {
            return;
        }

        var overflow = _phaseElapsedSeconds - _config.DayDurationSeconds;
        if (RequestTransition(DayNightPhase.Night))
        {
            _phaseElapsedSeconds = overflow;
            ProcessNight();
        }
    }

    private void ProcessNight()
    {
        if (_phaseElapsedSeconds < _config.NightDurationSeconds)
        {
            return;
        }

        var overflow = _phaseElapsedSeconds - _config.NightDurationSeconds;
        if (_day >= _config.MaxDay)
        {
            ForceTerminal();
            return;
        }

        if (RequestTransition(DayNightPhase.Day))
        {
            _phaseElapsedSeconds = overflow;
            ProcessDay();
        }
    }

    private void TransitionTo(DayNightPhase next)
    {
        var previous = _phase;
        _phase = next;

        if (next == DayNightPhase.Terminal)
        {
            return;
        }

        _checkpointCount += 1;
        OnCheckpoint?.Invoke(new DayNightCheckpoint(
            Day: _day,
            From: previous,
            To: next,
            Tick: _tick,
            RandomToken: _random.Next()));
    }

    private void RaiseTerminal()
    {
        if (_terminalRaised)
        {
            return;
        }

        _terminalRaised = true;
        OnTerminal?.Invoke(new DayNightTerminal(_day, _tick));
    }
}
