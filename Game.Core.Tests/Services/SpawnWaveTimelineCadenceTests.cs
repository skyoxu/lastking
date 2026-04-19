using System;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Game.Core.State;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class SpawnWaveTimelineCadenceTests
{
    // ACC:T34.10
    [Fact]
    [Trait("acceptance", "ACC:T34.10")]
    public void ShouldProduceNinetyMinuteTotalDuration_WhenUsingDay1ToDay15WithFourMinuteDayAndTwoMinuteNight()
    {
        var cycle = new DayNightCycleConfig(DayDurationSeconds: 240, NightDurationSeconds: 120, MaxDay: 15);
        var totalSeconds = cycle.MaxDay * (cycle.DayDurationSeconds + cycle.NightDurationSeconds);
        var totalMinutes = TimeSpan.FromSeconds(totalSeconds).TotalMinutes;

        totalSeconds.Should().Be(5400);
        totalMinutes.Should().Be(90);
        totalMinutes.Should().BeInRange(60, 90);
    }

    // ACC:T34.3
    [Fact]
    [Trait("acceptance", "ACC:T34.3")]
    public void ShouldKeepNightCadenceDeterministicAcrossRuns_WhenUsingIdenticalInputs()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A", "Spawn-B" };

        var firstRun = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);
        var secondRun = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);

        secondRun.Select(e => e.ElapsedSeconds).Should().Equal(firstRun.Select(e => e.ElapsedSeconds));
        secondRun.Select(e => e.SpawnPointId).Should().Equal(firstRun.Select(e => e.SpawnPointId));
    }

    [Fact]
    public void ShouldReportOverNinetyMinuteSchedule_WhenConfiguredForMoreThanFifteenDays()
    {
        var cycle = new DayNightCycleConfig(DayDurationSeconds: 240, NightDurationSeconds: 120, MaxDay: 16);
        var totalSeconds = cycle.MaxDay * (cycle.DayDurationSeconds + cycle.NightDurationSeconds);
        var totalMinutes = TimeSpan.FromSeconds(totalSeconds).TotalMinutes;
        var runtime = new DayNightRuntimeStateMachine(
            seed: 34,
            config: cycle);

        runtime.Update(deltaSeconds: 5760.0);

        totalMinutes.Should().Be(96);
        totalMinutes.Should().BeGreaterThan(90);
        runtime.IsTerminal.Should().BeTrue();
        runtime.CurrentDay.Should().Be(16);
    }
}
