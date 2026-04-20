using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class GameplayTuningConfigIntegrationTests
{
    // ACC:T38.1
    [Fact]
    public void ShouldResolveEnemyAndWaveRuntimeFromActiveConfig_WhenConfigValuesChangeWithoutCodeEdits()
    {
        var baselineJson = CreateGameplayConfigJson(
            enemyHealth: 12,
            enemyDamage: 3,
            enemySpeed: 1.2,
            spawnCadenceSeconds: 10,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            bossCount: 2);
        var promotedJson = CreateGameplayConfigJson(
            enemyHealth: 24,
            enemyDamage: 7,
            enemySpeed: 1.9,
            spawnCadenceSeconds: 6,
            day1Budget: 95,
            dailyGrowth: 1.4m,
            bossCount: 4);

        var manager = new ConfigManager();
        var waveManager = new WaveManager();

        var baselineLoad = manager.LoadInitialFromJson(baselineJson, "res://Config/gameplay-tuning-a.json");
        baselineLoad.Accepted.Should().BeTrue();
        var baselineProbe = TryResolveEnemyRuntimeStats(manager, baselineJson, "grunt");
        baselineProbe.Success.Should().BeTrue(baselineProbe.FailureMessage);
        var baselineWave = waveManager.Generate(
            dayIndex: 3,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 20260438);
        var baselineTimeline = waveManager.GenerateRegularSpawns(
            60,
            isBossNight: false,
            spawnPoints: new[] { "Spawn-A", "Spawn-B", "Spawn-C" },
            cadenceSeconds: manager.Snapshot.SpawnCadenceSeconds);

        var promotedLoad = manager.ReloadFromJson(promotedJson, "res://Config/gameplay-tuning-b.json");
        promotedLoad.Accepted.Should().BeTrue();
        var promotedProbe = TryResolveEnemyRuntimeStats(manager, promotedJson, "grunt");
        promotedProbe.Success.Should().BeTrue(promotedProbe.FailureMessage);
        var promotedWave = waveManager.Generate(
            dayIndex: 3,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 20260438);
        var promotedTimeline = waveManager.GenerateRegularSpawns(
            60,
            isBossNight: false,
            spawnPoints: new[] { "Spawn-A", "Spawn-B", "Spawn-C" },
            cadenceSeconds: manager.Snapshot.SpawnCadenceSeconds);

        promotedProbe.Health.Should().Be(24m);
        promotedProbe.Damage.Should().Be(7m);
        promotedProbe.Speed.Should().Be(1.9m);
        promotedProbe.Health.Should().NotBe(baselineProbe.Health);
        BuildWaveSnapshot(promotedWave).Should().NotBe(BuildWaveSnapshot(baselineWave));
        BuildSpacingSnapshot(promotedTimeline.Select(emission => emission.ElapsedSeconds))
            .Should().NotBe(BuildSpacingSnapshot(baselineTimeline.Select(emission => emission.ElapsedSeconds)));
    }

    // ACC:T38.2
    [Fact]
    public void ShouldFollowActiveConfigAndSeed_WhenGeneratingRuntimeWaveAndSpawnOutputs()
    {
        var configJson = CreateGameplayConfigJson(
            enemyHealth: 18,
            enemyDamage: 4,
            enemySpeed: 1.35,
            spawnCadenceSeconds: 6,
            day1Budget: 70,
            dailyGrowth: 1.35m,
            bossCount: 3);

        var manager = new ConfigManager();
        var load = manager.LoadInitialFromJson(configJson, "res://Config/gameplay-tuning.json");

        load.Accepted.Should().BeTrue();

        var enemyProbe = TryResolveEnemyRuntimeStats(manager, configJson, "grunt");
        enemyProbe.Success.Should().BeTrue(enemyProbe.FailureMessage);
        enemyProbe.Health.Should().Be(18m);
        enemyProbe.Damage.Should().Be(4m);
        enemyProbe.Speed.Should().Be(1.35m);

        var waveManager = new WaveManager();
        var firstWave = waveManager.Generate(
            dayIndex: 2,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 20260420);
        var replayWave = waveManager.Generate(
            dayIndex: 2,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 20260420);
        var driftedWave = waveManager.Generate(
            dayIndex: 2,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 20260421);

        BuildWaveSnapshot(replayWave).Should().Be(BuildWaveSnapshot(firstWave));
        BuildWaveSnapshot(driftedWave).Should().NotBe(BuildWaveSnapshot(firstWave));

        var spawnPoints = new[] { "Spawn-A", "Spawn-B", "Spawn-C" };
        var timeline = waveManager.GenerateRegularSpawns(
            60.0,
            isBossNight: false,
            spawnPoints,
            cadenceSeconds: load.Snapshot.SpawnCadenceSeconds);

        timeline.Should().HaveCountGreaterThan(2);
        BuildSpacingSnapshot(timeline.Select(emission => emission.ElapsedSeconds)).Should().Be("6|6|6|6|6|6|6");
    }

    // ACC:T38.21
    [Fact]
    public void ShouldPreserveDeterministicReplayBaseline_WhenConfigAndSeedStayUnchanged()
    {
        var configJson = CreateGameplayConfigJson(
            enemyHealth: 15,
            enemyDamage: 3,
            enemySpeed: 1.2,
            spawnCadenceSeconds: 8,
            day1Budget: 60,
            dailyGrowth: 1.25m,
            bossCount: 2);

        var manager = new ConfigManager();
        var load = manager.LoadInitialFromJson(configJson, "res://Config/gameplay-tuning.json");

        load.Accepted.Should().BeTrue();

        var waveManager = new WaveManager();
        var firstWave = waveManager.Generate(
            dayIndex: 4,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 1337);
        var secondWave = waveManager.Generate(
            dayIndex: 4,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 1337);

        BuildWaveSnapshot(secondWave).Should().Be(BuildWaveSnapshot(firstWave));

        var spawnPoints = new[] { "Spawn-A", "Spawn-B" };
        var firstTimeline = waveManager.GenerateRegularSpawns(
            80.0,
            isBossNight: false,
            spawnPoints,
            cadenceSeconds: load.Snapshot.SpawnCadenceSeconds);
        var secondTimeline = waveManager.GenerateRegularSpawns(
            80.0,
            isBossNight: false,
            spawnPoints,
            cadenceSeconds: load.Snapshot.SpawnCadenceSeconds);

        BuildElapsedSnapshot(secondTimeline.Select(emission => emission.ElapsedSeconds))
            .Should().Be(BuildElapsedSnapshot(firstTimeline.Select(emission => emission.ElapsedSeconds)));
        secondTimeline.Select(emission => emission.SpawnPointId)
            .Should().Equal(firstTimeline.Select(emission => emission.SpawnPointId));
    }

    // ACC:T38.22
    [Fact]
    public void ShouldChangeEnemyHealth_WhenOnlyEnemyConfigValuesChange()
    {
        var baselineJson = CreateGameplayConfigJson(
            enemyHealth: 12,
            enemyDamage: 3,
            enemySpeed: 1.2,
            spawnCadenceSeconds: 10,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            bossCount: 2);
        var tunedJson = CreateGameplayConfigJson(
            enemyHealth: 27,
            enemyDamage: 3,
            enemySpeed: 1.2,
            spawnCadenceSeconds: 10,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            bossCount: 2);

        var baselineManager = new ConfigManager();
        var tunedManager = new ConfigManager();

        baselineManager.LoadInitialFromJson(baselineJson, "res://Config/gameplay-tuning.json").Accepted.Should().BeTrue();
        tunedManager.LoadInitialFromJson(tunedJson, "res://Config/gameplay-tuning.json").Accepted.Should().BeTrue();

        var baselineStats = TryResolveEnemyRuntimeStats(baselineManager, baselineJson, "grunt");
        var tunedStats = TryResolveEnemyRuntimeStats(tunedManager, tunedJson, "grunt");

        baselineStats.Success.Should().BeTrue(baselineStats.FailureMessage);
        tunedStats.Success.Should().BeTrue(tunedStats.FailureMessage);
        baselineStats.Health.Should().Be(12m);
        tunedStats.Health.Should().Be(27m);
        tunedStats.Health.Should().NotBe(baselineStats.Health,
            "config-only enemy health changes must alter runtime enemy stats without script edits");
    }

    // ACC:T38.3
    [Fact]
    public void ShouldChangeSpawnTiming_WhenOnlySpawnConfigTimingValuesChange()
    {
        var baselineJson = CreateGameplayConfigJson(
            enemyHealth: 15,
            enemyDamage: 3,
            enemySpeed: 1.2,
            spawnCadenceSeconds: 10,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            bossCount: 2);
        var tunedJson = CreateGameplayConfigJson(
            enemyHealth: 15,
            enemyDamage: 3,
            enemySpeed: 1.2,
            spawnCadenceSeconds: 4,
            day1Budget: 50,
            dailyGrowth: 1.2m,
            bossCount: 2);

        var baselineManager = new ConfigManager();
        var tunedManager = new ConfigManager();
        var baselineLoad = baselineManager.LoadInitialFromJson(baselineJson, "res://Config/gameplay-tuning.json");
        var tunedLoad = tunedManager.LoadInitialFromJson(tunedJson, "res://Config/gameplay-tuning.json");

        baselineLoad.Accepted.Should().BeTrue();
        tunedLoad.Accepted.Should().BeTrue();

        var waveManager = new WaveManager();
        var spawnPoints = new[] { "Spawn-A", "Spawn-B" };
        var baselineTimeline = waveManager.GenerateRegularSpawns(
            60.0,
            isBossNight: false,
            spawnPoints,
            cadenceSeconds: baselineLoad.Snapshot.SpawnCadenceSeconds);
        var tunedTimeline = waveManager.GenerateRegularSpawns(
            60.0,
            isBossNight: false,
            spawnPoints,
            cadenceSeconds: tunedLoad.Snapshot.SpawnCadenceSeconds);

        BuildSpacingSnapshot(baselineTimeline.Select(emission => emission.ElapsedSeconds)).Should().Be("10|10|10|10");
        BuildSpacingSnapshot(tunedTimeline.Select(emission => emission.ElapsedSeconds)).Should().Be("4|4|4|4|4|4|4|4|4|4|4");
        BuildElapsedSnapshot(tunedTimeline.Select(emission => emission.ElapsedSeconds))
            .Should().NotBe(BuildElapsedSnapshot(baselineTimeline.Select(emission => emission.ElapsedSeconds)));
    }

    // ACC:T38.4
    [Fact]
    public void ShouldTrackRuntimeOutputDelta_WhenValidConfigProvidesDifferentTuningValues()
    {
        var firstJson = CreateGameplayConfigJson(
            enemyHealth: 14,
            enemyDamage: 3,
            enemySpeed: 1.15,
            spawnCadenceSeconds: 10,
            day1Budget: 52,
            dailyGrowth: 1.2m,
            bossCount: 2);
        var secondJson = CreateGameplayConfigJson(
            enemyHealth: 28,
            enemyDamage: 8,
            enemySpeed: 2.0,
            spawnCadenceSeconds: 4,
            day1Budget: 100,
            dailyGrowth: 1.45m,
            bossCount: 5);

        var manager = new ConfigManager();
        var waveManager = new WaveManager();
        var baselineLoad = manager.LoadInitialFromJson(firstJson, "res://Config/gameplay-tuning-first.json");
        baselineLoad.Accepted.Should().BeTrue();
        var baselineEnemy = TryResolveEnemyRuntimeStats(manager, firstJson, "grunt");
        baselineEnemy.Success.Should().BeTrue(baselineEnemy.FailureMessage);
        var baselineWave = waveManager.Generate(
            dayIndex: 5,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 4201);
        var baselineCadence = waveManager.GenerateRegularSpawns(
            50,
            isBossNight: false,
            spawnPoints: new[] { "Spawn-A", "Spawn-B" },
            cadenceSeconds: manager.Snapshot.SpawnCadenceSeconds);

        var promotedLoad = manager.ReloadFromJson(secondJson, "res://Config/gameplay-tuning-second.json");
        promotedLoad.Accepted.Should().BeTrue();
        var promotedEnemy = TryResolveEnemyRuntimeStats(manager, secondJson, "grunt");
        promotedEnemy.Success.Should().BeTrue(promotedEnemy.FailureMessage);
        var promotedWave = waveManager.Generate(
            dayIndex: 5,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 4201);
        var promotedCadence = waveManager.GenerateRegularSpawns(
            50,
            isBossNight: false,
            spawnPoints: new[] { "Spawn-A", "Spawn-B" },
            cadenceSeconds: manager.Snapshot.SpawnCadenceSeconds);

        promotedEnemy.Health.Should().Be(28m);
        promotedEnemy.Damage.Should().Be(8m);
        promotedEnemy.Speed.Should().Be(2m);
        promotedEnemy.Health.Should().NotBe(baselineEnemy.Health);
        BuildWaveSnapshot(promotedWave).Should().NotBe(BuildWaveSnapshot(baselineWave));
        BuildSpacingSnapshot(promotedCadence.Select(emission => emission.ElapsedSeconds))
            .Should().NotBe(BuildSpacingSnapshot(baselineCadence.Select(emission => emission.ElapsedSeconds)));
    }

    // ACC:T38.5
    [Fact]
    public void ShouldRejectPromotionAndKeepActiveConfigUnchanged_WhenGovernanceCriteriaAreMissing()
    {
        using var workspace = TemporaryDirectory.Create();

        var baselineJson = CreateGameplayConfigJson(
            enemyHealth: 14,
            enemyDamage: 3,
            enemySpeed: 1.1,
            spawnCadenceSeconds: 9,
            day1Budget: 52,
            dailyGrowth: 1.2m,
            bossCount: 2);
        var candidateJson = CreateGameplayConfigJson(
            enemyHealth: 30,
            enemyDamage: 7,
            enemySpeed: 1.8,
            spawnCadenceSeconds: 3,
            day1Budget: 90,
            dailyGrowth: 1.5m,
            bossCount: 4);

        var manager = new ConfigManager();
        var baselineLoad = manager.LoadInitialFromJson(baselineJson, "res://Config/gameplay-tuning.json");
        baselineLoad.Accepted.Should().BeTrue();

        var baselineSnapshot = manager.Snapshot;
        var baselineWaveSnapshot = BuildWaveSnapshot(new WaveManager().Generate(
            dayIndex: 2,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 77));

        var evaluation = EvaluateGovernanceAttempt(
            workspace.DirectoryPath,
            candidateJson,
            Array.Empty<string>(),
            "missing-governance-criteria");

        evaluation.Success.Should().BeTrue(evaluation.FailureMessage);
        evaluation.Decision.Should().Be("reject");
        evaluation.Reasons.Should().NotBeEmpty();
        HasAnyToken(evaluation.NormalizedReason, "criteria", "governance", "policy", "missing")
            .Should().BeTrue("missing governance criteria must block promotion with a machine-readable rejection reason");

        manager.Snapshot.Should().Be(baselineSnapshot);

        var replayWaveSnapshot = BuildWaveSnapshot(new WaveManager().Generate(
            dayIndex: 2,
            channelBudgetConfiguration: ChannelBudgetConfiguration.FromConfigManager(manager),
            seed: 77));
        replayWaveSnapshot.Should().Be(baselineWaveSnapshot);
    }

    // ACC:T38.7
    [Fact]
    public void ShouldReturnSameRejectionOutcomeAndReason_WhenIdenticalInvalidInputIsRetried()
    {
        using var workspace = TemporaryDirectory.Create();

        const string invalidConfigJson = "{ \"time\": { \"day_seconds\": 0 }, \"channels\": {} }";

        var firstEvaluation = EvaluateGovernanceAttempt(
            workspace.DirectoryPath,
            invalidConfigJson,
            Array.Empty<string>(),
            "deterministic-invalid-input");
        var secondEvaluation = EvaluateGovernanceAttempt(
            workspace.DirectoryPath,
            invalidConfigJson,
            Array.Empty<string>(),
            "deterministic-invalid-input");

        firstEvaluation.Success.Should().BeTrue(firstEvaluation.FailureMessage);
        secondEvaluation.Success.Should().BeTrue(secondEvaluation.FailureMessage);
        firstEvaluation.Decision.Should().Be("reject");
        secondEvaluation.Decision.Should().Be("reject");
        secondEvaluation.NormalizedReason.Should().Be(firstEvaluation.NormalizedReason);
    }

    private static string CreateGameplayConfigJson(
        int enemyHealth,
        int enemyDamage,
        double enemySpeed,
        int spawnCadenceSeconds,
        int day1Budget,
        decimal dailyGrowth,
        int bossCount)
    {
        var growth = dailyGrowth.ToString(CultureInfo.InvariantCulture);
        var speed = enemySpeed.ToString(CultureInfo.InvariantCulture);

        return $$"""
        {
          "time": {
            "day_seconds": 240,
            "night_seconds": 120
          },
          "waves": {
            "normal": {
              "day1_budget": {{day1Budget}},
              "daily_growth": {{growth}}
            }
          },
          "channels": {
            "elite": "elite",
            "boss": "boss"
          },
          "spawn": {
            "cadence_seconds": {{spawnCadenceSeconds}}
          },
          "boss": {
            "count": {{bossCount}}
          },
          "battle": {
            "castle_start_hp": 100
          },
          "enemies": [
            {
              "enemy_id": "grunt",
              "health": {{enemyHealth}},
              "damage": {{enemyDamage}},
              "speed": {{speed}},
              "enemy_type": "melee",
              "behavior": {
                "mode": "rush"
              }
            }
          ]
        }
        """;
    }

    private static GameplayEnemyRuntimeProbeResult TryResolveEnemyRuntimeStats(
        ConfigManager manager,
        string configJson,
        string enemyId)
    {
        LoadCoreAssembly();
        using var document = JsonDocument.Parse(configJson);
        var candidateTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .Where(type => type.Namespace is not null && type.Namespace.StartsWith("Game.Core", StringComparison.Ordinal))
            .Where(type => type.Name.Contains("Enemy", StringComparison.Ordinal))
            .Where(type =>
                type.Name.Contains("Config", StringComparison.Ordinal)
                || type.Name.Contains("Tuning", StringComparison.Ordinal)
                || type.Name.Contains("Runtime", StringComparison.Ordinal)
                || type.Name.Contains("Resolver", StringComparison.Ordinal)
                || type.Name.Contains("Catalog", StringComparison.Ordinal)
                || type.Name.Contains("Definition", StringComparison.Ordinal))
            .ToArray();

        foreach (var candidateType in candidateTypes)
        {
            object? instance;
            try
            {
                instance = CreateEnemyResolverInstance(candidateType, manager, workspacePath: Path.GetTempPath());
            }
            catch
            {
                continue;
            }

            var methods = candidateType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(method => method.ReturnType != typeof(void))
                .Where(method =>
                    method.Name.Contains("Resolve", StringComparison.Ordinal)
                    || method.Name.Contains("Get", StringComparison.Ordinal)
                    || method.Name.Contains("Build", StringComparison.Ordinal)
                    || method.Name.Contains("Create", StringComparison.Ordinal)
                    || method.Name.Contains("Find", StringComparison.Ordinal)
                    || method.Name.Contains("Load", StringComparison.Ordinal))
                .Where(method => CanBuildEnemyArguments(method.GetParameters()))
                .ToArray();

            foreach (var method in methods)
            {
                try
                {
                    var arguments = BuildEnemyArguments(method.GetParameters(), manager, document.RootElement, configJson, enemyId);
                    var rawResult = method.Invoke(method.IsStatic ? null : instance, arguments);
                    rawResult = ResolveAwaitable(rawResult);

                    if (TryExtractEnemyStats(rawResult, enemyId, out var stats))
                    {
                        return new GameplayEnemyRuntimeProbeResult(
                            Success: true,
                            Health: stats.Health,
                            Damage: stats.Damage,
                            Speed: stats.Speed,
                            FailureMessage: string.Empty);
                    }
                }
                catch
                {
                }
            }
        }

        return new GameplayEnemyRuntimeProbeResult(
            Success: false,
            Health: 0m,
            Damage: 0m,
            Speed: 0m,
            FailureMessage: "No production enemy runtime binding could prove that enemy stats are driven by the active gameplay config.");
    }

    private static object? CreateEnemyResolverInstance(Type candidateType, ConfigManager manager, string workspacePath)
    {
        var constructors = candidateType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(ctor => ctor.GetParameters().Length)
            .ToArray();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            if (!parameters.All(CanBuildEnemyConstructorParameter))
            {
                continue;
            }

            var arguments = parameters.Select(parameter => BuildEnemyConstructorArgument(parameter, manager, workspacePath)).ToArray();
            return constructor.Invoke(arguments);
        }

        return Activator.CreateInstance(candidateType);
    }

    private static bool CanBuildEnemyConstructorParameter(ParameterInfo parameter)
    {
        var parameterType = parameter.ParameterType;
        return parameterType == typeof(ConfigManager)
               || parameterType == typeof(BalanceSnapshot)
               || parameterType == typeof(string)
               || parameterType == typeof(DirectoryInfo)
               || parameterType == typeof(JsonDocument)
               || parameterType == typeof(JsonElement)
               || parameterType == typeof(int)
               || parameterType == typeof(bool);
    }

    private static object? BuildEnemyConstructorArgument(ParameterInfo parameter, ConfigManager manager, string workspacePath)
    {
        var parameterType = parameter.ParameterType;
        var parameterName = parameter.Name ?? string.Empty;

        if (parameterType == typeof(ConfigManager))
        {
            return manager;
        }

        if (parameterType == typeof(BalanceSnapshot))
        {
            return manager.Snapshot;
        }

        if (parameterType == typeof(string))
        {
            return ContainsAny(parameterName, "path", "directory", "root", "logs") ? workspacePath : "grunt";
        }

        if (parameterType == typeof(DirectoryInfo))
        {
            return new DirectoryInfo(workspacePath);
        }

        if (parameterType == typeof(int))
        {
            return 1337;
        }

        if (parameterType == typeof(bool))
        {
            return false;
        }

        return null;
    }

    private static bool CanBuildEnemyArguments(ParameterInfo[] parameters)
    {
        return parameters.All(parameter =>
            parameter.ParameterType == typeof(ConfigManager)
            || parameter.ParameterType == typeof(BalanceSnapshot)
            || parameter.ParameterType == typeof(string)
            || parameter.ParameterType == typeof(JsonDocument)
            || parameter.ParameterType == typeof(JsonElement)
            || parameter.ParameterType == typeof(int)
            || parameter.ParameterType == typeof(bool));
    }

    private static object?[] BuildEnemyArguments(
        ParameterInfo[] parameters,
        ConfigManager manager,
        JsonElement configRoot,
        string configJson,
        string enemyId)
    {
        return parameters.Select(parameter =>
        {
            var parameterType = parameter.ParameterType;
            var parameterName = parameter.Name ?? string.Empty;

            if (parameterType == typeof(ConfigManager))
            {
                return (object?)manager;
            }

            if (parameterType == typeof(BalanceSnapshot))
            {
                return manager.Snapshot;
            }

            if (parameterType == typeof(string))
            {
                if (ContainsAny(parameterName, "enemy", "id", "key", "name", "archetype"))
                {
                    return enemyId;
                }

                return configJson;
            }

            if (parameterType == typeof(JsonDocument))
            {
                return JsonDocument.Parse(configJson);
            }

            if (parameterType == typeof(JsonElement))
            {
                return configRoot.Clone();
            }

            if (parameterType == typeof(int))
            {
                return 1337;
            }

            if (parameterType == typeof(bool))
            {
                return false;
            }

            return null;
        }).ToArray();
    }

    private static bool TryExtractEnemyStats(object? rawResult, string enemyId, out GameplayEnemyRuntimeStats stats)
    {
        return TryExtractEnemyStats(rawResult, enemyId, depth: 0, out stats);
    }

    private static bool TryExtractEnemyStats(object? rawResult, string enemyId, int depth, out GameplayEnemyRuntimeStats stats)
    {
        stats = default;
        if (rawResult is null || depth > 4)
        {
            return false;
        }

        if (rawResult is JsonDocument document)
        {
            return TryExtractEnemyStatsFromJson(document.RootElement, enemyId, out stats);
        }

        if (rawResult is JsonElement element)
        {
            return TryExtractEnemyStatsFromJson(element, enemyId, out stats);
        }

        if (TryBuildEnemyStatsFromObject(rawResult, out stats))
        {
            return true;
        }

        if (rawResult is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                if (MatchesEnemyId(item, enemyId) && TryBuildEnemyStatsFromObject(item, out stats))
                {
                    return true;
                }

                if (TryExtractEnemyStats(item, enemyId, depth + 1, out stats))
                {
                    return true;
                }
            }
        }

        foreach (var propertyName in new[] { "Stats", "RuntimeStats", "BaseStats", "Definition", "Value", "Current", "Enemy", "Enemies" })
        {
            if (TryGetPropertyValue(rawResult, propertyName, out var nested)
                && TryExtractEnemyStats(nested, enemyId, depth + 1, out stats))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryExtractEnemyStatsFromJson(JsonElement element, string enemyId, out GameplayEnemyRuntimeStats stats)
    {
        stats = default;

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryExtractEnemyStatsFromJson(item, enemyId, out stats))
                {
                    return true;
                }
            }

            return false;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (TryReadJsonDecimal(element, "health", out var health)
            && TryReadJsonDecimal(element, "damage", out var damage)
            && TryReadJsonDecimal(element, "speed", out var speed))
        {
            if (!TryReadJsonString(element, "enemy_id", out var id) && !TryReadJsonString(element, "id", out id))
            {
                stats = new GameplayEnemyRuntimeStats(health, damage, speed);
                return true;
            }

            if (string.Equals(id, enemyId, StringComparison.Ordinal))
            {
                stats = new GameplayEnemyRuntimeStats(health, damage, speed);
                return true;
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (TryExtractEnemyStatsFromJson(property.Value, enemyId, out stats))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryBuildEnemyStatsFromObject(object value, out GameplayEnemyRuntimeStats stats)
    {
        stats = default;
        if (!TryGetDecimalLike(value, "Health", out var health)
            || !TryGetDecimalLike(value, "Damage", out var damage)
            || !TryGetDecimalLike(value, "Speed", out var speed))
        {
            return false;
        }

        stats = new GameplayEnemyRuntimeStats(health, damage, speed);
        return true;
    }

    private static bool MatchesEnemyId(object value, string enemyId)
    {
        return TryGetStringLike(value, "EnemyId", out var found)
               || TryGetStringLike(value, "Id", out found)
               || TryGetStringLike(value, "Name", out found)
            ? string.Equals(found, enemyId, StringComparison.Ordinal)
            : false;
    }

    private static GovernanceEvaluationProbeResult EvaluateGovernanceAttempt(
        string logsCiDirectory,
        string candidateConfigJson,
        IReadOnlyList<string> governanceCriteria,
        string scenarioId)
    {
        LoadCoreAssembly();
        var serviceType = FindType(
            "Game.Core.Services.ConfigGovernancePromotionService",
            "Game.Core.Services.ConfigPromotionGovernanceService",
            "Game.Core.Services.ConfigGovernanceService",
            "Game.Core.Services.GameplayTuningGovernanceService",
            "Game.Core.Services.ConfigPromotionService");

        if (serviceType is null)
        {
            return GovernanceEvaluationProbeResult.Failure("No production gameplay-tuning governance service could be found.");
        }

        var method = FindGovernanceEvaluationMethod(serviceType);
        if (method is null)
        {
            return GovernanceEvaluationProbeResult.Failure("No production gameplay-tuning governance evaluation method could be found.");
        }

        try
        {
            var instance = method.IsStatic ? null : CreateGovernanceInstance(serviceType, logsCiDirectory);
            var arguments = BuildGovernanceArguments(method.GetParameters(), logsCiDirectory, candidateConfigJson, governanceCriteria, scenarioId);
            var rawResult = ResolveAwaitable(method.Invoke(instance, arguments));
            return ParseGovernanceEvaluation(rawResult);
        }
        catch (Exception exception)
        {
            return GovernanceEvaluationProbeResult.Failure($"Governance evaluation failed to execute: {exception.GetType().Name}");
        }
    }

    private static MethodInfo? FindGovernanceEvaluationMethod(Type serviceType)
    {
        var methodNames = new[]
        {
            "EvaluatePromotionAttempt",
            "EvaluatePromotion",
            "Promote",
            "PromoteConfig",
            "ExecutePromotionAttempt",
            "Evaluate",
            "ValidatePromotion"
        };

        return serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => methodNames.Contains(method.Name, StringComparer.Ordinal))
            .Where(method => method.ReturnType != typeof(void))
            .FirstOrDefault(method => CanBuildGovernanceArguments(method.GetParameters()));
    }

    private static object CreateGovernanceInstance(Type serviceType, string logsCiDirectory)
    {
        var stringConstructor = serviceType.GetConstructor(new[] { typeof(string) });
        if (stringConstructor is not null)
        {
            return stringConstructor.Invoke(new object[] { logsCiDirectory });
        }

        var directoryConstructor = serviceType.GetConstructor(new[] { typeof(DirectoryInfo) });
        if (directoryConstructor is not null)
        {
            return directoryConstructor.Invoke(new object[] { new DirectoryInfo(logsCiDirectory) });
        }

        var defaultConstructor = serviceType.GetConstructor(Type.EmptyTypes);
        if (defaultConstructor is not null)
        {
            return defaultConstructor.Invoke(Array.Empty<object>());
        }

        return Activator.CreateInstance(serviceType)
               ?? throw new InvalidOperationException("Unable to create governance service instance.");
    }

    private static bool CanBuildGovernanceArguments(ParameterInfo[] parameters)
    {
        return parameters.All(parameter =>
            parameter.ParameterType == typeof(string)
            || parameter.ParameterType == typeof(DirectoryInfo)
            || parameter.ParameterType == typeof(JsonDocument)
            || parameter.ParameterType == typeof(JsonElement)
            || parameter.ParameterType == typeof(bool)
            || parameter.ParameterType == typeof(string[])
            || parameter.ParameterType == typeof(List<string>)
            || parameter.ParameterType == typeof(IReadOnlyList<string>)
            || parameter.ParameterType == typeof(IEnumerable<string>)
            || parameter.ParameterType.IsEnum
            || parameter.ParameterType.IsClass);
    }

    private static object?[] BuildGovernanceArguments(
        ParameterInfo[] parameters,
        string logsCiDirectory,
        string candidateConfigJson,
        IReadOnlyList<string> governanceCriteria,
        string scenarioId)
    {
        return parameters.Select(parameter =>
            BuildGovernanceArgument(parameter, logsCiDirectory, candidateConfigJson, governanceCriteria, scenarioId)).ToArray();
    }

    private static object? BuildGovernanceArgument(
        ParameterInfo parameter,
        string logsCiDirectory,
        string candidateConfigJson,
        IReadOnlyList<string> governanceCriteria,
        string scenarioId)
    {
        var parameterName = parameter.Name ?? string.Empty;
        var parameterType = parameter.ParameterType;
        var requestJson = BuildGovernanceRequestJson(logsCiDirectory, candidateConfigJson, governanceCriteria, scenarioId);

        if (parameterType == typeof(string))
        {
            if (ContainsAny(parameterName, "log", "ci", "artifact", "output"))
            {
                return logsCiDirectory;
            }

            if (ContainsAny(parameterName, "scenario", "case", "correlation", "promotionid", "promotion_id", "attemptid", "attempt_id"))
            {
                return scenarioId;
            }

            if (ContainsAny(parameterName, "decision", "outcome"))
            {
                return "reject";
            }

            if (ContainsAny(parameterName, "criteria", "policy", "governance"))
            {
                return string.Join(",", governanceCriteria);
            }

            if (ContainsAny(parameterName, "target", "scope"))
            {
                return "gameplay-tuning";
            }

            if (ContainsAny(parameterName, "requester", "requestedby", "requested_by", "caller"))
            {
                return "ci";
            }

            return candidateConfigJson;
        }

        if (parameterType == typeof(DirectoryInfo))
        {
            return new DirectoryInfo(logsCiDirectory);
        }

        if (parameterType == typeof(JsonDocument))
        {
            return JsonDocument.Parse(ContainsAny(parameterName, "config", "candidate") ? candidateConfigJson : requestJson);
        }

        if (parameterType == typeof(JsonElement))
        {
            return JsonDocument.Parse(ContainsAny(parameterName, "config", "candidate") ? candidateConfigJson : requestJson)
                .RootElement.Clone();
        }

        if (parameterType == typeof(bool))
        {
            return false;
        }

        if (parameterType == typeof(string[]))
        {
            return governanceCriteria.ToArray();
        }

        if (parameterType == typeof(List<string>))
        {
            return governanceCriteria.ToList();
        }

        if (parameterType == typeof(IReadOnlyList<string>) || parameterType == typeof(IEnumerable<string>))
        {
            return governanceCriteria.ToArray();
        }

        if (parameterType.IsEnum)
        {
            return BuildEnumValue(parameterType);
        }

        if (parameterType.IsClass)
        {
            return JsonSerializer.Deserialize(requestJson, parameterType);
        }

        return null;
    }

    private static string BuildGovernanceRequestJson(
        string logsCiDirectory,
        string candidateConfigJson,
        IReadOnlyList<string> governanceCriteria,
        string scenarioId)
    {
        var serializedCriteria = string.Join(", ", governanceCriteria.Select(criterion => $"\"{criterion}\""));
        var escapedCandidate = JsonEncodedText.Encode(candidateConfigJson).ToString();
        var escapedLogs = JsonEncodedText.Encode(logsCiDirectory).ToString();
        var escapedScenario = JsonEncodedText.Encode(scenarioId).ToString();

        return $$"""
        {
          "scenarioId": "{{escapedScenario}}",
          "target": "gameplay-tuning",
          "scope": "gameplay-tuning",
          "requestedBy": "ci",
          "decision": "reject",
          "governanceCriteria": [ {{serializedCriteria}} ],
          "criteria": [ {{serializedCriteria}} ],
          "candidateConfigJson": "{{escapedCandidate}}",
          "candidateJson": "{{escapedCandidate}}",
          "configJson": "{{escapedCandidate}}",
          "logsCiDirectory": "{{escapedLogs}}"
        }
        """;
    }

    private static GovernanceEvaluationProbeResult ParseGovernanceEvaluation(object? rawResult)
    {
        if (rawResult is null)
        {
            return GovernanceEvaluationProbeResult.Failure("Governance evaluation returned no result.");
        }

        var decision = ReadStringMembers(rawResult, "Decision", "PromotionDecision", "Outcome", "TerminalOutcome");
        if (string.IsNullOrWhiteSpace(decision) && TryReadBoolMember(rawResult, "Accepted", out var accepted))
        {
            decision = accepted ? "allow" : "reject";
        }

        var reasons = ReadStringCollectionMembers(rawResult,
            "ReasonCodes",
            "Reasons",
            "Errors",
            "Diagnostics",
            "EvaluatedReasons",
            "RejectionReasons");

        var singleReason = ReadStringMembers(rawResult,
            "Reason",
            "FailureReason",
            "RejectionReason",
            "Message");
        if (!string.IsNullOrWhiteSpace(singleReason))
        {
            reasons.Add(singleReason);
        }

        if (string.IsNullOrWhiteSpace(decision))
        {
            return GovernanceEvaluationProbeResult.Failure("Governance evaluation result did not expose a machine-readable decision.");
        }

        var normalizedReasons = reasons
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Select(reason => reason.Trim())
            .OrderBy(reason => reason, StringComparer.Ordinal)
            .ToArray();

        return new GovernanceEvaluationProbeResult(
            Success: true,
            Decision: decision.ToLowerInvariant(),
            Reasons: normalizedReasons,
            NormalizedReason: string.Join("|", normalizedReasons),
            FailureMessage: string.Empty);
    }

    private static string ReadStringMembers(object value, params string[] candidateNames)
    {
        foreach (var name in candidateNames)
        {
            if (TryGetPropertyValue(value, name, out var raw) && raw is not null)
            {
                if (raw is string text)
                {
                    return text;
                }

                if (raw is JsonElement element && element.ValueKind == JsonValueKind.String)
                {
                    return element.GetString() ?? string.Empty;
                }
            }
        }

        return string.Empty;
    }

    private static List<string> ReadStringCollectionMembers(object value, params string[] candidateNames)
    {
        foreach (var name in candidateNames)
        {
            if (!TryGetPropertyValue(value, name, out var raw) || raw is null)
            {
                continue;
            }

            if (raw is IEnumerable<string> strings)
            {
                return strings.ToList();
            }

            if (raw is IEnumerable enumerable and not string)
            {
                var values = new List<string>();
                foreach (var item in enumerable)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    if (item is string text)
                    {
                        values.Add(text);
                        continue;
                    }

                    if (item is JsonElement element && element.ValueKind == JsonValueKind.String)
                    {
                        values.Add(element.GetString() ?? string.Empty);
                        continue;
                    }

                    values.Add(item.ToString() ?? string.Empty);
                }

                return values;
            }

            if (raw is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement.EnumerateArray()
                    .Select(element => element.ValueKind == JsonValueKind.String ? element.GetString() ?? string.Empty : element.ToString())
                    .ToList();
            }
        }

        return new List<string>();
    }

    private static bool TryReadBoolMember(object value, string memberName, out bool result)
    {
        result = false;
        if (!TryGetPropertyValue(value, memberName, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is bool boolean)
        {
            result = boolean;
            return true;
        }

        if (raw is JsonElement element && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False))
        {
            result = element.GetBoolean();
            return true;
        }

        return false;
    }

    private static bool TryGetPropertyValue(object value, string propertyName, out object? result)
    {
        result = null;
        var property = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(candidate => string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        if (property is null)
        {
            return false;
        }

        result = property.GetValue(value);
        return true;
    }

    private static bool TryGetStringLike(object value, string propertyName, out string result)
    {
        result = string.Empty;
        if (!TryGetPropertyValue(value, propertyName, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is string text)
        {
            result = text;
            return true;
        }

        if (raw is JsonElement element && element.ValueKind == JsonValueKind.String)
        {
            result = element.GetString() ?? string.Empty;
            return true;
        }

        return false;
    }

    private static bool TryGetDecimalLike(object value, string propertyName, out decimal result)
    {
        result = 0m;
        if (!TryGetPropertyValue(value, propertyName, out var raw) || raw is null)
        {
            return false;
        }

        return TryConvertToDecimal(raw, out result);
    }

    private static bool TryReadJsonString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryReadJsonDecimal(JsonElement element, string propertyName, out decimal value)
    {
        value = 0m;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        return TryConvertToDecimal(property, out value);
    }

    private static bool TryConvertToDecimal(object raw, out decimal value)
    {
        switch (raw)
        {
            case decimal decimalValue:
                value = decimalValue;
                return true;
            case int intValue:
                value = intValue;
                return true;
            case long longValue:
                value = longValue;
                return true;
            case float floatValue:
                value = Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture);
                return true;
            case double doubleValue:
                value = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var decimalFromJson):
                value = decimalFromJson;
                return true;
            case JsonElement element when element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var doubleFromJson):
                value = Convert.ToDecimal(doubleFromJson, CultureInfo.InvariantCulture);
                return true;
            default:
                value = 0m;
                return false;
        }
    }

    private static object? ResolveAwaitable(object? rawResult)
    {
        if (rawResult is not Task task)
        {
            return rawResult;
        }

        task.GetAwaiter().GetResult();
        var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
        return resultProperty?.GetValue(task);
    }

    private static object BuildEnumValue(Type enumType)
    {
        var names = Enum.GetNames(enumType);
        var preferredName = names.FirstOrDefault(name =>
            string.Equals(name, "Reject", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Rejected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Deny", StringComparison.OrdinalIgnoreCase));

        return Enum.Parse(enumType, preferredName ?? names[0], ignoreCase: true);
    }

    private static void LoadCoreAssembly()
    {
        foreach (var assemblyName in new[] { "Game.Core", "Lastking.Game.Core" })
        {
            try
            {
                Assembly.Load(assemblyName);
            }
            catch
            {
            }
        }
    }

    private static Type? FindType(params string[] fullNames)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type => type.FullName is not null && fullNames.Contains(type.FullName, StringComparer.Ordinal));
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    private static string BuildWaveSnapshot(WaveResult waveResult)
    {
        var channelSnapshots = waveResult.ChannelResults
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{BuildChannelSnapshot(pair.Value)}");

        return $"{waveResult.DayIndex}|{waveResult.Seed}|{string.Join("|", channelSnapshots)}";
    }

    private static string BuildChannelSnapshot(ChannelWaveResult channelWaveResult)
    {
        var audit = channelWaveResult.Audit;
        return $"{audit.InputBudget},{audit.Allocated},{audit.Spent},{audit.Remaining}|{string.Join(",", channelWaveResult.SpawnOrder)}";
    }

    private static string BuildElapsedSnapshot(IEnumerable<double> elapsedSeconds)
    {
        return string.Join("|", elapsedSeconds.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)));
    }

    private static string BuildSpacingSnapshot(IEnumerable<double> elapsedSeconds)
    {
        var values = elapsedSeconds.ToArray();
        if (values.Length < 2)
        {
            return string.Empty;
        }

        var spacings = new List<string>(values.Length - 1);
        for (var index = 1; index < values.Length; index++)
        {
            var spacing = values[index] - values[index - 1];
            spacings.Add(spacing.ToString("0.###", CultureInfo.InvariantCulture));
        }

        return string.Join("|", spacings);
    }

    private static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException("Unable to locate repository root from test base directory.");
    }

    private static bool IsProductionSourceFile(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.EndsWith(".cs", StringComparison.Ordinal)
               && !normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/logs/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Game.Core.Tests/", StringComparison.OrdinalIgnoreCase)
               && !normalized.Contains("/Tests.Godot/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGameplayTuningRuntimePath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return !normalized.Contains("/Game.Core/Contracts/", StringComparison.OrdinalIgnoreCase)
               && (normalized.Contains("enemy", StringComparison.OrdinalIgnoreCase)
                   || normalized.Contains("wave", StringComparison.OrdinalIgnoreCase)
                   || normalized.Contains("spawn", StringComparison.OrdinalIgnoreCase)
                   || normalized.Contains("tuning", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsConfigDefinitionFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.EndsWith("Config.cs", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("ConfigManager.cs", StringComparison.OrdinalIgnoreCase)
               || fileName.Equals("ConfigLoadResult.cs", StringComparison.OrdinalIgnoreCase)
               || fileName.StartsWith("BalanceSnapshot", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasConfigManagerToken(string source)
    {
        return source.Contains("ConfigManager", StringComparison.Ordinal)
               || source.Contains("Snapshot", StringComparison.Ordinal)
               || source.Contains("LoadInitialFromJson", StringComparison.Ordinal)
               || source.Contains("ReloadFromJson", StringComparison.Ordinal)
               || source.Contains("GetConfig", StringComparison.Ordinal)
               || source.Contains("TryGet", StringComparison.Ordinal);
    }

    private static bool HasHardcodedTuningOverride(string source)
    {
        var normalized = source.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);

        return normalized.Contains("ChannelLimit:20", StringComparison.Ordinal)
               || normalized.Contains("ChannelLimit:8", StringComparison.Ordinal)
               || normalized.Contains("ChannelLimit:3", StringComparison.Ordinal)
               || normalized.Contains("CostPerEnemy:10", StringComparison.Ordinal)
               || normalized.Contains("CostPerEnemy:20", StringComparison.Ordinal)
               || normalized.Contains("CostPerEnemy:100", StringComparison.Ordinal)
               || normalized.Contains("SpawnCadenceSeconds:10", StringComparison.Ordinal)
               || normalized.Contains("BossCount:2", StringComparison.Ordinal);
    }

    private static bool HasAnyToken(string source, params string[] tokens)
    {
        return tokens.Any(token => source.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string source, params string[] tokens)
    {
        return tokens.Any(token => source.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record GameplayEnemyRuntimeStats(decimal Health, decimal Damage, decimal Speed);

    private sealed record GameplayEnemyRuntimeProbeResult(
        bool Success,
        decimal Health,
        decimal Damage,
        decimal Speed,
        string FailureMessage);

    private sealed record GovernanceEvaluationProbeResult(
        bool Success,
        string Decision,
        IReadOnlyList<string> Reasons,
        string NormalizedReason,
        string FailureMessage)
    {
        public static GovernanceEvaluationProbeResult Failure(string failureMessage)
        {
            return new GovernanceEvaluationProbeResult(
                Success: false,
                Decision: string.Empty,
                Reasons: Array.Empty<string>(),
                NormalizedReason: string.Empty,
                FailureMessage: failureMessage);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; }

        public static TemporaryDirectory Create()
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), "lastking-task38-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directoryPath);
            return new TemporaryDirectory(directoryPath);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
