using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Json.Schema;
using Xunit;
using Game.Core.Tests.Services;

namespace Game.Core.Tests.Domain;

public sealed class DifficultyConfigSchemaTests
{
    private static readonly object SchemaEvaluationGate = new();

    // ACC:T33.2
    // ACC:T33.4
    [Fact]
    public void ShouldParseSchemaFile_WhenDifficultySchemaExistsOnDisk()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/difficulty-config.schema.json");
        schemaDocument.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    // ACC:T33.5
    // ACC:T33.21
    [Fact]
    public void ShouldRequireVersionAndDifficultyLevel_WhenValidatingMissingRequiredFields()
    {
        var modifiersObjectToken = BuildModifiersObjectToken();
        var missingVersionPayload = "{ \"difficulty_level\": \"easy\", \"modifiers\": " + modifiersObjectToken + " }";
        var missingDifficultyPayload = "{ \"version\": \"1.0\", \"modifiers\": " + modifiersObjectToken + " }";

        TryValidatePayload(missingVersionPayload, out var missingVersionReason).Should().BeFalse();
        missingVersionReason.Should().Contain("version");

        TryValidatePayload(missingDifficultyPayload, out var missingDifficultyReason).Should().BeFalse();
        missingDifficultyReason.Should().Contain("difficulty_level");
    }

