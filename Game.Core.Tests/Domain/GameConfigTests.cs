using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Game.Core.Domain;
using Xunit;

namespace Game.Core.Tests.Domain;

public class GameConfigTests
{
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
