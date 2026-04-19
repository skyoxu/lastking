using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Json.Schema;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class DifficultyConfigSchemaValidationTests
{
    private static readonly object SchemaEvaluationGate = new();

    // ACC:T33.23
    [Fact]
    public void ShouldRejectWithExplicitReason_WhenDifficultyLevelEnumIsInvalid()
    {
        var payload = BuildValidPayload();
        payload["difficulty_level"] = "nightmare";

        var payloadJson = JsonSerializer.Serialize(payload);
        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().Contain("enum");
    }

    [Theory]
    [InlineData("version")]
    [InlineData("difficulty_level")]
    [InlineData("modifiers")]
    public void ShouldRejectWithExplicitReason_WhenRequiredFieldIsMissing(string missingField)
    {
        var payload = BuildValidPayload();
        payload.Remove(missingField);

        var payloadJson = JsonSerializer.Serialize(payload);
        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().Contain("required");
        reason.Should().Contain(missingField);
    }

    [Fact]
    public void ShouldRejectWithExplicitReason_WhenModifiersTypeDoesNotMatchSchema()
    {
        var payload = BuildValidPayload();
        payload["modifiers"] = "not-an-object";

        var payloadJson = JsonSerializer.Serialize(payload);
        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().ContainAny("type", "false schema");
    }

    [Fact]
    public void ShouldRejectWithExplicitReason_WhenDeclaredNumericBoundaryIsViolated()
    {
        var payload = BuildValidPayload();
        var modifiers = (Dictionary<string, object?>)payload["modifiers"]!;
        modifiers["enemy_hp_mult"] = 0;

        var payloadJson = JsonSerializer.Serialize(payload);
        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().Contain("minimum");
    }

    private static Dictionary<string, object?> BuildValidPayload()
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["version"] = "1.0",
            ["difficulty_level"] = "medium",
            ["modifiers"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["enemy_hp_mult"] = 1.0,
                ["enemy_dmg_mult"] = 1.0,
                ["resource_mult"] = 1.0,
                ["budget_mult_normal"] = 1.0,
                ["budget_mult_elite"] = 1.0,
                ["budget_mult_boss"] = 1.0,
            },
        };
    }

    private static bool TryValidatePayload(string payloadJson, out string reason)
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
            lock (SchemaEvaluationGate)
            {
                var schema = LoadDifficultySchema();
                var result = schema.Evaluate(payload.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
                if (result.IsValid)
                {
                    reason = string.Empty;
                    return true;
                }

                reason = BuildFailureReason(result);
                return false;
            }
        }
    }

    private static JsonSchema LoadDifficultySchema()
    {
        var repoRoot = ResolveRepoRoot();
        var schemaPath = Path.Combine(repoRoot.FullName, "config", "schemas", "difficulty-config.schema.json");
        var schemaJson = File.ReadAllText(schemaPath);
        var baseUri = new Uri("urn:lastking:test:difficulty-config-schema");
        var buildOptions = new BuildOptions
        {
            SchemaRegistry = new SchemaRegistry(),
        };
        return JsonSchema.FromText(schemaJson, buildOptions: buildOptions, baseUri: baseUri);
    }

    private static DirectoryInfo ResolveRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Game.sln")))
            {
                return dir;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Cannot resolve repository root from test runtime.");
    }

    private static string BuildFailureReason(EvaluationResults root)
    {
        return EnemyConfigSchemaTestSupport.BuildFailureReason(root);
    }

    private static IEnumerable<EvaluationResults> FlattenResults(EvaluationResults root)
    {
        yield return root;
        if (root.Details is null)
        {
            yield break;
        }

        foreach (var child in root.Details)
        {
            foreach (var nested in FlattenResults(child))
            {
                yield return nested;
            }
        }
    }

    private static void ResetGlobalSchemaRegistryForTests()
    {
        var global = SchemaRegistry.Global;
        var field = global.GetType().GetField("_registered", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null)
        {
            return;
        }

        var registered = field.GetValue(global);
        if (registered is null)
        {
            return;
        }

        var clearMethod = registered.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
        clearMethod?.Invoke(registered, null);
    }
}
