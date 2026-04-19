using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Game.Core.Domain;
using Game.Core.Services;
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
    // ACC:T31.13
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

}
