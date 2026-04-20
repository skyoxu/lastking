using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Game.Core.Tests.Services;
using Json.Schema;
using Xunit;

namespace Game.Core.Tests.Domain;

public sealed class PressureNormalizationConfigSchemaTests
{
    private static readonly object SchemaEvaluationGate = new();

    // ACC:T35.2
    // ACC:T35.4
    [Fact]
    public void ShouldParseSchemaFile_WhenSchemaExistsOnDisk()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/pressure-normalization.config.schema.json");
        schemaDocument.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    // ACC:T35.5
    // ACC:T35.13
    [Fact]
    public void ShouldDeclareExpectedCoreTypes_WhenInspectingSchemaProperties()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/pressure-normalization.config.schema.json");
        var properties = schemaDocument.RootElement.GetProperty("properties");

        properties.GetProperty("baseline").GetProperty("type").GetString().Should().Be("number");
        properties.GetProperty("min_pressure").GetProperty("type").GetString().Should().Be("number");
        properties.GetProperty("max_pressure").GetProperty("type").GetString().Should().Be("number");

        var normalizationFactors = properties.GetProperty("normalization_factors");
        normalizationFactors.GetProperty("type").GetString().Should().Be("array");
        normalizationFactors.GetProperty("items").GetProperty("type").GetString().Should().Be("number");
    }

    // ACC:T35.6
    // ACC:T35.14
    [Theory]
    [InlineData("{\"min_pressure\":0.0,\"max_pressure\":1.0,\"normalization_factors\":[1.0]}")]
    [InlineData("{\"baseline\":1.0,\"max_pressure\":1.0,\"normalization_factors\":[1.0]}")]
    [InlineData("{\"baseline\":1.0,\"min_pressure\":0.0,\"normalization_factors\":[1.0]}")]
    [InlineData("{\"baseline\":1.0,\"min_pressure\":0.0,\"max_pressure\":1.0}")]
    public void ShouldRejectPayload_WhenAnyRequiredPropertyIsMissing(string payloadJson)
    {
        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T35.7
    // ACC:T35.15
    [Fact]
    public void ShouldDeclareExplicitBounds_WhenInspectingBaselineAndPressureKeywords()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/pressure-normalization.config.schema.json");
        var properties = schemaDocument.RootElement.GetProperty("properties");

        var baselineProperty = properties.GetProperty("baseline");
        var minPressureProperty = properties.GetProperty("min_pressure");
        var maxPressureProperty = properties.GetProperty("max_pressure");

        TryGetLowerBound(baselineProperty, out _, out _).Should().BeTrue();
        HasAnyBound(minPressureProperty).Should().BeTrue();
        HasAnyBound(maxPressureProperty).Should().BeTrue();
        schemaDocument.RootElement.GetProperty(PressureNormalizationConfigContractValidator.RangeContractKeyword).GetString()
            .Should().Be("min_pressure < max_pressure");

        if (baselineProperty.TryGetProperty("default", out _))
        {
            var payloadWithoutBaseline = "{ \"min_pressure\": 0.0, \"max_pressure\": 1.0, \"normalization_factors\": [1.0] }";
            TryValidatePayload(payloadWithoutBaseline, out _).Should().BeFalse();
        }
    }

    // ACC:T35.8
    // ACC:T35.16
    [Theory]
    [InlineData("5.0", "5.0")]
    [InlineData("6.0", "5.0")]
    public void ShouldRejectConfiguration_WhenMinPressureIsGreaterThanOrEqualToMaxPressure(string minPressureToken, string maxPressureToken)
    {
        var payloadJson = BuildPayload(minPressureToken: minPressureToken, maxPressureToken: maxPressureToken);

        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T35.9
    [Fact]
    public void ShouldAcceptConfiguration_WhenMinPressureIsLessThanMaxPressure()
    {
        var payloadJson = BuildPayload(minPressureToken: "0.5", maxPressureToken: "1.5");

        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeTrue(reason);
    }

    // ACC:T35.10
    [Fact]
    public void ShouldRejectConfiguration_WhenBaselineIsNonNumeric()
    {
        var payloadJson = BuildPayload(baselineToken: "\"not_numeric\"");

        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T35.11
    [Fact]
    public void ShouldRejectConfiguration_WhenNormalizationFactorsContainNonNumericElements()
    {
        var payloadJson = BuildPayload(normalizationFactorsToken: "[1.0, \"invalid\", 0.9]");

        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T35.12
    [Fact]
    public void ShouldRejectConfiguration_WhenRangeCheckConstraintIsNonBoolean()
    {
        var payloadJson = BuildPayload(constraintsToken: "{ \"range_check\": 1 }");

        var isValid = TryValidatePayload(payloadJson, out var reason);

        isValid.Should().BeFalse();
        reason.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T35.17
    [Fact]
    public void ShouldApplyValidationMatrix_WhenEvaluatingCombinedAcceptanceRules()
    {
        var validPayload = BuildPayload(
            baselineToken: "1.0",
            minPressureToken: "0.2",
            maxPressureToken: "2.0",
            normalizationFactorsToken: "[0.8, 1.0, 1.2]",
            constraintsToken: "{ \"range_check\": true }");
        var invalidBaselinePayload = BuildPayload(
            baselineToken: "\"invalid\"",
            minPressureToken: "0.2",
            maxPressureToken: "2.0",
            normalizationFactorsToken: "[0.8, 1.0, 1.2]");
        var invalidOrderPayload = BuildPayload(
            baselineToken: "1.0",
            minPressureToken: "3.0",
            maxPressureToken: "2.0",
            normalizationFactorsToken: "[0.8, 1.0, 1.2]");
        var invalidFactorsPayload = BuildPayload(
            baselineToken: "1.0",
            minPressureToken: "0.2",
            maxPressureToken: "2.0",
            normalizationFactorsToken: "[0.8, \"bad\", 1.2]");

        TryValidatePayload(validPayload, out var validReason).Should().BeTrue(validReason);
        TryValidatePayload(invalidBaselinePayload, out var baselineReason).Should().BeFalse();
        TryValidatePayload(invalidOrderPayload, out var orderReason).Should().BeFalse();
        TryValidatePayload(invalidFactorsPayload, out var factorsReason).Should().BeFalse();

        baselineReason.Should().NotBeNullOrWhiteSpace();
        orderReason.Should().NotBeNullOrWhiteSpace();
        factorsReason.Should().NotBeNullOrWhiteSpace();
    }

    // ACC:T35.18
    [Fact]
    public void ShouldEnforceBaselineLowerBound_WhenValidatingBoundaryValues()
    {
        using var schemaDocument = LoadJsonDocument("config/schemas/pressure-normalization.config.schema.json");
        var baselineProperty = schemaDocument.RootElement.GetProperty("properties").GetProperty("baseline");

        var hasLowerBound = TryGetLowerBound(baselineProperty, out var lowerBound, out var exclusiveLowerBound);
        hasLowerBound.Should().BeTrue();

        var invalidBaseline = exclusiveLowerBound ? lowerBound : lowerBound - 0.001d;
        var validBaseline = exclusiveLowerBound ? lowerBound + 0.001d : lowerBound;

        var invalidPayload = BuildPayload(baselineToken: invalidBaseline.ToString("R", CultureInfo.InvariantCulture));
        var validPayload = BuildPayload(baselineToken: validBaseline.ToString("R", CultureInfo.InvariantCulture));

        TryValidatePayload(invalidPayload, out var invalidReason).Should().BeFalse();
        TryValidatePayload(validPayload, out var validReason).Should().BeTrue(validReason);
        invalidReason.Should().NotBeNullOrWhiteSpace();
    }

    private static string BuildPayload(
        string baselineToken = "1.0",
        string minPressureToken = "0.0",
        string maxPressureToken = "1.0",
        string normalizationFactorsToken = "[1.0]",
        string? constraintsToken = null)
    {
        var constraintsSegment = constraintsToken is null
            ? string.Empty
            : ", \"constraints\": " + constraintsToken;

        return "{ " +
               "\"baseline\": " + baselineToken + ", " +
               "\"min_pressure\": " + minPressureToken + ", " +
               "\"max_pressure\": " + maxPressureToken + ", " +
               "\"normalization_factors\": " + normalizationFactorsToken +
               constraintsSegment +
               " }";
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
                var schema = LoadPressureNormalizationSchema();
                var result = schema.Evaluate(payloadDocument.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
                if (!result.IsValid)
                {
                    reason = EnemyConfigSchemaTestSupport.BuildFailureReason(result);
                    return false;
                }

                if (!PressureNormalizationConfigContractValidator.TryValidate(payloadDocument.RootElement, out var contractReason))
                {
                    reason = contractReason;
                    return false;
                }

                reason = string.Empty;
                return true;
            }
        }
    }

    private static JsonSchema LoadPressureNormalizationSchema()
    {
        ResetGlobalSchemaRegistryForTests();
        var schemaPath = Path.Combine(
            ResolveRepoRoot().FullName,
            "config",
            "schemas",
            "pressure-normalization.config.schema.json");
        var schemaText = File.ReadAllText(schemaPath);
        var baseUri = new Uri("urn:lastking:test:pressure-normalization-config-schema");
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

    private static bool HasAnyBound(JsonElement propertyNode)
    {
        return propertyNode.TryGetProperty("minimum", out _) ||
               propertyNode.TryGetProperty("exclusiveMinimum", out _) ||
               propertyNode.TryGetProperty("maximum", out _) ||
               propertyNode.TryGetProperty("exclusiveMaximum", out _);
    }

    private static bool TryGetLowerBound(JsonElement propertyNode, out double lowerBound, out bool exclusive)
    {
        if (propertyNode.TryGetProperty("minimum", out var minimumElement) && minimumElement.TryGetDouble(out lowerBound))
        {
            exclusive = false;
            return true;
        }

        if (propertyNode.TryGetProperty("exclusiveMinimum", out var exclusiveMinimumElement) && exclusiveMinimumElement.TryGetDouble(out lowerBound))
        {
            exclusive = true;
            return true;
        }

        lowerBound = 0d;
        exclusive = false;
        return false;
    }
}
