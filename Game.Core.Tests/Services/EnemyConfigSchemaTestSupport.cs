using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Reflection;
using Json.Schema;

namespace Game.Core.Tests.Services;

internal static class EnemyConfigSchemaTestSupport
{
    private static readonly object SchemaEvaluationGate = new();

    public static bool TryValidatePayload(string payloadJson, out string reason)
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
            return TryValidatePayload(payload.RootElement, out reason);
        }
    }

    public static bool TryValidatePayload(JsonElement payload, out string reason)
    {
        lock (SchemaEvaluationGate)
        {
            var schema = LoadEnemySchema();
            var result = schema.Evaluate(payload, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
            if (result.IsValid)
            {
                reason = string.Empty;
                return true;
            }

            reason = BuildFailureReason(result);
            return false;
        }
    }

    public static JsonSchema LoadEnemySchema()
    {
        ResetGlobalSchemaRegistryForTests();
        var schemaPath = Path.Combine(
            ResolveRepoRoot().FullName,
            "config",
            "schemas",
            "enemy-config.schema.json");
        var schemaJson = File.ReadAllText(schemaPath);
        return JsonSchema.FromText(schemaJson);
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

    public static DirectoryInfo ResolveRepoRoot()
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

    public static string BuildFailureReason(EvaluationResults root)
    {
        var nodes = FlattenResults(root).ToList();
        var firstWithError = nodes.FirstOrDefault(node => node.Errors is { Count: > 0 });
        if (firstWithError is not null && firstWithError.Errors is not null)
        {
            var firstError = firstWithError.Errors.First();
            return $"{firstError.Key}: {firstError.Value}";
        }

        var firstInvalidNode = nodes.FirstOrDefault(node => !node.IsValid);
        if (firstInvalidNode is not null)
        {
            return $"invalid at {firstInvalidNode.EvaluationPath}";
        }

        return "schema validation failed";
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
}
