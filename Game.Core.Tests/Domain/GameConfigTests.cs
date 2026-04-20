using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Core.Tests.Services;
using Json.Schema;
using Xunit;

namespace Game.Core.Tests.Domain;

public class GameConfigTests
{
    // ACC:T2.16
    // ACC:T19.13
    [Fact]
    public void ShouldSetPropertiesAsExpected_WhenConstructed()
    {
        // Arrange
        var config = new GameConfig(
            MaxLevel: 10,
            InitialHealth: 100,
            ScoreMultiplier: 2.5,
            AutoSave: true,
            Difficulty: Difficulty.Hard
        );

        // Act & Assert
        config.MaxLevel.Should().Be(10);
        config.InitialHealth.Should().Be(100);
        config.ScoreMultiplier.Should().Be(2.5);
        config.AutoSave.Should().BeTrue();
        config.Difficulty.Should().Be(Difficulty.Hard);
    }

    // ACC:T2.16
    // ACC:T31.9
    [Fact]
    public void ShouldExposeTypedBalanceSnapshotWithDeterministicDefaults_WhenOptionalFieldsAreMissing()
    {
        var manager = new ConfigManager();
        const string json = """
                            {
                              "time": { "day_seconds": 240, "night_seconds": 120 },
                              "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                              "channels": { "elite": "elite", "boss": "boss" }
                            }
                            """;

        var result = manager.LoadInitialFromJson(json, "res://Config/balance.json");

        result.Accepted.Should().BeTrue();
        result.Source.Should().Be("initial");
        result.Snapshot.DaySeconds.Should().Be(240);
        result.Snapshot.NightSeconds.Should().Be(120);
        result.Snapshot.Day1Budget.Should().Be(50);
        result.Snapshot.DailyGrowth.Should().Be(1.2m);
        result.Snapshot.EliteChannel.Should().Be("elite");
        result.Snapshot.BossChannel.Should().Be("boss");
        result.Snapshot.SpawnCadenceSeconds.Should().Be(10);
        result.Snapshot.BossCount.Should().Be(2);
    }

    // ACC:T2.2
    // ACC:T37.2
    // ACC:T31.13
    // ACC:T34.1
    // ACC:T34.2
    [Fact]
    public void ShouldKeepLoadReloadFallbackOrderDeterministic_WhenAppliedInSequence()
    {
        var manager = new ConfigManager();
        const string initialJson = """
                                   {
                                     "time": { "day_seconds": 240, "night_seconds": 120 },
                                     "waves": { "normal": { "day1_budget": 50, "daily_growth": 1.2 } },
                                     "channels": { "elite": "elite", "boss": "boss" },
                                     "spawn": { "cadence_seconds": 10 },
                                     "boss": { "count": 2 }
                                   }
                                   """;
        const string reloadJson = """
                                  {
                                    "time": { "day_seconds": 180, "night_seconds": 90 },
                                    "waves": { "normal": { "day1_budget": 75, "daily_growth": 1.3 } },
                                    "channels": { "elite": "elite", "boss": "boss" },
                                    "spawn": { "cadence_seconds": 8 },
                                    "boss": { "count": 2 }
                                  }
                                  """;
        const string brokenJson = "{ invalid_payload";

        var first = manager.LoadInitialFromJson(initialJson, "res://Config/balance.json");
        var second = manager.ReloadFromJson(reloadJson, "res://Config/balance.json");
        var third = manager.ReloadFromJson(brokenJson, "res://Config/balance.json");

        first.Source.Should().Be("initial");
        second.Source.Should().Be("reload");
        third.Source.Should().Be("fallback");
        third.Snapshot.Should().Be(second.Snapshot);
        third.ReasonCodes.Should().Contain(ConfigManager.ParseErrorReason);
    }

    [Fact]
    public void ShouldFailOrderValidation_WhenReloadAppearsBeforeInitialLoad()
    {
        var manager = new ConfigManager();
        const string reloadJson = """
                                  {
                                    "time": { "day_seconds": 180, "night_seconds": 90 },
                                    "waves": { "normal": { "day1_budget": 75, "daily_growth": 1.3 } },
                                    "channels": { "elite": "elite", "boss": "boss" },
                                    "spawn": { "cadence_seconds": 8 },
                                    "boss": { "count": 2 }
                                  }
                                  """;

        var first = manager.ReloadFromJson(reloadJson, "res://Config/balance.json");

        first.Accepted.Should().BeFalse();
        first.Source.Should().Be("fallback");
        first.ReasonCodes.Should().Contain(ConfigManager.InvalidOrderReason);
    }

