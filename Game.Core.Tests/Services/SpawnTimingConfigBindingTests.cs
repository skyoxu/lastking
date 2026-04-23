using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class SpawnTimingConfigBindingTests
{
    // ACC:T38.5
    [Fact]
    public void ShouldChangeRuntimeWaveSpawnTiming_WhenOnlySpawnConfigTimingValuesChange()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var spawnPoints = new[] { "North", "East" };

        var initialLoad = configManager.LoadInitialFromJson(
            BuildBalanceJson(spawnCadenceSeconds: 10),
            "inline-task38-spawn-a.json");
        var beforeEmissions = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        var reload = configManager.ReloadFromJson(
            BuildBalanceJson(spawnCadenceSeconds: 6),
            "inline-task38-spawn-b.json");
        var afterEmissions = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        initialLoad.Accepted.Should().BeTrue();
        reload.Accepted.Should().BeTrue();
        MeasureFirstIntervalSeconds(beforeEmissions).Should().Be(10.0);
        MeasureFirstIntervalSeconds(afterEmissions).Should().Be(6.0);
        afterEmissions.Count.Should().NotBe(beforeEmissions.Count);
    }

    // ACC:T38.10
    [Fact]
    public void ShouldMatchSelectedEnemyHealthAndSpawnProfileInterval_WhenActiveConfigValuesAreAppliedToRuntime()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var resolver = new EnemyConfigRuntimeResolver();
        var enemyId = "ogre";
        var spawnPoints = new[] { "North", "East" };
        var spawnConfigJson = BuildProfileAwareSpawnBalanceJson(globalCadenceSeconds: 10, regularCadenceSeconds: 4, bossCadenceSeconds: 9);
        var gameplayConfigJson = BuildGameplayConfigWithEnemyJson(
            enemyId: enemyId,
            health: 135,
            regularCadenceSeconds: 4,
            bossCadenceSeconds: 9);

        var loadResult = configManager.LoadInitialFromJson(gameplayConfigJson, "inline-task38-profile-config.json");
        var regularEmissions = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);
        var bossEmissions = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: true, spawnPoints);
        var observedEnemyHealth = resolver.Resolve(configManager, gameplayConfigJson)
            .Single(item => string.Equals(item.EnemyId, enemyId, StringComparison.Ordinal))
            .Health;
        var expectedRegularInterval = ReadProfileCadenceSeconds(gameplayConfigJson, "regular");
        var expectedBossInterval = ReadProfileCadenceSeconds(gameplayConfigJson, "boss");

        loadResult.Accepted.Should().BeTrue();
        observedEnemyHealth.Should().Be(135);
        MeasureFirstIntervalSeconds(regularEmissions).Should().Be(expectedRegularInterval);
        MeasureFirstIntervalSeconds(bossEmissions).Should().Be(expectedBossInterval);
    }

    // ACC:T38.13
    [Fact]
    public void ShouldChangeEnemyHealthAndWaveOutputMetric_WhenSwitchingBetweenValidConfigSets()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var enemyId = "brute";
        var spawnPoints = new[] { "North", "East" };
        var resolver = new EnemyConfigRuntimeResolver();
        var gameplayConfigSetA = BuildGameplayConfigWithEnemyJson(enemyId: enemyId, health: 90, regularCadenceSeconds: 12, bossCadenceSeconds: 12);
        var gameplayConfigSetB = BuildGameplayConfigWithEnemyJson(enemyId: enemyId, health: 140, regularCadenceSeconds: 5, bossCadenceSeconds: 5);

        var initialLoad = configManager.LoadInitialFromJson(gameplayConfigSetA, "inline-task38-config-set-a.json");
        var waveOutputA = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);
        var enemyHealthA = resolver.Resolve(configManager, gameplayConfigSetA)
            .Single(item => string.Equals(item.EnemyId, enemyId, StringComparison.Ordinal))
            .Health;

        var reload = configManager.ReloadFromJson(gameplayConfigSetB, "inline-task38-config-set-b.json");
        var waveOutputB = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);
        var enemyHealthB = resolver.Resolve(configManager, gameplayConfigSetB)
            .Single(item => string.Equals(item.EnemyId, enemyId, StringComparison.Ordinal))
            .Health;

        initialLoad.Accepted.Should().BeTrue();
        reload.Accepted.Should().BeTrue();
        enemyHealthB.Should().NotBe(enemyHealthA);
        MeasureFirstIntervalSeconds(waveOutputB).Should().NotBe(MeasureFirstIntervalSeconds(waveOutputA));
        waveOutputB.Count.Should().NotBe(waveOutputA.Count);
    }

    [Fact]
    public void ShouldKeepPreviousSpawnTiming_WhenConfigOnlyTimingUpdateIsRejected()
    {
        var waveManager = new WaveManager();
        var configManager = new ConfigManager();
        var spawnPoints = new[] { "North" };

        var initialLoad = configManager.LoadInitialFromJson(
            BuildBalanceJson(spawnCadenceSeconds: 8),
            "inline-task38-stable-config.json");
        var beforeEmissions = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        var rejectedReload = configManager.ReloadFromJson(
            BuildBalanceJson(spawnCadenceSeconds: 0),
            "inline-task38-invalid-config.json");
        var afterEmissions = waveManager.GenerateNightSpawnsFromConfig(configManager, isBossNight: false, spawnPoints);

        initialLoad.Accepted.Should().BeTrue();
        rejectedReload.Accepted.Should().BeFalse();
        rejectedReload.ReasonCodes.Should().Contain(ConfigManager.OutOfRangeReason);
        MeasureFirstIntervalSeconds(afterEmissions).Should().Be(MeasureFirstIntervalSeconds(beforeEmissions));
        afterEmissions.Select(emission => emission.ElapsedSeconds)
            .Should()
            .Equal(beforeEmissions.Select(emission => emission.ElapsedSeconds));
    }

    private static string BuildBalanceJson(int spawnCadenceSeconds, int nightSeconds = 120, int bossCount = 3)
    {
        return $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": {{nightSeconds}} },
          "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
          "channels": { "elite": "elite", "boss": "boss" },
          "spawn": { "cadence_seconds": {{spawnCadenceSeconds}} },
          "boss": { "count": {{bossCount}} },
          "battle": { "castle_start_hp": 100 }
        }
        """;
    }

    private static string BuildProfileAwareSpawnBalanceJson(
        int globalCadenceSeconds,
        int regularCadenceSeconds,
        int bossCadenceSeconds,
        int nightSeconds = 120,
        int bossCount = 3)
    {
        return $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": {{nightSeconds}} },
          "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
          "channels": { "elite": "elite", "boss": "boss" },
          "spawn": {
            "cadence_seconds": {{globalCadenceSeconds}},
            "profiles": {
              "regular": { "cadence_seconds": {{regularCadenceSeconds}} },
              "boss": { "cadence_seconds": {{bossCadenceSeconds}} }
            }
          },
          "boss": { "count": {{bossCount}} },
          "battle": { "castle_start_hp": 100 }
        }
        """;
    }

    private static string BuildGameplayConfigWithEnemyJson(
        string enemyId,
        int health,
        int regularCadenceSeconds,
        int bossCadenceSeconds)
    {
        return $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": 120 },
          "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
          "channels": { "elite": "elite", "boss": "boss" },
          "spawn": {
            "cadence_seconds": {{regularCadenceSeconds}},
            "profiles": {
              "regular": { "cadence_seconds": {{regularCadenceSeconds}} },
              "boss": { "cadence_seconds": {{bossCadenceSeconds}} }
            }
          },
          "boss": { "count": 3 },
          "battle": { "castle_start_hp": 100 },
          "enemies": [
            {
              "enemy_id": "{{enemyId}}",
              "health": {{health}},
              "damage": 12,
              "speed": 1.5,
              "enemy_type": "melee",
              "behavior": { "mode": "rush" }
            }
          ]
        }
        """;
    }

    private static double MeasureFirstIntervalSeconds(System.Collections.Generic.IReadOnlyList<NightSpawnEmission> emissions)
    {
        if (emissions.Count < 2)
        {
            throw new InvalidOperationException("At least two emissions are required to measure a spawn interval.");
        }

        return emissions[1].ElapsedSeconds - emissions[0].ElapsedSeconds;
    }

    private static double ReadProfileCadenceSeconds(string spawnConfigJson, string profileName)
    {
        using var document = JsonDocument.Parse(spawnConfigJson);

        return document.RootElement
            .GetProperty("spawn")
            .GetProperty("profiles")
            .GetProperty(profileName)
            .GetProperty("cadence_seconds")
            .GetDouble();
    }
}
