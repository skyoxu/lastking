using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class WaveManagerSpawnPolicyTests
{
    // ACC:T5.2
    [Fact]
    public void ShouldEmitRegularSpawnsAtTenSecondCadence_WhenNightWindowIsOpen()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "North" };
        var spawnPolicy = new NightSpawnPolicy(NightDurationSeconds: 120.0, CadenceSeconds: 10.0, BossSpawnCount: 2);

        var emissions = sut.GenerateNightSpawns(spawnPolicy, isBossNight: false, spawnPoints);
        var intervals = emissions
            .Zip(emissions.Skip(1), (left, right) => right.ElapsedSeconds - left.ElapsedSeconds)
            .ToArray();

        emissions.Select(emission => emission.ElapsedSeconds)
            .Should()
            .Equal(0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0);
        intervals.Should().OnlyContain(interval => Math.Abs(interval - 10.0) < 0.0001);
    }

    // ACC:T5.7
    // ACC:T5.11
    [Fact]
    public void ShouldEmitExactlyTwoBossSpawns_WhenNightIsBossNight()
    {
        var sut = new WaveManager();
        var configManager = CreateLoadedConfigManager(bossCount: 2);
        var spawnPoints = new[] { "North", "East" };

        var emissions = sut.GenerateNightSpawnsFromConfig(configManager, isBossNight: true, spawnPoints);
        var bossCount = emissions.Count(emission => emission.EnemyType == NightEnemyType.Boss);

        bossCount.Should().Be(2);
        emissions.Should().HaveCount(2);
        emissions.Should().OnlyContain(emission => emission.EnemyType == NightEnemyType.Boss);
    }

    // ACC:T5.8
    [Fact]
    public void ShouldUseOnlyPredefinedSpawnPoints_WhenAnySpawnIsEmitted()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "North", "East", "South" };
        var spawnPolicy = new NightSpawnPolicy(NightDurationSeconds: 40.0, CadenceSeconds: 10.0, BossSpawnCount: 2);

        var regularEmissions = sut.GenerateNightSpawns(spawnPolicy, isBossNight: false, spawnPoints);
        var bossEmissions = sut.GenerateNightSpawns(spawnPolicy, isBossNight: true, spawnPoints);
        var allEmissions = regularEmissions.Concat(bossEmissions).ToArray();

        allEmissions.Should().NotBeEmpty();
        allEmissions
            .Select(emission => emission.SpawnPointId)
            .Should()
            .OnlyContain(spawnPointId => spawnPoints.Contains(spawnPointId));
    }

    // ACC:T5.15
    [Fact]
    public void ShouldEmitNoSpawnsInFinalTwentyPercentWindow_WhenObligationLockO6Applies()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "North", "East" };
        var spawnPolicy = new NightSpawnPolicy(NightDurationSeconds: 120.0, CadenceSeconds: 10.0, BossSpawnCount: 2);
        var finalWindowStartSeconds = spawnPolicy.NightDurationSeconds * 0.8;

        var regularEmissions = sut.GenerateNightSpawns(spawnPolicy, isBossNight: false, spawnPoints);
        var bossEmissions = sut.GenerateNightSpawns(spawnPolicy, isBossNight: true, spawnPoints);
        var allEmissions = regularEmissions.Concat(bossEmissions).ToArray();

        allEmissions.Should().OnlyContain(emission => emission.ElapsedSeconds < finalWindowStartSeconds);
        bossEmissions.Should().OnlyContain(emission => emission.EnemyType == NightEnemyType.Boss);
        bossEmissions.Count.Should().BeLessOrEqualTo(spawnPolicy.BossSpawnCount);
    }

    [Fact]
    public void ShouldBlockBossEmissionsInFinalTwentyPercentWindow_WhenBossCountAttemptsToOverflow()
    {
        var sut = new WaveManager();
        var spawnPoints = new[] { "North", "East" };
        var spawnPolicy = new NightSpawnPolicy(NightDurationSeconds: 120.0, CadenceSeconds: 10.0, BossSpawnCount: 20);
        var finalWindowStartSeconds = spawnPolicy.NightDurationSeconds * 0.8;

        var bossEmissions = sut.GenerateNightSpawns(spawnPolicy, isBossNight: true, spawnPoints);

        bossEmissions.Should().OnlyContain(emission => emission.ElapsedSeconds < finalWindowStartSeconds);
        bossEmissions.Should().HaveCount(10);
    }

    [Fact]
    public void ShouldEmitNoSpawns_WhenSpawnPointSetIsEmpty()
    {
        var sut = new WaveManager();
        var spawnPolicy = new NightSpawnPolicy(NightDurationSeconds: 120.0, CadenceSeconds: 10.0, BossSpawnCount: 2);

        var emissions = sut.GenerateNightSpawns(spawnPolicy, isBossNight: false, Array.Empty<string>());

        emissions.Should().BeEmpty();
    }

    // ACC:T5.16
    [Fact]
    public void ShouldEmitNoBossSpawns_WhenBossNightHasNoSpawnPointsConfigured()
    {
        var sut = new WaveManager();
        var spawnPolicy = new NightSpawnPolicy(NightDurationSeconds: 120.0, CadenceSeconds: 10.0, BossSpawnCount: 2);

        var emissions = sut.GenerateNightSpawns(spawnPolicy, isBossNight: true, Array.Empty<string>());

        emissions.Should().BeEmpty();
    }

    private static ConfigManager CreateLoadedConfigManager(int bossCount)
    {
        var manager = new ConfigManager();
        var json = $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": 120 },
          "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
          "channels": { "elite": "elite", "boss": "boss" },
          "spawn": { "cadence_seconds": 10 },
          "boss": { "count": {{bossCount}} },
          "battle": { "castle_start_hp": 100 }
        }
        """;

        var result = manager.LoadInitialFromJson(json, "inline-task5-boss-config.json");
        result.Accepted.Should().BeTrue();
        return manager;
    }

}
