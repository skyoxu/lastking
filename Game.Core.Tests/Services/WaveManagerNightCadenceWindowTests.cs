using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerNightCadenceWindowTests
{
    // ACC:T5.1
    [Fact]
    public void ShouldGateRegularSpawnsToFirstEightyPercent_WhenNightDurationIs120Seconds()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A" };

        var events = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);

        events.Should().OnlyContain(e => e.ElapsedSeconds < 96.0);
    }

    // ACC:T5.2
    [Fact]
    public void ShouldEmitRegularSpawnsAtTenSecondCadence_WhenSpawningWindowIsOpen()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A" };

        var events = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);
        var enabledWindowTimes = events
            .Where(e => e.ElapsedSeconds < 96.0)
            .Select(e => e.ElapsedSeconds)
            .ToArray();

        enabledWindowTimes.Should().Equal(0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0);
    }

    // ACC:T5.3
    [Fact]
    public void ShouldEmitNoNewRegularSpawns_WhenElapsedTimeIsInFinalTwentyPercentWindow()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A" };

        var events = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);
        var finalWindowCount = events.Count(e => e.ElapsedSeconds >= 96.0 && e.ElapsedSeconds <= 120.0);

        finalWindowCount.Should().Be(0);
    }

    // ACC:T5.4
    [Fact]
    public void ShouldEmitAtLeastOneRegularSpawn_WhenNightIsNonBossAndSpawnPointExists()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A" };

        var events = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);
        var enabledWindowCount = events.Count(e => e.ElapsedSeconds < 96.0);

        enabledWindowCount.Should().BeGreaterThan(0);
    }

    // ACC:T5.5
    [Theory]
    [InlineData(120.0, 10)]
    [InlineData(150.0, 12)]
    public void ShouldApplyEightyPercentWindowRule_WhenNightDurationChanges(double durationSeconds, int cadenceSeconds)
    {
        var sut = new WaveManager();
        var configManager = CreateLoadedConfigManager((int)durationSeconds, cadenceSeconds);
        var spawnPoints = new[] { "Spawn-A" };
        var cutoff = durationSeconds * 0.8;

        var events = sut.GenerateRegularSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);
        var intervals = events.Zip(events.Skip(1), (left, right) => right.ElapsedSeconds - left.ElapsedSeconds).ToArray();

        events.Should().OnlyContain(e => e.ElapsedSeconds < cutoff);
        intervals.Should().OnlyContain(interval => Math.Abs(interval - cadenceSeconds) < 0.0001);
    }

    // ACC:T5.6
    [Fact]
    public void ShouldStopRegularCadenceFromOneHundredTwentySeconds_WhenNightDurationIsOneHundredFiftySeconds()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A" };

        var events = sut.GenerateRegularSpawns(150.0, isBossNight: false, spawnPoints);
        var blockedWindowCount = events.Count(e => e.ElapsedSeconds >= 120.0 && e.ElapsedSeconds <= 150.0);

        blockedWindowCount.Should().Be(0);
    }

    // ACC:T5.10
    [Fact]
    public void ShouldKeepRegularSpawnCountBetweenNineAndTenBeforeCutoff_WhenNightDurationIs120Seconds()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A" };

        var events = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);
        var beforeCutoffCount = events.Count(e => e.ElapsedSeconds < 96.0);
        var afterCutoffCount = events.Count(e => e.ElapsedSeconds >= 96.0 && e.ElapsedSeconds <= 120.0);

        beforeCutoffCount.Should().BeInRange(9, 10);
        afterCutoffCount.Should().Be(0);
    }

    // ACC:T5.11
    [Fact]
    public void ShouldEmitNoRegularSpawns_WhenNightIsBossNight()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A", "Spawn-B" };

        var events = sut.GenerateRegularSpawns(120.0, isBossNight: true, spawnPoints);

        events.Should().BeEmpty();
    }

    // ACC:T5.12
    [Fact]
    public void ShouldUseOnlyConfiguredSpawnPoints_WhenRegularSpawnsAreEmitted()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "Spawn-A", "Spawn-B", "Spawn-C" };

        var events = sut.GenerateRegularSpawns(40.0, isBossNight: false, spawnPoints);
        var usedSpawnPoints = events.Select(e => e.SpawnPointId).Distinct().ToArray();

        usedSpawnPoints.Should().OnlyContain(spawnPointId => spawnPoints.Contains(spawnPointId));
        usedSpawnPoints.Should().Contain("Spawn-A");
        usedSpawnPoints.Should().Contain("Spawn-B");
        usedSpawnPoints.Should().Contain("Spawn-C");
    }

    // ACC:T5.13
    [Fact]
    public void ShouldEmitPeriodicTimerDrivenSpawns_WhenSimulatingOneSecondFrames()
    {
        var sut = new WaveManager();
        const int frameCount = 120;
        var oneSecondSampling = CollectTimerDrivenEmissionTimes(sut, frameCount, samplingStepSeconds: 1.0);
        var halfSecondSampling = CollectTimerDrivenEmissionTimes(sut, frameCount, samplingStepSeconds: 0.5);
        var intervals = oneSecondSampling
            .Zip(oneSecondSampling.Skip(1), (left, right) => right - left)
            .ToArray();

        oneSecondSampling.Should().Equal(halfSecondSampling);
        oneSecondSampling.Count.Should().BeLessThan(frameCount);
        intervals.Should().OnlyContain(interval => Math.Abs(interval - 10.0) < 0.0001);
    }

    // ACC:T5.14
    [Fact]
    public void ShouldRefuseCadenceSpawning_WhenNoSpawnPointsAreConfigured()
    {
        var sut = new WaveManager();
        var spawnPoints = Array.Empty<string>();

        var events = sut.GenerateRegularSpawns(120.0, isBossNight: false, spawnPoints);

        events.Should().BeEmpty();
    }

    private static ConfigManager CreateLoadedConfigManager(int nightSeconds, int cadenceSeconds)
    {
        var manager = new ConfigManager();
        var json = $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": {{nightSeconds}} },
          "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
          "channels": { "elite": "elite", "boss": "boss" },
          "spawn": { "cadence_seconds": {{cadenceSeconds}} },
          "boss": { "count": 2 },
          "battle": { "castle_start_hp": 100 }
        }
        """;

        var result = manager.LoadInitialFromJson(json, "inline-task5-night-config.json");
        result.Accepted.Should().BeTrue();
        return manager;
    }

    private static IReadOnlyList<double> CollectTimerDrivenEmissionTimes(
        WaveManager sut,
        int frameCount,
        double samplingStepSeconds)
    {
        var emissions = new List<StateGatedSpawnEmission>();
        for (var elapsedSeconds = 0.0; elapsedSeconds < frameCount; elapsedSeconds += samplingStepSeconds)
        {
            emissions.AddRange(sut.GenerateCadenceDrivenSpawns(
                NightGameplayState.Night,
                elapsedSeconds,
                spawnPointId: "Spawn-A",
                enemyId: "Grunt"));
        }

        return emissions
            .Select(emission => emission.ElapsedSeconds)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
    }

}
