using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Services;

public sealed class EnemyConfigSampleFileValidationTests
{
    // ACC:T32.13
    [Fact]
    public void ShouldAcceptSampleContainingMultipleEnemies_WhenLoadingValidSampleFile()
    {
        using var payload = LoadJsonDocument("config/samples/enemy-config.sample.valid.json");
        EnemyConfigSchemaTestSupport.TryValidatePayload(payload.RootElement, out var reason).Should().BeTrue(reason);
    }

    // ACC:T32.15
    [Fact]
    public void ShouldAcceptBalanceOnlyValueChanges_WhenSchemaShapeAndTypesStayValid()
    {
        using var payload = LoadJsonDocument("config/samples/enemy-config.sample.valid.json");
        var adjustedJson = payload.RootElement.GetRawText()
            .Replace("\"health\": 12", "\"health\": 20", StringComparison.Ordinal)
            .Replace("\"damage\": 3", "\"damage\": 6", StringComparison.Ordinal);
        using var adjusted = JsonDocument.Parse(adjustedJson);

        EnemyConfigSchemaTestSupport.TryValidatePayload(adjusted.RootElement, out var reason).Should().BeTrue(reason);
    }

    // ACC:T32.16
    [Fact]
    public void ShouldRejectInvalidSampleFiles_WhenLoadedFromDisk()
    {
        using var missingRequired = LoadJsonDocument("config/samples/enemy-config.sample.missing-required.json");
        using var invalidType = LoadJsonDocument("config/samples/enemy-config.sample.invalid-type.json");
        using var belowMinimum = LoadJsonDocument("config/samples/enemy-config.sample.below-minimum.json");

        EnemyConfigSchemaTestSupport.TryValidatePayload(missingRequired.RootElement, out var missingReason).Should().BeFalse();
        missingReason.Should().Contain("required");

        EnemyConfigSchemaTestSupport.TryValidatePayload(invalidType.RootElement, out var typeReason).Should().BeFalse();
        typeReason.Should().Contain("type");
        typeReason.Should().Contain("object");

        EnemyConfigSchemaTestSupport.TryValidatePayload(belowMinimum.RootElement, out var minimumReason).Should().BeFalse();
        minimumReason.Should().Contain("minimum");
    }

    // ACC:T32.17
    [Fact]
    public void ShouldFailValidationRun_WhenSampleFilesAreMissingUnreadableOrInvalidJson()
    {
        Action missingAction = () => LoadJsonDocument("config/samples/enemy-config.sample.not-found.json");
        Action invalidJsonAction = () => LoadJsonDocument("config/samples/enemy-config.sample.invalid-json.json");

        missingAction.Should().Throw<FileNotFoundException>();
        invalidJsonAction.Should().Throw<JsonException>();
    }

    private static JsonDocument LoadJsonDocument(string relativePath)
    {
        var fullPath = Path.Combine(
            EnemyConfigSchemaTestSupport.ResolveRepoRoot().FullName,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Required sample file not found.", fullPath);
        }

        var text = File.ReadAllText(fullPath);
        return JsonDocument.Parse(text);
    }
}
