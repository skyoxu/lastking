using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class GameplayConfigSetSwitchingTests
{
    private static readonly string[] SpawnPoints = new[] { "north", "south", "west" };

    // ACC:T38.8
    [Fact]
    public void ShouldProduceDeterministicEnemyAndWaveDifferences_WhenRunningDistinctValidConfigSets()
    {
        var configSetA = CreateValidConfigSet(
            enemyId: "grunt",
            health: 12,
            damage: 3,
            speed: 1.2m,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            spawnCadenceSeconds: 10,
            bossCount: 2);

        var configSetB = CreateValidConfigSet(
            enemyId: "grunt",
            health: 18,
            damage: 5,
            speed: 1.8m,
            day1Budget: 80,
            dailyGrowth: 1.3m,
            spawnCadenceSeconds: 8,
            bossCount: 3);

        var firstRun = ObserveConfigSet(configSetA, seed: 1337, dayIndex: 3);
        var replayRun = ObserveConfigSet(configSetA, seed: 1337, dayIndex: 3);
        var switchedRun = ObserveConfigSet(configSetB, seed: 1337, dayIndex: 3);

        replayRun.Should().BeEquivalentTo(firstRun);
        switchedRun.Enemy.Should().NotBe(firstRun.Enemy);
        switchedRun.WaveBudgetSnapshot.Should().NotBe(firstRun.WaveBudgetSnapshot);
        switchedRun.CadenceSnapshot.Should().NotBe(firstRun.CadenceSnapshot);
        switchedRun.NightCompositionSnapshot.Should().NotBe(firstRun.NightCompositionSnapshot);
    }

    // ACC:T38.9
    [Fact]
    public void ShouldApplyRuntimeTuningDelta_WhenOnlyGameplayConfigPayloadChanges()
    {
        var baselineConfigSet = CreateValidConfigSet(
            enemyId: "grunt",
            health: 12,
            damage: 3,
            speed: 1.2m,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            spawnCadenceSeconds: 10,
            bossCount: 2);
        var promotedConfigSet = CreateValidConfigSet(
            enemyId: "grunt",
            health: 24,
            damage: 7,
            speed: 2.1m,
            day1Budget: 95,
            dailyGrowth: 1.4m,
            spawnCadenceSeconds: 6,
            bossCount: 4);

        var configManager = new ConfigManager();
        var resolver = new EnemyConfigRuntimeResolver();
        var waveManager = new WaveManager();

        var baselineLoad = configManager.LoadInitialFromJson(baselineConfigSet.GameplayConfigJson, "gameplay-config-a.json");
        baselineLoad.Accepted.Should().BeTrue();
        var baselineObservation = CaptureObservation(
            configManager,
            resolver,
            waveManager,
            enemyId: baselineConfigSet.EnemyId,
            seed: 1701,
            dayIndex: 4);

        var promotedReload = configManager.ReloadFromJson(promotedConfigSet.GameplayConfigJson, "gameplay-config-b.json");
        promotedReload.Accepted.Should().BeTrue();
        var promotedObservation = CaptureObservation(
            configManager,
            resolver,
            waveManager,
            enemyId: promotedConfigSet.EnemyId,
            seed: 1701,
            dayIndex: 4);

        promotedObservation.Enemy.Health.Should().Be(24);
        promotedObservation.Enemy.Damage.Should().Be(7);
        promotedObservation.Enemy.Speed.Should().Be(2.1m);
        promotedObservation.Enemy.Should().NotBe(baselineObservation.Enemy);
        promotedObservation.WaveBudgetSnapshot.Should().NotBe(baselineObservation.WaveBudgetSnapshot);
        promotedObservation.CadenceSnapshot.Should().NotBe(baselineObservation.CadenceSnapshot);
        promotedObservation.NightCompositionSnapshot.Should().NotBe(baselineObservation.NightCompositionSnapshot);
    }

    [Fact]
    public void ShouldKeepEnemyAndWaveOutputsUnchanged_WhenReloadingEquivalentConfigSets()
    {
        var firstConfigSet = CreateValidConfigSet(
            enemyId: "grunt",
            health: 12,
            damage: 3,
            speed: 1.2m,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            spawnCadenceSeconds: 10,
            bossCount: 2);

        var secondConfigSet = CreateValidConfigSet(
            enemyId: "grunt",
            health: 12,
            damage: 3,
            speed: 1.2m,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            spawnCadenceSeconds: 10,
            bossCount: 2);

        var firstRun = ObserveConfigSet(firstConfigSet, seed: 2026, dayIndex: 4);
        var secondRun = ObserveConfigSet(secondConfigSet, seed: 2026, dayIndex: 4);

        secondRun.Should().BeEquivalentTo(firstRun);
    }

    // ACC:T38.13
    [Fact]
    public void ShouldChangeEnemyStatAndWaveMetric_WhenSwitchingFromConfigSetAToConfigSetB()
    {
        var configSetA = CreateValidConfigSet(
            enemyId: "grunt",
            health: 12,
            damage: 3,
            speed: 1.2m,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            spawnCadenceSeconds: 10,
            bossCount: 2);

        var configSetB = CreateValidConfigSet(
            enemyId: "grunt",
            health: 20,
            damage: 6,
            speed: 1.9m,
            day1Budget: 90,
            dailyGrowth: 1.35m,
            spawnCadenceSeconds: 7,
            bossCount: 4);

        var baselineRun = ObserveConfigSet(configSetA, seed: 77, dayIndex: 4);
        var switchedRun = ObserveConfigSet(configSetB, seed: 77, dayIndex: 4);

        switchedRun.Enemy.Health.Should().Be(20);
        switchedRun.Enemy.Damage.Should().Be(6);
        switchedRun.Enemy.Speed.Should().Be(1.9m);
        switchedRun.Enemy.Health.Should().NotBe(baselineRun.Enemy.Health);
        switchedRun.WaveBudgetSnapshot.Should().NotBe(baselineRun.WaveBudgetSnapshot);
        switchedRun.CadenceSnapshot.Should().NotBe(baselineRun.CadenceSnapshot);
    }

    // ACC:T38.14
    [Fact]
    public void ShouldRestoreOriginalEnemyAndWaveOutputs_WhenSwitchingFromAToBToAWithFixedSeed()
    {
        var configSetA = CreateValidConfigSet(
            enemyId: "grunt",
            health: 12,
            damage: 3,
            speed: 1.2m,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            spawnCadenceSeconds: 10,
            bossCount: 2);

        var configSetB = CreateValidConfigSet(
            enemyId: "grunt",
            health: 18,
            damage: 5,
            speed: 1.7m,
            day1Budget: 75,
            dailyGrowth: 1.25m,
            spawnCadenceSeconds: 8,
            bossCount: 3);

        var firstRunA = ObserveConfigSet(configSetA, seed: 99, dayIndex: 5);
        var runB = ObserveConfigSet(configSetB, seed: 99, dayIndex: 5);
        var restoredRunA = ObserveConfigSet(configSetA, seed: 99, dayIndex: 5);

        restoredRunA.Should().BeEquivalentTo(firstRunA);
        runB.Should().NotBeEquivalentTo(firstRunA);
    }

    private static GameplayConfigSet CreateValidConfigSet(
        string enemyId,
        int health,
        int damage,
        decimal speed,
        int day1Budget,
        decimal dailyGrowth,
        int spawnCadenceSeconds,
        int bossCount)
    {
        var gameplayConfigJson = $$"""
        {
          "time": { "day_seconds": 240, "night_seconds": 120 },
          "waves": { "normal": { "day1_budget": {{day1Budget}}, "daily_growth": {{dailyGrowth.ToString(CultureInfo.InvariantCulture)}} } },
          "channels": { "elite": "elite_a", "boss": "boss_a" },
          "spawn": { "cadence_seconds": {{spawnCadenceSeconds}} },
          "boss": { "count": {{bossCount}} },
          "battle": { "castle_start_hp": 100 },
          "enemies": [
            {
              "enemy_id": "{{enemyId}}",
              "enemy_type": "melee",
              "behavior": { "mode": "rush" },
              "health": {{health}},
              "damage": {{damage}},
              "speed": {{speed.ToString(CultureInfo.InvariantCulture)}}
            }
          ]
        }
        """;

        return new GameplayConfigSet(enemyId, gameplayConfigJson);
    }

    private static ConfigSetObservation ObserveConfigSet(GameplayConfigSet configSet, int seed, int dayIndex)
    {
        var configManager = new ConfigManager();
        var resolver = new EnemyConfigRuntimeResolver();
        var loadResult = configManager.LoadInitialFromJson(configSet.GameplayConfigJson, "gameplay-config.json");

        loadResult.Accepted.Should().BeTrue(
            $"expected a valid gameplay config set but got reason codes: {string.Join(",", loadResult.ReasonCodes)}");

        return CaptureObservation(
            configManager,
            resolver,
            waveManager: new WaveManager(),
            enemyId: configSet.EnemyId,
            seed,
            dayIndex);
    }

    private static ConfigSetObservation CaptureObservation(
        ConfigManager configManager,
        EnemyConfigRuntimeResolver resolver,
        WaveManager waveManager,
        string enemyId,
        int seed,
        int dayIndex)
    {
        var waveResult = waveManager.GenerateFromConfig(dayIndex, configManager, seed);
        var cadenceSpawns = waveManager.GenerateRegularSpawnsFromConfig(configManager, isBossNight: false, spawnPoints: SpawnPoints);
        var deterministicNightSpawns = waveManager.GenerateDeterministicNightSpawns(
            new NightRunConfiguration(
                CadenceSeconds: configManager.Snapshot.SpawnCadenceSeconds,
                NightDurationSeconds: configManager.Snapshot.NightSeconds),
            SpawnPoints,
            new[] { enemyId, "support" },
            deterministicSeed: seed);
        var resolvedEnemy = resolver.Resolve(configManager, configManager.ActiveConfigJson)
            .Single(item => string.Equals(item.EnemyId, enemyId, StringComparison.Ordinal));

        return new ConfigSetObservation(
            new EnemyRuntimeObservation(
                resolvedEnemy.EnemyId,
                decimal.ToInt32(resolvedEnemy.Health),
                decimal.ToInt32(resolvedEnemy.Damage),
                resolvedEnemy.Speed),
            BuildWaveBudgetSnapshot(waveResult),
            BuildCadenceSnapshot(cadenceSpawns),
            BuildNightCompositionSnapshot(deterministicNightSpawns));
    }

    private static string BuildWaveBudgetSnapshot(WaveResult waveResult)
    {
        var channelSnapshots = waveResult.ChannelResults
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
                $"{pair.Key}:{pair.Value.Audit.InputBudget},{pair.Value.Audit.Allocated},{pair.Value.Audit.Spent},{pair.Value.Audit.Remaining}|{string.Join(",", pair.Value.SpawnOrder)}");

        return $"{waveResult.DayIndex}|{waveResult.Seed}|{string.Join("|", channelSnapshots)}";
    }

    private static string BuildCadenceSnapshot(IReadOnlyList<CadenceSpawnEmission> cadenceSpawns)
    {
        var emissions = cadenceSpawns
            .Select(emission => $"{emission.ElapsedSeconds.ToString(CultureInfo.InvariantCulture)}:{emission.SpawnPointId}");

        return $"{cadenceSpawns.Count}|{string.Join("|", emissions)}";
    }

    private static string BuildNightCompositionSnapshot(IReadOnlyList<DeterministicNightSpawnEmission> emissions)
    {
        var snapshot = emissions
            .Select(emission =>
                $"{emission.ElapsedSeconds.ToString(CultureInfo.InvariantCulture)}:{emission.SpawnPointId}:{emission.EnemyId}");

        return $"{emissions.Count}|{string.Join("|", snapshot)}";
    }

    private static bool HasConfigAccessToken(string source)
    {
        return source.Contains("ConfigManager", StringComparison.Ordinal)
               || source.Contains("FromConfigManager", StringComparison.Ordinal)
               || source.Contains("GenerateFromConfig", StringComparison.Ordinal)
               || source.Contains("GenerateRegularSpawnsFromConfig", StringComparison.Ordinal)
               || source.Contains("GenerateNightSpawnsFromConfig", StringComparison.Ordinal)
               || source.Contains(".Snapshot", StringComparison.Ordinal);
    }

    private static bool IsGameplayTuningPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var fileName = Path.GetFileName(normalized);

        return normalized.Contains("/Game.Core/Services/", StringComparison.OrdinalIgnoreCase)
               && (fileName.Contains("Enemy", StringComparison.OrdinalIgnoreCase)
                   || fileName.Contains("Wave", StringComparison.OrdinalIgnoreCase)
                   || fileName.Contains("Spawn", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsConfigDefinitionFile(string path)
    {
        var fileName = Path.GetFileName(path);

        return fileName.Contains("Config", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("BalanceSnapshot.cs", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("WaveBudgetModels.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "project.godot")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private readonly record struct GameplayConfigSet(string EnemyId, string GameplayConfigJson);

    private readonly record struct EnemyRuntimeObservation(string EnemyId, int Health, int Damage, decimal Speed);

    private readonly record struct ConfigSetObservation(
        EnemyRuntimeObservation Enemy,
        string WaveBudgetSnapshot,
        string CadenceSnapshot,
        string NightCompositionSnapshot);
}
