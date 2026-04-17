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
public sealed record DayNightTerminal(int Day, long Tick, DayNightPhase FromPhase);

public sealed class DayNightRuntimeStateMachine
{
    private readonly DayNightCycleConfig _config;
    private Random _random;
    private int _seed;
    private DayNightPhase _phase = DayNightPhase.Day;
    private int _day = 1;
    private bool _terminalRaised;
    private double _phaseElapsedSeconds;
    private long _tick;
    private int _checkpointCount;
    private DayNightPhase _terminalFromPhase = DayNightPhase.Day;

    public DayNightRuntimeStateMachine(int seed, DayNightCycleConfig? config = null)
    {
        _config = config ?? DayNightCycleConfig.Default;
        _seed = seed;
        _random = CreateRandomAtCheckpoint(seed, checkpointCount: 0);
    }

    public DayNightPhase CurrentPhase => _phase;
    public int CurrentDay => _day;
    public int MaxDay => _config.MaxDay;
    public bool IsTerminal => _phase == DayNightPhase.Terminal;
    public double PhaseElapsedSeconds => _phaseElapsedSeconds;
    public long Tick => _tick;
    public int CheckpointCount => _checkpointCount;

    public event Action<DayNightCheckpoint>? OnCheckpoint;
    public event Action<DayNightTerminal>? OnTerminal;

    public DayNightRuntimeSnapshot ExportSnapshot()
    {
        return new DayNightRuntimeSnapshot(
            Seed: _seed,
            Day: _day,
            Phase: _phase,
            PhaseElapsedSeconds: _phaseElapsedSeconds,
            Tick: _tick,
            CheckpointCount: _checkpointCount,
            TerminalRaised: _terminalRaised,
            TerminalFromPhase: _terminalFromPhase);
    }

    public void RestoreSnapshot(DayNightRuntimeSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (snapshot.Day <= 0)
        {
            throw new InvalidOperationException("DayNight runtime snapshot is invalid: day must be positive.");
        }

        if (snapshot.PhaseElapsedSeconds < 0d)
        {
            throw new InvalidOperationException("DayNight runtime snapshot is invalid: phase elapsed seconds must be non-negative.");
        }

        if (snapshot.Tick < 0)
        {
            throw new InvalidOperationException("DayNight runtime snapshot is invalid: tick must be non-negative.");
        }

        if (snapshot.CheckpointCount < 0)
        {
            throw new InvalidOperationException("DayNight runtime snapshot is invalid: checkpoint count must be non-negative.");
        }

        _day = snapshot.Day;
        _phase = snapshot.Phase;
        _phaseElapsedSeconds = snapshot.PhaseElapsedSeconds;
        _tick = snapshot.Tick;
        _checkpointCount = snapshot.CheckpointCount;
        _terminalRaised = snapshot.TerminalRaised;
        _terminalFromPhase = snapshot.TerminalFromPhase;
        _seed = snapshot.Seed;
        _random = CreateRandomAtCheckpoint(_seed, _checkpointCount);
    }

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

    public void Reset()
    {
        _phase = DayNightPhase.Day;
        _day = 1;
        _phaseElapsedSeconds = 0d;
        _tick = 0;
        _checkpointCount = 0;
        _terminalRaised = false;
        _terminalFromPhase = DayNightPhase.Day;
        _random = CreateRandomAtCheckpoint(_seed, checkpointCount: 0);
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
            _terminalFromPhase = previous;
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
        OnTerminal?.Invoke(new DayNightTerminal(_day, _tick, _terminalFromPhase));
    }

    private static Random CreateRandomAtCheckpoint(int seed, int checkpointCount)
    {
        var random = new Random(seed);
        for (var index = 0; index < checkpointCount; index++)
        {
            _ = random.Next();
        }

        return random;
    }
}
