using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;
using Game.Core.Ports;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class GameManagerTerminalConditionsTests
{
    // ACC:T19.16
    [Fact]
    public void ShouldEnterWinOnlyAfterDay15ToNight15_WhenCastleHpRemainsPositive()
    {
        var manager = CreateInitializedManager(castleHp: 12);
        var outcomes = new List<RunTerminalOutcome>();
        manager.OnRunTerminal += state => outcomes.Add(state.Outcome);

        AdvanceUpdates(manager, 28);

        manager.CurrentDayNightDay.Should().Be(15);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
        manager.IsRunTerminal.Should().BeFalse("win should not happen before Day15 -> Night15 completes");

        manager.UpdateDayNightRuntime(1);

        manager.CurrentDayNightDay.Should().Be(15);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);
        manager.IsRunTerminal.Should().BeFalse("win should happen after Night15 completes");

        manager.UpdateDayNightRuntime(1);

        manager.CurrentDayNightDay.Should().Be(15);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Terminal);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
        outcomes.Should().Equal(RunTerminalOutcome.Win);
    }

    // ACC:T19.16
    [Fact]
    public void ShouldNotEmitLoseAfterWin_WhenRunAlreadyWon()
    {
        var manager = CreateInitializedManager(castleHp: 12);
        var outcomes = new List<RunTerminalOutcome>();
        manager.OnRunTerminal += state => outcomes.Add(state.Outcome);

        AdvanceUpdates(manager, 40);
        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);

        manager.SetCastleHp(0);
        manager.UpdateDayNightRuntime(30);

        outcomes.Should().ContainSingle().Which.Should().Be(RunTerminalOutcome.Win);
        outcomes.Should().NotContain(RunTerminalOutcome.Loss);
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
    }

    // ACC:T19.17
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShouldStopPhaseTransitionsAndDayProgression_WhenRunHasEnteredTerminal(bool triggerLossFirst)
    {
        var manager = CreateInitializedManager(castleHp: 8);
        var checkpointCount = 0;
        manager.OnDayNightCheckpoint += _ => checkpointCount += 1;

        if (triggerLossFirst)
        {
            manager.SetCastleHp(0);
            manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
        }
        else
        {
            AdvanceUpdates(manager, 40);
            manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
        }

        var dayAtTerminal = manager.CurrentDayNightDay;
        var phaseAtTerminal = manager.CurrentDayNightPhase;
        var checkpointCountAtTerminal = checkpointCount;

        manager.UpdateDayNightRuntime(60);
        manager.UpdateDayNightRuntime(60);
        manager.UpdateDayNightRuntime(60);

        manager.CurrentDayNightDay.Should().Be(dayAtTerminal);
        manager.CurrentDayNightPhase.Should().Be(phaseAtTerminal);
        checkpointCount.Should().Be(checkpointCountAtTerminal);
    }

    // ACC:T19.21
    [Fact]
    public void ShouldEnterLose_WhenCastleHpFallsToZeroOrBelow()
    {
        var manager = CreateInitializedManager(castleHp: 6);

        manager.ApplyCastleDamage(7);

        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
        manager.IsWinPresentationVisible.Should().BeFalse();
    }

    // ACC:T19.21
    [Fact]
    public void ShouldEnterWin_WhenCastleSurvivesThroughDay15ToNight15()
    {
        var manager = CreateInitializedManager(castleHp: 6);

        AdvanceUpdates(manager, 30);

        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);
    }

    // ACC:T19.9
    [Fact]
    public void ShouldPreferLoseOverWin_WhenCastleHpDropsToZeroDuringDay15()
    {
        var manager = CreateInitializedManager(castleHp: 9);

        AdvanceUpdates(manager, 28);
        manager.CurrentDayNightDay.Should().Be(15);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);

        manager.SetCastleHp(0);

        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
    }

    // ACC:T19.9
    [Fact]
    public void ShouldPreferLoseOverWin_WhenCastleHpDropsToZeroDuringNight15()
    {
        var manager = CreateInitializedManager(castleHp: 9);

        AdvanceUpdates(manager, 29);
        manager.CurrentDayNightDay.Should().Be(15);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);

        manager.SetCastleHp(0);

        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Loss);
    }

    private static void AdvanceUpdates(GameStateManager manager, int updateCount)
    {
        for (var index = 0; index < updateCount; index++)
        {
            manager.UpdateDayNightRuntime(1);
        }
    }

    private static GameStateManager CreateInitializedManager(int castleHp)
    {
        var manager = new GameStateManager(
            store: new NoopStore(),
            dayNightSeed: 19,
            dayNightConfig: new DayNightCycleConfig(
                DayDurationSeconds: 1,
                NightDurationSeconds: 1,
                MaxDay: 15));

        manager.SetState(
            state: new GameState(
                Id: "run-terminal-tests",
                Level: 1,
                Score: 0,
                Health: castleHp,
                Inventory: Array.Empty<string>(),
                Position: new Position(0, 0),
                Timestamp: DateTime.UtcNow),
            config: new GameConfig(
                MaxLevel: 50,
                InitialHealth: 100,
                ScoreMultiplier: 1.0,
                AutoSave: false,
                Difficulty: Difficulty.Medium));

        return manager;
    }

    private sealed class NoopStore : IDataStore
    {
        public Task SaveAsync(string key, string json) => Task.CompletedTask;

        public Task<string?> LoadAsync(string key) => Task.FromResult<string?>(null);

        public Task DeleteAsync(string key) => Task.CompletedTask;
    }
}
