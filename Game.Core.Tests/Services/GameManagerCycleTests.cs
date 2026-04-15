using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Domain;
using Game.Core.Domain.ValueObjects;
using Game.Core.Ports;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Services;

public class GameManagerCycleTests
{
    // ACC:T19.6
    [Fact]
    public void ShouldFollowFixedOrderAndExactCycleDurations_WhenAdvancingThreeCompleteCycles()
    {
        var manager = CreateInitializedManager(new DayNightCycleConfig(DayDurationSeconds: 240, NightDurationSeconds: 120, MaxDay: 30));
        var checkpoints = new List<DayNightCheckpoint>();
        manager.OnDayNightCheckpoint += checkpoints.Add;

        for (var cycleIndex = 0; cycleIndex < 3; cycleIndex++)
        {
            manager.UpdateDayNightRuntime(240);
            manager.UpdateDayNightRuntime(120);
        }

        checkpoints.Select(checkpoint => (checkpoint.Day, checkpoint.From, checkpoint.To)).Should().Equal(
            (1, DayNightPhase.Day, DayNightPhase.Night),
            (2, DayNightPhase.Night, DayNightPhase.Day),
            (2, DayNightPhase.Day, DayNightPhase.Night),
            (3, DayNightPhase.Night, DayNightPhase.Day),
            (3, DayNightPhase.Day, DayNightPhase.Night),
            (4, DayNightPhase.Night, DayNightPhase.Day));

        manager.CurrentDayNightDay.Should().Be(4);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
    }

    // ACC:T19.15
    [Fact]
    public void ShouldIncrementDayExactlyOncePerFullPair_WhenCrossingDayAndNightBoundaries()
    {
        var manager = CreateInitializedManager(new DayNightCycleConfig(DayDurationSeconds: 240, NightDurationSeconds: 120, MaxDay: 30));

        manager.CurrentDayNightDay.Should().Be(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);

        manager.UpdateDayNightRuntime(239);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);

        manager.UpdateDayNightRuntime(1);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);

        manager.UpdateDayNightRuntime(119);
        manager.CurrentDayNightDay.Should().Be(1);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Night);

        manager.UpdateDayNightRuntime(1);
        manager.CurrentDayNightDay.Should().Be(2);
        manager.CurrentDayNightPhase.Should().Be(DayNightPhase.Day);
    }

    // ACC:T19.18
    [Fact]
    public void ShouldReachWinAfterNight15AndNeverStartDay16_WhenRunProgressesFromDay1()
    {
        var manager = CreateInitializedManager(new DayNightCycleConfig(DayDurationSeconds: 240, NightDurationSeconds: 120, MaxDay: 15));
        var checkpoints = new List<DayNightCheckpoint>();
        manager.OnDayNightCheckpoint += checkpoints.Add;

        for (var second = 0; second < 6000 && !manager.IsRunTerminal; second++)
        {
            manager.UpdateDayNightRuntime(1);
        }

        manager.IsRunTerminal.Should().BeTrue();
        manager.CurrentRunTerminalOutcome.Should().Be(RunTerminalOutcome.Win);

        var hasDay1ToNight1 = checkpoints.Any(checkpoint =>
            checkpoint.Day == 1 &&
            checkpoint.From == DayNightPhase.Day &&
            checkpoint.To == DayNightPhase.Night);

        var hasDay15ToNight15 = checkpoints.Any(checkpoint =>
            checkpoint.Day == 15 &&
            checkpoint.From == DayNightPhase.Day &&
            checkpoint.To == DayNightPhase.Night);

        hasDay1ToNight1.Should().BeTrue();
        hasDay15ToNight15.Should().BeTrue("the win path must expose Day15 -> Night15 before terminal win");
        checkpoints.Should().NotContain(checkpoint => checkpoint.Day == 16);
    }

    // ACC:T19.20
    [Theory]
    [InlineData(240, 120, 240, 120, 0.01, true)]
    [InlineData(241, 120, 240, 120, 0.01, false)]
    [InlineData(240, 121, 240, 120, 0.01, false)]
    public void ShouldValidateObservedDurationsAgainstTolerance_WhenMeasuringPhaseLengths(
        int configuredDaySeconds,
        int configuredNightSeconds,
        int expectedDaySeconds,
        int expectedNightSeconds,
        double toleranceSeconds,
        bool expectedWithinTolerance)
    {
        var runtime = new DayNightRuntimeStateMachine(
            seed: 101,
            config: new DayNightCycleConfig(
                DayDurationSeconds: configuredDaySeconds,
                NightDurationSeconds: configuredNightSeconds,
                MaxDay: 30));

        var observedDaySeconds = MeasureDurationUntilTransition(runtime, DayNightPhase.Day);
        var observedNightSeconds = MeasureDurationUntilTransition(runtime, DayNightPhase.Night);

        var isDayWithinTolerance = Math.Abs(observedDaySeconds - expectedDaySeconds) <= toleranceSeconds;
        var isNightWithinTolerance = Math.Abs(observedNightSeconds - expectedNightSeconds) <= toleranceSeconds;

        (isDayWithinTolerance && isNightWithinTolerance).Should().Be(expectedWithinTolerance);
    }

    private static double MeasureDurationUntilTransition(DayNightRuntimeStateMachine runtime, DayNightPhase phaseToMeasure)
    {
        runtime.CurrentPhase.Should().Be(phaseToMeasure);

        var elapsedSeconds = 0d;
        for (var updateCount = 0; updateCount < 100000 && runtime.CurrentPhase == phaseToMeasure; updateCount++)
        {
            runtime.Update(1);
            elapsedSeconds += 1;
        }

        runtime.CurrentPhase.Should().NotBe(phaseToMeasure);
        return elapsedSeconds;
    }

    private static GameStateManager CreateInitializedManager(DayNightCycleConfig dayNightConfig)
    {
        var manager = new GameStateManager(
            store: new NoopStore(),
            dayNightSeed: 19,
            dayNightConfig: dayNightConfig);

        manager.SetState(
            state: new GameState(
                Id: "run-1",
                Level: 1,
                Score: 0,
                Health: 100,
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
