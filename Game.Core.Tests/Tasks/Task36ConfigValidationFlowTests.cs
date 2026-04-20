using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Json.Schema;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task36ConfigValidationFlowTests
{
    private const string PositiveFixtureKind = "positive";
    private const string SummaryArtifactPath = "logs/ci/task-36/config-validation-summary.json";
    private static readonly object SchemaEvaluationGate = new();

    private static readonly string[] RequiredDomains =
    [
        "difficulty",
        "spawn",
        "pressure-normalization"
    ];

    private static readonly string[] NegativeFixtureKinds =
    [
        "malformed",
        "boundary-breach"
    ];

    private static readonly Dictionary<string, string> PositiveSampleFilesByDomain = new(StringComparer.Ordinal)
    {
        ["difficulty"] = "Game.Core/Contracts/Config/difficulty-config.sample.json",
        ["spawn"] = "Game.Core/Contracts/Config/spawn-config.sample.json",
        ["pressure-normalization"] = "Game.Core/Contracts/Config/pressure-normalization.config.sample.json"
    };

    private static readonly Dictionary<string, string> SchemaFilesByDomain = new(StringComparer.Ordinal)
    {
        ["difficulty"] = "Game.Core/Contracts/Config/difficulty-config.schema.json",
        ["spawn"] = "Game.Core/Contracts/Config/spawn-config.schema.json",
        ["pressure-normalization"] = "Game.Core/Contracts/Config/pressure-normalization.config.schema.json"
    };

    // acceptance: ACC:T36.2
    [Fact]
    public void ShouldPassPositiveSamples_WhenRunningValidationFlow()
    {
        var summary = BuildValidationSummary();
        var positiveEntries = summary.Results
            .Where(result => string.Equals(result.FixtureKind, PositiveFixtureKind, StringComparison.Ordinal))
            .ToArray();

        positiveEntries.Should().HaveCount(RequiredDomains.Length);
        positiveEntries.Select(result => result.Domain).Should().BeEquivalentTo(RequiredDomains);
        positiveEntries.Should().OnlyContain(
            result => result.Schema.Passed && result.Semantic.Passed,
            "every required positive sample must pass schema and semantic validation in the same automated flow");
    }

    // acceptance: ACC:T36.3
    [Fact]
    public void ShouldReportRequiredDomains_WhenValidationOutputIsInspected()
    {
        var summary = BuildValidationSummary();
        var reportedDomains = summary.Domains.OrderBy(domain => domain, StringComparer.Ordinal).ToArray();
        var resultDomains = summary.Results
            .Select(entry => entry.Domain)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(domain => domain, StringComparer.Ordinal)
            .ToArray();
        var expectedDomains = RequiredDomains.OrderBy(domain => domain, StringComparer.Ordinal).ToArray();

        reportedDomains.Should().Equal(expectedDomains);
        resultDomains.Should().Equal(expectedDomains);
    }

    // acceptance: ACC:T36.7
    [Fact]
    public void ShouldRejectNegativeFixtures_WhenRunningValidationFlow()
    {
        var summary = BuildValidationSummary();

        foreach (var domain in RequiredDomains)
        {
            var positiveEntry = summary.Results.Single(entry =>
                string.Equals(entry.Domain, domain, StringComparison.Ordinal)
                && string.Equals(entry.FixtureKind, PositiveFixtureKind, StringComparison.Ordinal));
            positiveEntry.Schema.Passed.Should().BeTrue($"{domain} positive schema must pass");
            positiveEntry.Semantic.Passed.Should().BeTrue($"{domain} positive semantic must pass");

            foreach (var fixtureKind in NegativeFixtureKinds)
            {
                var negatives = summary.Results.Where(entry =>
                        string.Equals(entry.Domain, domain, StringComparison.Ordinal)
                        && string.Equals(entry.FixtureKind, fixtureKind, StringComparison.Ordinal))
                    .ToArray();

                negatives.Should().NotBeEmpty($"{domain} must provide {fixtureKind} fixtures");
                negatives.Should().OnlyContain(entry => !(entry.Schema.Passed && entry.Semantic.Passed));
            }
        }
    }

    // acceptance: ACC:T36.8
    [Fact]
    public void ShouldProduceCiSummaryArtifact_WhenValidationFlowCompletes()
    {
        var summary = BuildValidationSummary();
        var summaryPath = ResolveRepositoryPath(SummaryArtifactPath);
        var summaryJson = JsonSerializer.Serialize(summary, JsonOptions);

        Directory.CreateDirectory(Path.GetDirectoryName(summaryPath)!);
        File.WriteAllText(summaryPath, summaryJson);

        File.Exists(summaryPath).Should().BeTrue();
        using var parsed = JsonDocument.Parse(File.ReadAllText(summaryPath));
        parsed.RootElement.GetProperty("taskId").GetInt32().Should().Be(36);
        parsed.RootElement.GetProperty("results").GetArrayLength().Should().Be(summary.Results.Count);
    }

    // acceptance: ACC:T36.9
    [Fact]
    public void ShouldFailValidation_WhenRequiredPositiveSampleIsMissingEmptyOrUnreported()
    {
        var sampleFilesByDomain = new Dictionary<string, string>(PositiveSampleFilesByDomain, StringComparer.Ordinal)
        {
            ["difficulty"] = "logs/ci/task-36/__missing-positive-sample-do-not-create__.json"
        };
        var missingResult = ValidatePositiveSample("difficulty", sampleFilesByDomain["difficulty"]);
        var emptyResult = ValidatePositiveSampleContent("spawn", string.Empty, PositiveSampleFilesByDomain["spawn"]);
        var reportedResults = BuildValidationSummary().Results
            .Where(result => !string.Equals(result.Domain, "pressure-normalization", StringComparison.Ordinal)
                             || !string.Equals(result.FixtureKind, PositiveFixtureKind, StringComparison.Ordinal))
            .ToArray();
        var coverageResult = EvaluateRequiredPositiveCoverage(reportedResults);

        missingResult.Schema.Passed.Should().BeFalse();
        missingResult.Semantic.Passed.Should().BeFalse();
        missingResult.Schema.ReasonCodes.Should().Contain("missing-file");

        emptyResult.Schema.Passed.Should().BeFalse();
        emptyResult.Semantic.Passed.Should().BeFalse();
        emptyResult.Schema.ReasonCodes.Should().Contain("empty-file");

        coverageResult.Passed.Should().BeFalse();
        coverageResult.ReasonCodes.Should().Contain("unreported-positive-sample");
    }

    // acceptance: ACC:T36.10
    [Fact]
    public void ShouldReportFileLevelResults_WhenPositiveSamplesAreValidated()
    {
        var summary = BuildValidationSummary();

        foreach (var entry in PositiveSampleFilesByDomain)
        {
            var domain = entry.Key;
            var expectedPath = NormalizePath(entry.Value);
            var matchingEntries = summary.Results
                .Where(result =>
                    string.Equals(result.Domain, domain, StringComparison.Ordinal)
                    && string.Equals(result.FixtureKind, PositiveFixtureKind, StringComparison.Ordinal)
                    && string.Equals(result.FilePath, expectedPath, StringComparison.Ordinal))
                .ToArray();

            matchingEntries.Should().ContainSingle();
            matchingEntries.Single().Schema.Should().NotBeNull();
            matchingEntries.Single().Semantic.Should().NotBeNull();
        }
    }

    // acceptance: ACC:T36.11
    [Fact]
    public void ShouldRejectEachNegativeFixtureWithReasonCodes_WhenValidationFlowRuns()
    {
        var summary = BuildValidationSummary();

        foreach (var domain in RequiredDomains)
        {
            foreach (var fixtureKind in NegativeFixtureKinds)
            {
                var negativeEntries = summary.Results.Where(result =>
                        string.Equals(result.Domain, domain, StringComparison.Ordinal)
                        && string.Equals(result.FixtureKind, fixtureKind, StringComparison.Ordinal))
                    .ToArray();
                negativeEntries.Should().NotBeEmpty();

                foreach (var entry in negativeEntries)
                {
                    (entry.Schema.Passed && entry.Semantic.Passed).Should().BeFalse();
                    entry.ReasonCodes.Should().NotBeEmpty();
                    entry.ReasonCodes.Should().OnlyContain(reasonCode => !string.IsNullOrWhiteSpace(reasonCode));
                    entry.ReasonCodes.Should().NotContain("unknown");
                }
            }
        }
    }

    private static ValidationSummary BuildValidationSummary()
    {
        var results = new List<ValidationResult>();
        foreach (var domain in RequiredDomains)
        {
            results.Add(ValidatePositiveSample(domain, PositiveSampleFilesByDomain[domain]));
            results.Add(ValidateNegativeFixture(domain, "malformed", "malformed-json"));
            results.Add(ValidateNegativeFixture(domain, "boundary-breach", "boundary-breach"));
        }

        return new ValidationSummary(36, RequiredDomains, results);
    }

    private static ValidationResult ValidatePositiveSample(string domain, string relativePath)
    {
        var fullPath = ResolveRepositoryPath(relativePath);
        if (!File.Exists(fullPath))
        {
            return ValidationResult.Reject(domain, PositiveFixtureKind, "sample", NormalizePath(relativePath), "missing-file");
        }

        var content = File.ReadAllText(fullPath);
        return ValidatePositiveSampleContent(domain, content, relativePath);
    }

    private static ValidationResult ValidatePositiveSampleContent(string domain, string content, string relativePath)
    {
        var normalizedPath = NormalizePath(relativePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return ValidationResult.Reject(domain, PositiveFixtureKind, "sample", normalizedPath, "empty-file");
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var schemaReasons = new List<string>();
            var semanticReasons = new List<string>();

            ValidateAgainstSchema(domain, doc.RootElement, schemaReasons);
            ValidateSemantics(domain, doc.RootElement, semanticReasons);

            return new ValidationResult(
                domain,
                PositiveFixtureKind,
                "sample",
                normalizedPath,
                new PhaseResult(schemaReasons.Count == 0, schemaReasons.Distinct(StringComparer.Ordinal).ToArray()),
                new PhaseResult(schemaReasons.Count == 0 && semanticReasons.Count == 0, semanticReasons.Distinct(StringComparer.Ordinal).ToArray()));
        }
        catch (JsonException)
        {
            return ValidationResult.Reject(domain, PositiveFixtureKind, "sample", normalizedPath, "malformed-json");
        }
    }

    private static ValidationResult ValidateNegativeFixture(string domain, string fixtureKind, string caseId)
    {
        var fixturePath = NormalizePath($"logs/ci/task-36/fixtures/{domain}.{fixtureKind}.json");
        var fullPath = ResolveRepositoryPath(fixturePath);
        if (!File.Exists(fullPath))
        {
            return ValidationResult.Reject(domain, fixtureKind, caseId, fixturePath, "missing-file");
        }

        var content = File.ReadAllText(fullPath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return ValidationResult.Reject(domain, fixtureKind, caseId, fixturePath, "empty-file");
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            var schemaReasons = new List<string>();
            var semanticReasons = new List<string>();
            ValidateAgainstSchema(domain, doc.RootElement, schemaReasons);
            ValidateSemantics(domain, doc.RootElement, semanticReasons);

            if (schemaReasons.Count == 0 && semanticReasons.Count == 0)
            {
                semanticReasons.Add("negative-fixture-not-rejected");
            }

            return new ValidationResult(
                domain,
                fixtureKind,
                caseId,
                fixturePath,
                new PhaseResult(schemaReasons.Count == 0, schemaReasons.Distinct(StringComparer.Ordinal).ToArray()),
                new PhaseResult(schemaReasons.Count == 0 && semanticReasons.Count == 0, semanticReasons.Distinct(StringComparer.Ordinal).ToArray()));
        }
        catch (JsonException)
        {
            return ValidationResult.Reject(domain, fixtureKind, caseId, fixturePath, "malformed-json");
        }
    }

    private static CoverageResult EvaluateRequiredPositiveCoverage(IReadOnlyCollection<ValidationResult> validationResults)
    {
        var reasonCodes = new List<string>();
        foreach (var domain in RequiredDomains)
        {
            var positives = validationResults.Where(result =>
                    string.Equals(result.Domain, domain, StringComparison.Ordinal)
                    && string.Equals(result.FixtureKind, PositiveFixtureKind, StringComparison.Ordinal))
                .ToArray();

            if (positives.Length == 0)
            {
                reasonCodes.Add("unreported-positive-sample");
                continue;
            }

            foreach (var positive in positives)
            {
                if (string.IsNullOrWhiteSpace(positive.FilePath))
                {
                    reasonCodes.Add("empty-positive-file-path");
                }

                if (!positive.Schema.Passed || !positive.Semantic.Passed)
                {
                    reasonCodes.Add("failed-positive-validation");
                }
            }
        }

        return new CoverageResult(reasonCodes.Count == 0, reasonCodes.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static void ValidateAgainstSchema(string domain, JsonElement root, List<string> schemaReasonCodes)
    {
        var schemaPath = ResolveRepositoryPath(SchemaFilesByDomain[domain]);
        var schemaText = File.ReadAllText(schemaPath);
        lock (SchemaEvaluationGate)
        {
            ResetGlobalSchemaRegistryForTests();
            var buildOptions = new BuildOptions
            {
                SchemaRegistry = new SchemaRegistry(),
            };
            var baseUri = new Uri($"urn:lastking:test:task36:{domain}");
            var schema = JsonSchema.FromText(schemaText, buildOptions: buildOptions, baseUri: baseUri);
            var result = schema.Evaluate(root, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
            if (!result.IsValid)
            {
                schemaReasonCodes.Add("schema-invalid");
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

    private static void ValidateSemantics(string domain, JsonElement root, List<string> semanticReasonCodes)
    {
        switch (domain)
        {
            case "difficulty":
                if (!root.TryGetProperty("version", out var version) || string.IsNullOrWhiteSpace(version.GetString()))
                {
                    semanticReasonCodes.Add("semantic-version-empty");
                }

                if (!root.TryGetProperty("default_level_id", out var defaultLevel) || string.IsNullOrWhiteSpace(defaultLevel.GetString()))
                {
                    semanticReasonCodes.Add("semantic-default-level-id-missing");
                }

                if (!root.TryGetProperty("levels", out var levels) || levels.ValueKind != JsonValueKind.Array || levels.GetArrayLength() < 10)
                {
                    semanticReasonCodes.Add("semantic-levels-coverage-required");
                }

                break;
            case "spawn":
                if (!root.TryGetProperty("refresh_step_seconds", out var step) || step.GetInt32() <= 0)
                {
                    semanticReasonCodes.Add("semantic-refresh-step-positive-required");
                }

                if (!root.TryGetProperty("night_schedule", out var schedule)
                    || !schedule.TryGetProperty("elite_days", out var eliteDays)
                    || !schedule.TryGetProperty("boss_days", out var bossDays)
                    || eliteDays.ValueKind != JsonValueKind.Array
                    || bossDays.ValueKind != JsonValueKind.Array)
                {
                    semanticReasonCodes.Add("semantic-night-schedule-required");
                }

                break;
            case "pressure-normalization":
                if (!root.TryGetProperty("score_range", out var scoreRange)
                    || !scoreRange.TryGetProperty("min", out var minScore)
                    || !scoreRange.TryGetProperty("max", out var maxScore)
                    || minScore.GetInt32() >= maxScore.GetInt32())
                {
                    semanticReasonCodes.Add("semantic-score-range-order-required");
                }

                if (!root.TryGetProperty("weights", out var weights)
                    || weights.ValueKind != JsonValueKind.Object
                    || weights.EnumerateObject().Any(item => item.Value.GetInt32() <= 0))
                {
                    semanticReasonCodes.Add("semantic-weights-positive-required");
                }

                break;
            default:
                semanticReasonCodes.Add("semantic-unknown-domain");
                break;
        }
    }

    private static string ResolveRepositoryPath(string relativePath)
    {
        return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string NormalizePath(string path)
    {
        return (path ?? string.Empty).Replace('\\', '/').Trim();
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "Game.Core.Tests"))
                && Directory.Exists(Path.Combine(current.FullName, ".taskmaster")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be located from the test output directory.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private sealed record ValidationSummary(int TaskId, IReadOnlyList<string> Domains, IReadOnlyList<ValidationResult> Results);

    private sealed record ValidationResult(
        string Domain,
        string FixtureKind,
        string CaseId,
        string FilePath,
        PhaseResult Schema,
        PhaseResult Semantic)
    {
        public string[] ReasonCodes => Schema.ReasonCodes.Concat(Semantic.ReasonCodes).Distinct(StringComparer.Ordinal).ToArray();

        public static ValidationResult Reject(string domain, string fixtureKind, string caseId, string filePath, string reasonCode)
        {
            return new ValidationResult(
                domain,
                fixtureKind,
                caseId,
                filePath,
                new PhaseResult(false, [reasonCode]),
                new PhaseResult(false, [reasonCode]));
        }
    }

    private sealed record PhaseResult(bool Passed, string[] ReasonCodes);

    private sealed record CoverageResult(bool Passed, string[] ReasonCodes);
}