    // ACC:T33.6
    [Theory]
    [InlineData("\"1.0\"", true)]
    [InlineData("\"v1\"", false)]
    [InlineData("1", false)]
    public void ShouldMatchVersionPattern_WhenValidatingVersionValue(string versionToken, bool expectedValid)
    {
        var payload = BuildPayload(versionToken, "\"medium\"");

        var isValid = TryValidatePayload(payload, out var reason);

        isValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            reason.Should().NotBeNullOrWhiteSpace();
        }
    }

    // ACC:T33.7
    [Theory]
    [InlineData("\"easy\"", true)]
    [InlineData("\"medium\"", true)]
    [InlineData("\"hard\"", true)]
    [InlineData("\"nightmare\"", false)]
    public void ShouldAllowOnlyKnownDifficultyLevels_WhenValidatingDifficultyEnum(string difficultyLevelToken, bool expectedValid)
    {
        var payload = BuildPayload("\"1.0\"", difficultyLevelToken);

        var isValid = TryValidatePayload(payload, out var reason);

        isValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            reason.Should().NotBeNullOrWhiteSpace();
        }
    }

    // ACC:T33.8
    // ACC:T33.12
    [Fact]
    public void ShouldDeclareModifiersAsNumericObjectFields_WhenInspectingSchemaDefinition()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/difficulty-config.schema.json");
        var modifiers = schemaDocument.RootElement.GetProperty("properties").GetProperty("modifiers");
        var modifierProperties = modifiers.GetProperty("properties").EnumerateObject().ToList();

        modifiers.GetProperty("type").GetString().Should().Be("object");
        modifierProperties.Should().NotBeEmpty();

        foreach (var modifierProperty in modifierProperties)
        {
            var type = modifierProperty.Value.GetProperty("type").GetString();
            type.Should().BeOneOf("number", "integer");
        }
    }

    // ACC:T33.14
    [Fact]
    public void ShouldRejectModifierValue_WhenAnyDeclaredModifierIsNonNumeric()
    {
        var modifierFieldName = GetDeclaredModifierFieldNames().First();
        var modifierOverrides = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [modifierFieldName] = "\"non_numeric\"",
        };
        var payload = BuildPayload("\"1.0\"", "\"easy\"", modifierOverrides);

        var isValid = TryValidatePayload(payload, out var reason);

        isValid.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T33.15
    // ACC:T33.16
    // ACC:T33.17
    // ACC:T33.22
    [Fact]
    public void ShouldExposeVersionAsRequiredTopLevelMetadata_WhenInspectingSchemaContract()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/difficulty-config.schema.json");
        var root = schemaDocument.RootElement;
        var properties = root.GetProperty("properties");
        var required = root.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToHashSet(StringComparer.Ordinal);

        required.Should().Contain("version");
        required.Should().Contain("difficulty_level");

        var versionProperty = properties.GetProperty("version");
        versionProperty.GetProperty("type").GetString().Should().Be("string");
        versionProperty.GetProperty("pattern").GetString().Should().Be(@"^\d+\.\d+$");
    }

    // ACC:T33.10
    // ACC:T33.11
    // ACC:T33.20
    [Fact]
    public void ShouldReportExpectedOutcomes_WhenRunningPositiveAndNegativeValidationMatrix()
    {
        var modifiersObjectToken = BuildModifiersObjectToken();
        var validPayload = BuildPayload("\"1.0\"", "\"easy\"");
        var invalidDifficultyPayload = BuildPayload("\"1.0\"", "\"legendary\"");
        var missingVersionPayload = "{ \"difficulty_level\": \"easy\", \"modifiers\": " + modifiersObjectToken + " }";
        var invalidVersionPatternPayload = BuildPayload("\"v1\"", "\"easy\"");

        TryValidatePayload(validPayload, out var validReason).Should().BeTrue(validReason);
        TryValidatePayload(invalidDifficultyPayload, out var invalidDifficultyReason).Should().BeFalse();
        TryValidatePayload(missingVersionPayload, out var missingVersionReason).Should().BeFalse();
        TryValidatePayload(invalidVersionPatternPayload, out var invalidVersionPatternReason).Should().BeFalse();

        invalidDifficultyReason.Should().NotBeNullOrWhiteSpace();
        missingVersionReason.Should().NotBeNullOrWhiteSpace();
        invalidVersionPatternReason.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T33.18
    // ACC:T33.19
    [Fact]
    public void ShouldAcceptTwoConfigurations_WhenOnlyVersionValueChangesWithinPattern()
    {
        var firstPayload = BuildPayload("\"1.0\"", "\"medium\"");
        var secondPayload = BuildPayload("\"2.1\"", "\"medium\"");

        TryValidatePayload(firstPayload, out var firstReason).Should().BeTrue(firstReason);
        TryValidatePayload(secondPayload, out var secondReason).Should().BeTrue(secondReason);
    }

    // ACC:T33.18
    // ACC:T33.19
    [Fact]
    public void ShouldNotLockVersionToSingleConstant_WhenInspectingVersionSchemaNode()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/difficulty-config.schema.json");
        var versionProperty = schemaDocument.RootElement
            .GetProperty("properties")
            .GetProperty("version");

        versionProperty.TryGetProperty("const", out _).Should().BeFalse();

        if (versionProperty.TryGetProperty("enum", out var enumElement))
        {
            enumElement.ValueKind.Should().Be(JsonValueKind.Array);
            enumElement.GetArrayLength().Should().BeGreaterThan(1);
        }
    }

    private static string BuildPayload(
        string versionToken,
        string difficultyLevelToken,
        IReadOnlyDictionary<string, string>? modifierOverrides = null)
    {
        var modifiersObjectToken = BuildModifiersObjectToken(modifierOverrides);
        return "{ " +
               "\"version\": " + versionToken + ", " +
               "\"difficulty_level\": " + difficultyLevelToken + ", " +
               "\"modifiers\": " + modifiersObjectToken +
               " }";
    }

    private static string BuildModifiersObjectToken(IReadOnlyDictionary<string, string>? modifierOverrides = null)
    {
        var modifierFieldNames = GetDeclaredModifierFieldNames();
        if (modifierFieldNames.Count == 0)
        {
            throw new InvalidOperationException("Schema must declare at least one modifier field.");
        }

        var modifierEntries = new List<string>(modifierFieldNames.Count);
        foreach (var modifierFieldName in modifierFieldNames)
        {
            var valueToken = "1.0";
            if (modifierOverrides is not null && modifierOverrides.TryGetValue(modifierFieldName, out var overrideValueToken))
            {
                valueToken = overrideValueToken;
            }

            modifierEntries.Add("\"" + modifierFieldName + "\": " + valueToken);
        }

        return "{ " + string.Join(", ", modifierEntries) + " }";
    }

    private static IReadOnlyList<string> GetDeclaredModifierFieldNames()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/difficulty-config.schema.json");
        return schemaDocument
            .RootElement
            .GetProperty("properties")
            .GetProperty("modifiers")
            .GetProperty("properties")
            .EnumerateObject()
            .Select(x => x.Name)
            .ToList();
    }

    private static bool TryValidatePayload(string payloadJson, out string reason)
    {
        JsonDocument payloadDocument;
        try
        {
            payloadDocument = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException ex)
        {
            reason = ex.Message;
            return false;
        }

        using (payloadDocument)
        {
            lock (SchemaEvaluationGate)
            {
                var schema = LoadDifficultySchema();
                var result = schema.Evaluate(payloadDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
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
        var schemaPath = Path.Combine(
            ResolveRepoRoot().FullName,
            "config",
            "schemas",
            "difficulty-config.schema.json");
        var schemaText = File.ReadAllText(schemaPath);
        var baseUri = new Uri("urn:lastking:test:difficulty-config-schema");
        var buildOptions = new BuildOptions
        {
            SchemaRegistry = new SchemaRegistry(),
        };
        return JsonSchema.FromText(schemaText, buildOptions: buildOptions, baseUri: baseUri);
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

    private static JsonDocument LoadJsonDocument(string relativePath)
    {
        var fullPath = Path.Combine(
            ResolveRepoRoot().FullName,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        var text = File.ReadAllText(fullPath);
        return JsonDocument.Parse(text);
    }

    private static DirectoryInfo ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Game.sln")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot resolve repository root from test runtime.");
    }

    private static string BuildFailureReason(EvaluationResults root)
    {
        return EnemyConfigSchemaTestSupport.BuildFailureReason(root);
    }

    private static IEnumerable<EvaluationResults> Flatten(EvaluationResults root)
    {
        yield return root;
        if (root.Details is null)
        {
            yield break;
        }

        foreach (var child in root.Details)
        {
            foreach (var nested in Flatten(child))
            {
                yield return nested;
            }
        }
    }
}
