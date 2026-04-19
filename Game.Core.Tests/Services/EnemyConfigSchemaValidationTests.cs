using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class EnemyConfigSchemaValidationTests
{
    // ACC:T32.1
    // ACC:T32.4
    [Fact]
    public void ShouldDeclareDraft07ObjectRoot_WhenLoadingEnemySchema()
    {
        using var schema = LoadJsonDocument("config/schemas/enemy-config.schema.json");
        var root = schema.RootElement;

        root.GetProperty("$schema").GetString().Should().Be("http://json-schema.org/draft-07/schema#");
        root.GetProperty("type").GetString().Should().Be("object");
    }

    // ACC:T32.5
    [Fact]
    public void ShouldDefineEnemiesArrayItemsAsObject_WhenInspectingEnemyCollectionContract()
    {
        using var schema = LoadJsonDocument("config/schemas/enemy-config.schema.json");
        var enemies = schema.RootElement.GetProperty("properties").GetProperty("enemies");

        enemies.GetProperty("type").GetString().Should().Be("array");
        enemies.GetProperty("items").GetProperty("type").GetString().Should().Be("object");
    }

    // ACC:T32.6
    // ACC:T32.7
    [Fact]
    public void ShouldRequireCoreFieldsWithTypeAndMinimumRules_WhenInspectingEnemyItemContract()
    {
        using var schema = LoadJsonDocument("config/schemas/enemy-config.schema.json");
        var item = schema.RootElement.GetProperty("properties").GetProperty("enemies").GetProperty("items");
        var properties = item.GetProperty("properties");
        var required = item.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToHashSet(StringComparer.Ordinal);

        required.Should().Contain(new[] { "enemy_id", "health", "damage", "speed", "enemy_type", "behavior" });
        properties.GetProperty("enemy_id").GetProperty("type").GetString().Should().Be("string");
        properties.GetProperty("enemy_type").GetProperty("type").GetString().Should().Be("string");
        properties.GetProperty("behavior").GetProperty("type").GetString().Should().Be("object");
        properties.GetProperty("health").GetProperty("minimum").GetDecimal().Should().Be(1m);
        properties.GetProperty("damage").GetProperty("minimum").GetDecimal().Should().Be(0m);
        properties.GetProperty("speed").GetProperty("minimum").GetDecimal().Should().Be(0m);
    }

    // ACC:T32.11
    [Fact]
    public void ShouldRejectPayloadWithoutEnemies_WhenValidatingTopLevelRequiredField()
    {
        const string payload = """{"seed_policy":"deterministic"}""";
        var ok = EnemyConfigSchemaTestSupport.TryValidatePayload(payload, out var reason);

        ok.Should().BeFalse();
        reason.Should().Contain("enemies");
    }

    // ACC:T32.14
    [Theory]
    [InlineData("enemy_id")]
    [InlineData("health")]
    [InlineData("damage")]
    [InlineData("speed")]
    public void ShouldRejectEnemyEntry_WhenAnyRequiredFieldIsMissing(string missingField)
    {
        var payload = BuildEnemyPayloadWithoutRequiredField(missingField);
        var ok = EnemyConfigSchemaTestSupport.TryValidatePayload(payload, out var reason);

        ok.Should().BeFalse();
        reason.Should().Contain(missingField);
    }

    // ACC:T32.8
    // ACC:T32.12
    [Fact]
    public void ShouldRejectPayload_WhenTypesAreInvalidOrNumericValuesAreBelowMinimum()
    {
        const string invalidTypePayload = """
        {
          "seed_policy": "deterministic",
          "enemies": [
            { "enemy_id": "grunt", "enemy_type": "melee", "behavior": "rush", "health": 10, "damage": 2, "speed": 1.5 }
          ]
        }
        """;
        const string invalidMinimumPayload = """
        {
          "seed_policy": "deterministic",
          "enemies": [
            { "enemy_id": "grunt", "enemy_type": "melee", "behavior": { "mode": "rush" }, "health": 0, "damage": -1, "speed": -0.5 }
          ]
        }
        """;

        EnemyConfigSchemaTestSupport.TryValidatePayload(invalidTypePayload, out var typeReason).Should().BeFalse();
        typeReason.Should().Contain("type");
        typeReason.Should().Contain("object");

        EnemyConfigSchemaTestSupport.TryValidatePayload(invalidMinimumPayload, out var minimumReason).Should().BeFalse();
        minimumReason.Should().Contain("minimum");
    }

    [Fact]
    public void ShouldRejectPayload_WhenTopLevelAdditionalPropertiesExist()
    {
        const string payload = """
        {
          "seed_policy": "deterministic",
          "enemies": [
            { "enemy_id": "grunt", "enemy_type": "melee", "behavior": { "mode": "rush" }, "health": 10, "damage": 2, "speed": 1.2 }
          ],
          "unexpected": true
        }
        """;

        EnemyConfigSchemaTestSupport.TryValidatePayload(payload, out var reason).Should().BeFalse();
        reason.Should().Contain("false schema");
    }

    [Fact]
    public void ShouldRejectPayload_WhenEnemyItemAdditionalPropertiesExist()
    {
        const string payload = """
        {
          "seed_policy": "deterministic",
          "enemies": [
            { "enemy_id": "grunt", "enemy_type": "melee", "behavior": { "mode": "rush" }, "health": 10, "damage": 2, "speed": 1.2, "extra": 1 }
          ]
        }
        """;

        EnemyConfigSchemaTestSupport.TryValidatePayload(payload, out var reason).Should().BeFalse();
        reason.Should().Contain("false schema");
    }

    [Fact]
    public void ShouldRejectPayload_WhenSeedPolicyIsNotDeterministic()
    {
        const string payload = """
        {
          "seed_policy": "random",
          "enemies": [
            { "enemy_id": "grunt", "enemy_type": "melee", "behavior": { "mode": "rush" }, "health": 10, "damage": 2, "speed": 1.2 }
          ]
        }
        """;

        EnemyConfigSchemaTestSupport.TryValidatePayload(payload, out var reason).Should().BeFalse();
        reason.Should().Contain("enum");
    }

    [Fact]
    public void ShouldRejectPayload_WhenEnemiesArrayIsEmpty()
    {
        const string payload = """
        {
          "seed_policy": "deterministic",
          "enemies": []
        }
        """;

        EnemyConfigSchemaTestSupport.TryValidatePayload(payload, out var reason).Should().BeFalse();
        reason.Should().Contain("minItems");
    }

    // ACC:T32.2
    // ACC:T32.9
    // ACC:T32.10
    [Fact]
    public void ShouldAcceptValidPayloadAndAllowBalanceAdjustments_WhenWithinSchemaConstraints()
    {
        const string firstPayload = """
        {
          "seed_policy": "deterministic",
          "enemies": [
            { "enemy_id": "grunt", "enemy_type": "melee", "behavior": { "mode": "rush" }, "health": 12, "damage": 3, "speed": 1.2 }
          ]
        }
        """;
        const string secondPayload = """
        {
          "seed_policy": "deterministic",
          "enemies": [
            { "enemy_id": "grunt", "enemy_type": "melee", "behavior": { "mode": "rush" }, "health": 18, "damage": 5, "speed": 1.8 }
          ]
        }
        """;

        EnemyConfigSchemaTestSupport.TryValidatePayload(firstPayload, out var firstReason).Should().BeTrue(firstReason);
        EnemyConfigSchemaTestSupport.TryValidatePayload(secondPayload, out var secondReason).Should().BeTrue(secondReason);
    }

    private static string BuildEnemyPayloadWithoutRequiredField(string missingField)
    {
        var enemyFields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["enemy_id"] = "\"enemy_id\": \"grunt\"",
            ["enemy_type"] = "\"enemy_type\": \"melee\"",
            ["behavior"] = "\"behavior\": { \"mode\": \"rush\" }",
            ["health"] = "\"health\": 10",
            ["damage"] = "\"damage\": 2",
            ["speed"] = "\"speed\": 1.2",
        };

        enemyFields.Remove(missingField);
        var enemyJson = string.Join(", ", enemyFields.Values);
        return "{" +
               "\"seed_policy\": \"deterministic\", " +
               "\"enemies\": [ { " + enemyJson + " } ]" +
               "}";
    }

    private static JsonDocument LoadJsonDocument(string relativePath)
    {
        var fullPath = Path.Combine(
            EnemyConfigSchemaTestSupport.ResolveRepoRoot().FullName,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        var json = File.ReadAllText(fullPath);
        return JsonDocument.Parse(json);
    }
}
