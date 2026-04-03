namespace Game.Core.State;

public enum RunTerminalOutcome
{
    None,
    Win,
    Loss
}

public enum EndOfGameHandling
{
    Pause,
    Reset
}

public sealed record RunTerminalState(
    RunTerminalOutcome Outcome,
    int Day,
    int CastleHp,
    EndOfGameHandling AppliedHandling,
    bool WinPresentationVisible,
    long Tick
);
