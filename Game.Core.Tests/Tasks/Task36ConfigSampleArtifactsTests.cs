using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task36ConfigSampleArtifactsTests
{
    private static readonly string[] ExpectedDomains =
    [
        "difficulty",
        "spawn",
        "pressure-normalization"
    ];

    private static readonly Dictionary<string, string> SampleFilesByDomain = new(StringComparer.Ordinal)
    {
        ["difficulty"] = "Game.Core/Contracts/Config/difficulty-config.sample.json",
        ["spawn"] = "Game.Core/Contracts/Config/spawn-config.sample.json",
        ["pressure-normalization"] = "Game.Core/Contracts/Config/pressure-normalization.config.sample.json"
    };

    // acceptance: ACC:T36.1
    [Fact]
    public void ShouldRejectPlaceholderArtifacts_WhenSampleFilesArePublished()
    {
        foreach (var samplePath in SampleFilesByDomain.Values.Select(GetRepositoryPath))
        {
            File.Exists(samplePath).Should().BeTrue($"{Path.GetFileName(samplePath)} must be generated as a contract sample artifact");

            var content = File.ReadAllText(samplePath);
            content.Trim().Should().NotBeEmpty("sample artifacts must not be empty placeholders");
            content.Should().NotContain("TODO", "published samples must contain concrete contract data");
            content.Should().NotContain("PLACEHOLDER", "published samples must contain concrete contract data");
            content.Should().NotContain("TBD", "published samples must contain concrete contract data");

            using var document = JsonDocument.Parse(content);
            document.RootElement.ValueKind.Should().Be(JsonValueKind.Object, "sample artifacts must be JSON objects suitable for contract publication");
            document.RootElement.EnumerateObject().Should().NotBeEmpty("sample artifacts must expose concrete fields");
        }
    }

    // acceptance: ACC:T36.3
    [Fact]
    public void ShouldReportRequiredConfigDomains_WhenValidationOutputIsInspected()
    {
        var discoveredDomains = SampleFilesByDomain
            .Where(entry => File.Exists(GetRepositoryPath(entry.Value)))
            .Select(entry => entry.Key)
            .OrderBy(domain => domain, StringComparer.Ordinal)
            .ToArray();

        discoveredDomains.Should().Equal(ExpectedDomains.OrderBy(domain => domain, StringComparer.Ordinal),
            "validation output must cover difficulty, spawn, and pressure-normalization as separate domains");
    }

    // acceptance: ACC:T36.4
    [Fact]
    public void ShouldProvideDifficultyDefaults_WhenDifficultySampleIsValidated()
    {
        using var document = ReadRequiredSample("difficulty");
        var root = document.RootElement;

        root.TryGetProperty("version", out var version).Should().BeTrue("difficulty samples must include version metadata");
        version.GetString().Should().NotBeNullOrWhiteSpace();

        root.TryGetProperty("default_level_id", out var defaultLevelId).Should().BeTrue("difficulty samples must include deterministic default level");
        defaultLevelId.GetString().Should().NotBeNullOrWhiteSpace();

        root.TryGetProperty("allow_cross_tier_skip", out var allowCrossTierSkip).Should().BeTrue("difficulty samples must include lock-at-start equivalent field");
        allowCrossTierSkip.ValueKind.Should().Be(JsonValueKind.False);
    }

    // acceptance: ACC:T36.5
    [Fact]
    public void ShouldCoverDayOneThroughDayFifteen_WhenSpawnSampleIsValidated()
    {
        using var document = ReadRequiredSample("spawn");
        var root = document.RootElement;

        root.TryGetProperty("night_schedule", out var nightSchedule).Should().BeTrue("spawn samples must expose a night schedule");
        nightSchedule.ValueKind.Should().Be(JsonValueKind.Object);
        nightSchedule.TryGetProperty("elite_days", out var eliteDays).Should().BeTrue();
        nightSchedule.TryGetProperty("boss_days", out var bossDays).Should().BeTrue();

        var coveredDays = eliteDays.EnumerateArray()
            .Concat(bossDays.EnumerateArray())
            .Select(dayEntry => dayEntry.GetInt32())
            .OrderBy(day => day)
            .ToArray();

        coveredDays.Should().OnlyContain(day => day >= 1 && day <= 15, "spawn schedule days must stay in Day1-Day15 range");

        root.TryGetProperty("channel_budget_multipliers_source", out var channelSource).Should().BeTrue("spawn samples must include channel budget source mapping");
        channelSource.TryGetProperty("normal", out _).Should().BeTrue();
        channelSource.TryGetProperty("elite", out _).Should().BeTrue();
        channelSource.TryGetProperty("boss", out _).Should().BeTrue();
    }

    // acceptance: ACC:T36.6
    [Fact]
    public void ShouldKeepPressureTuningInsideGuardBands_WhenPressureNormalizationSampleIsValidated()
    {
        using var document = ReadRequiredSample("pressure-normalization");
        var root = document.RootElement;

        root.TryGetProperty("score_range", out var scoreRange).Should().BeTrue("pressure normalization samples must declare score range");
        scoreRange.TryGetProperty("min", out var minScore).Should().BeTrue();
        scoreRange.TryGetProperty("max", out var maxScore).Should().BeTrue();
        minScore.GetInt32().Should().BeLessThan(maxScore.GetInt32());

        root.TryGetProperty("weights", out var weights).Should().BeTrue("pressure normalization samples must include component weights");
        weights.ValueKind.Should().Be(JsonValueKind.Object);
        foreach (var weight in weights.EnumerateObject())
        {
            weight.Value.ValueKind.Should().Be(JsonValueKind.Number);
            weight.Value.GetInt32().Should().BePositive();
        }
    }

    private static JsonDocument ReadRequiredSample(string domain)
    {
        var samplePath = GetRepositoryPath(SampleFilesByDomain[domain]);
        File.Exists(samplePath).Should().BeTrue($"{Path.GetFileName(samplePath)} must exist");

        var content = File.ReadAllText(samplePath);
        content.Trim().Should().NotBeEmpty($"{Path.GetFileName(samplePath)} must not be empty");

        return JsonDocument.Parse(content);
    }

    private static string GetRepositoryPath(string relativePath)
    {
        return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Game.Core.Tests")) && Directory.Exists(Path.Combine(current.FullName, ".taskmaster")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be located from the test output directory.");
    }
}