    // ACC:T35.1
    [Fact]
    public void ShouldExposePressureNormalizationSchemaContractKeywords_WhenInspectingTask35SchemaFile()
    {
        var schemaPath = Path.Combine(
            EnemyConfigSchemaTestSupport.ResolveRepoRoot().FullName,
            "config",
            "schemas",
            "pressure-normalization.config.schema.json");
        var schemaJson = File.ReadAllText(schemaPath);
        using var schemaDocument = JsonDocument.Parse(schemaJson);
        var root = schemaDocument.RootElement;
        var properties = root.GetProperty("properties");
        var required = root.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToHashSet(StringComparer.Ordinal);

        required.Should().Contain(new[] { "baseline", "min_pressure", "max_pressure", "normalization_factors" });
        properties.GetProperty("baseline").GetProperty("minimum").GetDouble().Should().Be(0d);
        root.GetProperty("x-range-check").GetString().Should().Be("min_pressure < max_pressure");
    }

    // ACC:T11.11
    [Fact]
    public void ShouldDeserializeFromJsonWithoutErrors_WhenConfigurationPayloadProvided()
    {
        // Arrange
        const string json = """
                            {
                              "MaxLevel": 10,
                              "InitialHealth": 100,
                              "ScoreMultiplier": 2.5,
                              "AutoSave": true,
                              "Difficulty": "Hard"
                            }
                            """;
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());

        // Act
        var config = JsonSerializer.Deserialize<GameConfig>(json, options);

        // Assert
        config.Should().NotBeNull();
        config!.MaxLevel.Should().Be(10);
        config.InitialHealth.Should().Be(100);
        config.ScoreMultiplier.Should().Be(2.5);
        config.AutoSave.Should().BeTrue();
        config.Difficulty.Should().Be(Difficulty.Hard);
    }

    [Fact]
    public void ShouldRejectInvalidDifficultyValue_WhenConfigurationPayloadIsNotSupported()
    {
        // Arrange
        const string json = """
                            {
                              "MaxLevel": 10,
                              "InitialHealth": 100,
                              "ScoreMultiplier": 2.5,
                              "AutoSave": true,
                              "Difficulty": "Impossible"
                            }
                            """;
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());

        // Act
        var act = () => JsonSerializer.Deserialize<GameConfig>(json, options);

        // Assert
        act.Should().Throw<JsonException>();
    }

    // ACC:T34.4
    [Fact]
    public void ShouldDeclareSeedAndWavesAsRequiredTopLevelFields_WhenInspectingSpawnSchemaContract()
    {
        using var schema = LoadSpawnSchemaDocument();
        var root = schema.RootElement;
        var required = root.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToHashSet(StringComparer.Ordinal);

        root.GetProperty("type").GetString().Should().Be("object");
        required.Should().Contain(new[] { "seed", "waves" });
        root.GetProperty("properties").GetProperty("seed").GetProperty("type").GetString().Should().Be("integer");
        root.GetProperty("properties").GetProperty("waves").GetProperty("type").GetString().Should().Be("array");
    }

    // ACC:T34.5
    // ACC:T34.6
    [Fact]
    public void ShouldConstrainWaveDayAndEnemyShape_WhenInspectingSpawnSchemaRules()
    {
        using var schema = LoadSpawnSchemaDocument();
        var waveItem = schema.RootElement
            .GetProperty("properties")
            .GetProperty("waves")
            .GetProperty("items");

        var day = waveItem.GetProperty("properties").GetProperty("day");
        day.GetProperty("type").GetString().Should().Be("integer");
        day.GetProperty("minimum").GetDecimal().Should().Be(1m);
        day.GetProperty("maximum").GetDecimal().Should().Be(15m);

        var enemyItem = waveItem
            .GetProperty("properties")
            .GetProperty("enemies")
            .GetProperty("items");
        enemyItem.GetProperty("properties").GetProperty("enemy_id").GetProperty("type").GetString().Should().Be("string");

        var count = enemyItem.GetProperty("properties").GetProperty("count");
        count.GetProperty("type").GetString().Should().Be("integer");
        count.GetProperty("minimum").GetDecimal().Should().Be(0m);
    }

    // ACC:T34.7
    [Fact]
    public void ShouldAcceptValidPayloadAndRejectOutOfRangeDayOrNegativeCount_WhenValidatingSpawnSchema()
    {
        const string validPayload = """
                                    {
                                      "seed": 34,
                                      "waves": [
                                        {
                                          "day": 1,
                                          "enemies": [
                                            { "enemy_id": "slime", "count": 2 }
                                          ]
                                        }
                                      ]
                                    }
                                    """;
        const string invalidDayPayload = """
                                         {
                                           "seed": 34,
                                           "waves": [
                                             {
                                               "day": 16,
                                               "enemies": [
                                                 { "enemy_id": "slime", "count": 2 }
                                               ]
                                             }
                                           ]
                                         }
                                         """;
        const string invalidCountPayload = """
                                           {
                                             "seed": 34,
                                             "waves": [
                                               {
                                                 "day": 1,
                                                 "enemies": [
                                                   { "enemy_id": "slime", "count": -1 }
                                                 ]
                                               }
                                             ]
                                           }
                                           """;

        TryValidateSpawnPayload(validPayload, out var validReason).Should().BeTrue(validReason);

        TryValidateSpawnPayload(invalidDayPayload, out var invalidDayReason).Should().BeFalse();
        invalidDayReason.Should().ContainAny("maximum", "16");

        TryValidateSpawnPayload(invalidCountPayload, out var invalidCountReason).Should().BeFalse();
        invalidCountReason.Should().ContainAny("minimum", "-1");
    }

    // ACC:T34.8
    [Fact]
    public void ShouldRejectPayload_WhenSeedOrWavesUsesInvalidType()
    {
        const string invalidSeedTypePayload = """
                                              {
                                                "seed": "34",
                                                "waves": [
                                                  {
                                                    "day": 1,
                                                    "enemies": [
                                                      { "enemy_id": "slime", "count": 2 }
                                                    ]
                                                  }
                                                ]
                                              }
                                              """;
        const string invalidWavesTypePayload = """
                                               {
                                                 "seed": 34,
                                                 "waves": {
                                                   "day": 1
                                                 }
                                               }
                                               """;
        const string invalidEnemyIdTypePayload = """
                                                 {
                                                   "seed": 34,
                                                   "waves": [
                                                     {
                                                       "day": 1,
                                                       "enemies": [
                                                         { "enemy_id": 123, "count": 2 }
                                                       ]
                                                     }
                                                   ]
                                                 }
                                                 """;
        const string invalidEnemyCountTypePayload = """
                                                    {
                                                      "seed": 34,
                                                      "waves": [
                                                        {
                                                          "day": 1,
                                                          "enemies": [
                                                            { "enemy_id": "slime", "count": "2" }
                                                          ]
                                                        }
                                                      ]
                                                    }
                                                    """;

        TryValidateSpawnPayload(invalidSeedTypePayload, out var invalidSeedReason).Should().BeFalse();
        invalidSeedReason.Should().ContainAny("type", "integer");

        TryValidateSpawnPayload(invalidWavesTypePayload, out var invalidWavesReason).Should().BeFalse();
        invalidWavesReason.Should().ContainAny("type", "array");

        TryValidateSpawnPayload(invalidEnemyIdTypePayload, out var invalidEnemyIdReason).Should().BeFalse();
        invalidEnemyIdReason.Should().ContainAny("type", "string");

        TryValidateSpawnPayload(invalidEnemyCountTypePayload, out var invalidEnemyCountReason).Should().BeFalse();
        invalidEnemyCountReason.Should().ContainAny("type", "integer");
    }

    // ACC:T34.9
    [Fact]
    public void ShouldRejectUnknownOverrideFields_WhenValidatingSpawnSchemaInputs()
    {
        const string payloadWithHiddenOverride = """
                                                 {
                                                   "seed": 34,
                                                   "hidden_override": true,
                                                   "waves": [
                                                     {
                                                       "day": 1,
                                                       "enemies": [
                                                         { "enemy_id": "slime", "count": 2 }
                                                       ]
                                                     }
                                                   ]
                                                 }
                                                 """;

        TryValidateSpawnPayload(payloadWithHiddenOverride, out var reason).Should().BeFalse();
        reason.Should().Contain("false schema");
    }

    // ACC:T34.1
    // ACC:T34.3
    [Fact]
    public void ShouldKeepTimelineStableForSameSeedAndChangeForDifferentSeed_WhenWaveInputsStaySame()
    {
        var sut = new WaveManager();
        var config = new ChannelBudgetConfiguration(
            Normal: new ChannelRule(Day1Budget: 50, DailyGrowth: 1.2m, ChannelLimit: 20, CostPerEnemy: 10),
            Elite: new ChannelRule(Day1Budget: 120, DailyGrowth: 1.2m, ChannelLimit: 8, CostPerEnemy: 20),
            Boss: new ChannelRule(Day1Budget: 300, DailyGrowth: 1.2m, ChannelLimit: 3, CostPerEnemy: 100));

        var sameSeedFirst = sut.Generate(dayIndex: 2, channelBudgetConfiguration: config, seed: 34);
        var sameSeedSecond = sut.Generate(dayIndex: 2, channelBudgetConfiguration: config, seed: 34);
        var changedSeed = sut.Generate(dayIndex: 2, channelBudgetConfiguration: config, seed: 35);

        BuildWaveSnapshot(sameSeedSecond).Should().Be(BuildWaveSnapshot(sameSeedFirst));
        BuildWaveSnapshot(changedSeed).Should().NotBe(BuildWaveSnapshot(sameSeedFirst));
    }

    // ACC:T34.7
    [Fact]
    public void ShouldRejectInvalidConfigAndEmitNoTimeline_WhenSchemaValidationFailsBeforeGeneration()
    {
        const string invalidPayload = """
                                      {
                                        "seed": 34,
                                        "waves": [
                                          {
                                            "day": 16,
                                            "enemies": [
                                              { "enemy_id": "slime", "count": -1 }
                                            ]
                                          }
                                        ]
                                      }
                                      """;
        var sut = new WaveManager();
        var config = new ChannelBudgetConfiguration(
            Normal: new ChannelRule(Day1Budget: 50, DailyGrowth: 1.2m, ChannelLimit: 20, CostPerEnemy: 10),
            Elite: new ChannelRule(Day1Budget: 120, DailyGrowth: 1.2m, ChannelLimit: 8, CostPerEnemy: 20),
            Boss: new ChannelRule(Day1Budget: 300, DailyGrowth: 1.2m, ChannelLimit: 3, CostPerEnemy: 100));

        TryValidateSpawnPayload(invalidPayload, out var reason).Should().BeFalse();
        reason.Should().ContainAny("maximum", "minimum");

        var timeline = TryGenerateWaveTimelineWhenValid(
            payloadJson: invalidPayload,
            waveManager: sut,
            channelBudgetConfiguration: config,
            generated: out var generated,
            generatedWave: out var generatedWave);

        timeline.Should().BeEmpty();
        generated.Should().BeFalse();
        generatedWave.Should().BeNull();
    }

    private static bool TryValidateSpawnPayload(string payloadJson, out string reason)
    {
        JsonDocument payload;
        try
        {
            payload = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException ex)
        {
            reason = ex.Message;
            return false;
        }

        using (payload)
        {
            var schema = LoadSpawnSchema();
            var result = schema.Evaluate(payload.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
            if (result.IsValid)
            {
                reason = string.Empty;
                return true;
            }

            reason = EnemyConfigSchemaTestSupport.BuildFailureReason(result);
            return false;
        }
    }

    private static JsonSchema LoadSpawnSchema()
    {
        var schemaPath = Path.Combine(
            EnemyConfigSchemaTestSupport.ResolveRepoRoot().FullName,
            "config",
            "schemas",
            "spawn-config.schema.json");
        var schemaJson = File.ReadAllText(schemaPath);
        var buildOptions = new BuildOptions
        {
            SchemaRegistry = new SchemaRegistry(),
        };
        return JsonSchema.FromText(schemaJson, buildOptions: buildOptions, baseUri: new Uri("urn:lastking:test:spawn-config-schema"));
    }

    private static JsonDocument LoadSpawnSchemaDocument()
    {
        var schemaPath = Path.Combine(
            EnemyConfigSchemaTestSupport.ResolveRepoRoot().FullName,
            "config",
            "schemas",
            "spawn-config.schema.json");
        var text = File.ReadAllText(schemaPath);
        return JsonDocument.Parse(text);
    }

    private static IReadOnlyList<string> TryGenerateWaveTimelineWhenValid(
        string payloadJson,
        WaveManager waveManager,
        ChannelBudgetConfiguration channelBudgetConfiguration,
        out bool generated,
        out WaveResult? generatedWave)
    {
        generated = false;
        generatedWave = null;

        if (!TryValidateSpawnPayload(payloadJson, out _))
        {
            return Array.Empty<string>();
        }

        using var payload = JsonDocument.Parse(payloadJson);
        var root = payload.RootElement;
        var seed = root.GetProperty("seed").GetInt32();
        var firstWave = root.GetProperty("waves").EnumerateArray().First();
        var day = firstWave.GetProperty("day").GetInt32();

        generatedWave = waveManager.Generate(day, channelBudgetConfiguration, seed);
        generated = true;
        return generatedWave.ChannelResults
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{string.Join(",", pair.Value.SpawnOrder)}")
            .ToArray();
    }

    private static string BuildWaveSnapshot(WaveResult waveResult)
    {
        var channelSnapshots = waveResult.ChannelResults
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
            {
                var audit = pair.Value.Audit;
                return $"{pair.Key}:{audit.InputBudget},{audit.Allocated},{audit.Spent},{audit.Remaining}|{string.Join(",", pair.Value.SpawnOrder)}";
            });
        return $"{waveResult.DayIndex}|{waveResult.Seed}|{string.Join("|", channelSnapshots)}";
    }

}
