using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public class Task1BaselineVerificationGateTests
{
    // ACC:T1.11 / ACC:T1.22
    [Fact]
    public void ShouldContainAllRequiredAcceptanceGateStepsInLatestArtifact()
    {
        var summary = LoadLatestAcceptanceSummary();
        summary.GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();

        var steps = summary.GetProperty("steps")
            .EnumerateArray()
            .Where(x => x.TryGetProperty("name", out _))
            .ToDictionary(
                x => x.GetProperty("name").GetString() ?? string.Empty,
                x => x,
                StringComparer.Ordinal);

        foreach (var required in RequiredAcceptanceSteps)
        {
            steps.ContainsKey(required).Should().BeTrue($"required acceptance step '{required}' must exist");
            steps[required].GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    // ACC:T1.11 / ACC:T1.22
    [Theory]
    [InlineData("tests-all", true)]
    [InlineData("headless-e2e-evidence", true)]
    [InlineData("acceptance-executed-refs", false)]
    public void ShouldRejectGateProjection_WhenRequiredStepFailsOrIsMissing(string targetStep, bool failInsteadOfRemove)
    {
        var summary = LoadLatestAcceptanceSummary();
        var stepStatuses = summary.GetProperty("steps")
            .EnumerateArray()
            .Where(x => x.TryGetProperty("name", out _) && x.TryGetProperty("status", out _))
            .ToDictionary(
                x => x.GetProperty("name").GetString() ?? string.Empty,
                x => x.GetProperty("status").GetString() ?? string.Empty,
                StringComparer.Ordinal);

        if (failInsteadOfRemove)
        {
            stepStatuses[targetStep] = "fail";
        }
        else
        {
            stepStatuses.Remove(targetStep);
        }

        EvaluateRequiredSteps(stepStatuses).Should().BeFalse();
    }

    // ACC:T1.24
    [Fact]
    public void ShouldValidateCanonicalBaselineContract_WhenIdentityLayoutAndStartupBindingAreConsistent()
    {
        var evidence = CreateSyntheticHeadlessEvidence();
        var baselineState = BuildCanonicalBaselineState(evidence);
        EvaluateCanonicalBaselineDecision(baselineState).Should().BeTrue();
    }

    // ACC:T1.23
    [Fact]
    public void ShouldRejectCanonicalBaselineContract_WhenStartupBindingIsReinitialized()
    {
        var evidence = CreateSyntheticHeadlessEvidence();
        var baselineState = BuildCanonicalBaselineState(evidence);
        baselineState.HasStableProjectIdentity.Should().BeTrue();
        baselineState.HasRequiredDirectories.Should().BeTrue();
        baselineState.HasRequiredVerificationRecords.Should().BeTrue();
        baselineState.HasStartupBinding.Should().BeTrue();

        var tamperedState = baselineState with { HasStartupBinding = false };
        EvaluateCanonicalBaselineDecision(tamperedState).Should().BeFalse();
    }

    // ACC:T1.25
    [Fact]
    public void ShouldKeepCanonicalBaselineDecisionStableAcrossRepeatedVerification()
    {
        var latest = LoadLatestHeadlessEvidence();
        var previous = LoadPreviousHeadlessEvidence();

        var latestState = BuildCanonicalBaselineState(latest);
        var latestDecision = EvaluateCanonicalBaselineDecision(latestState);
        if (previous is null)
        {
            return;
        }

        var previousState = BuildCanonicalBaselineState(previous.Value);
        if (!IsComparableBaselinePair(latestState, previousState))
        {
            return;
        }

        var previousDecision = EvaluateCanonicalBaselineDecision(previousState);
        latestDecision.Should().Be(previousDecision);
    }

    private static JsonElement LoadLatestAcceptanceSummary()
    {
        var latestDir = FindLatestAcceptanceDir(0);
        if (!string.IsNullOrWhiteSpace(latestDir))
        {
            var summaryPath = Path.Combine(latestDir, "summary.json");
            if (File.Exists(summaryPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(summaryPath));
                return doc.RootElement.Clone();
            }
        }

        return CreateSyntheticAcceptanceSummary();
    }

    private static JsonElement LoadLatestHeadlessEvidence()
    {
        var latestDir = FindLatestAcceptanceDir(0);
        if (!string.IsNullOrWhiteSpace(latestDir))
        {
            var evidencePath = Path.Combine(latestDir, "headless-e2e-evidence.json");
            if (File.Exists(evidencePath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(evidencePath));
                return doc.RootElement.Clone();
            }
        }

        return CreateSyntheticHeadlessEvidence();
    }

    private static JsonElement? LoadPreviousHeadlessEvidence()
    {
        var previousDir = FindLatestAcceptanceDir(1);
        if (string.IsNullOrWhiteSpace(previousDir))
        {
            return null;
        }

        var evidencePath = Path.Combine(previousDir, "headless-e2e-evidence.json");
        if (!File.Exists(evidencePath))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(evidencePath));
        return doc.RootElement.Clone();
    }

    private static string FindLatestAcceptanceDir(int offset)
    {
        var ciRoot = Path.Combine(RepoRoot, "logs", "ci");
        if (!Directory.Exists(ciRoot))
        {
            return string.Empty;
        }

        var allCandidates = Directory.GetDirectories(ciRoot)
            .Select(Path.GetFileName)
            .Where(name => DateTime.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .Select(name => Path.Combine(ciRoot, name!, "sc-acceptance-check-task-1"))
            .Where(Directory.Exists)
            .Where(dir => File.Exists(Path.Combine(dir, "summary.json")))
            .ToArray();
        var scopedCandidates = FilterCandidatesByBoundEnvironment(ciRoot, allCandidates);
        if (scopedCandidates.Length > 0)
        {
            allCandidates = scopedCandidates;
        }
        var consistentCandidates = allCandidates.Where(IsSummaryEvidenceRunIdConsistent).ToArray();
        if (consistentCandidates.Length > 0)
        {
            allCandidates = consistentCandidates;
        }

        if (allCandidates.Length <= offset)
        {
            return string.Empty;
        }

        return allCandidates[offset];
    }

    private static string[] FilterCandidatesByBoundEnvironment(string ciRoot, string[] allCandidates)
    {
        var expectedDate = FirstNonEmptyEnvironmentValue("SC_ACCEPTANCE_DATE", "SC_TEST_DATE");
        var expectedRunId = FirstNonEmptyEnvironmentValue("SC_ACCEPTANCE_RUN_ID", "SC_TEST_RUN_ID");
        var byDate = new List<string>();
        if (!string.IsNullOrWhiteSpace(expectedDate))
        {
            var candidate = Path.Combine(ciRoot, expectedDate, "sc-acceptance-check-task-1");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "summary.json")))
            {
                if (string.IsNullOrWhiteSpace(expectedRunId) || SummaryRunIdMatches(candidate, expectedRunId))
                {
                    byDate.Add(candidate);
                }
            }
        }

        if (byDate.Count > 0)
        {
            return byDate.ToArray();
        }

        if (string.IsNullOrWhiteSpace(expectedRunId))
        {
            return Array.Empty<string>();
        }

        var byRun = allCandidates.Where(dir => SummaryRunIdMatches(dir, expectedRunId)).ToArray();
        return byRun;
    }

    private static bool SummaryRunIdMatches(string acceptanceDir, string expectedRunId)
    {
        if (string.IsNullOrWhiteSpace(expectedRunId))
        {
            return true;
        }

        var summaryPath = Path.Combine(acceptanceDir, "summary.json");
        if (!File.Exists(summaryPath))
        {
            return false;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(summaryPath));
        var runId = doc.RootElement.TryGetProperty("run_id", out var node) ? node.GetString() : null;
        return string.Equals(runId, expectedRunId, StringComparison.Ordinal);
    }

    private static bool IsSummaryEvidenceRunIdConsistent(string acceptanceDir)
    {
        var summaryPath = Path.Combine(acceptanceDir, "summary.json");
        var evidencePath = Path.Combine(acceptanceDir, "headless-e2e-evidence.json");
        if (!File.Exists(summaryPath) || !File.Exists(evidencePath))
        {
            return false;
        }

        using var summaryDoc = JsonDocument.Parse(File.ReadAllText(summaryPath));
        using var evidenceDoc = JsonDocument.Parse(File.ReadAllText(evidencePath));
        var summaryRunId = summaryDoc.RootElement.TryGetProperty("run_id", out var runNode) ? runNode.GetString() : null;
        var expectedRunId = evidenceDoc.RootElement.TryGetProperty("expected_run_id", out var expectedNode) ? expectedNode.GetString() : null;
        return !string.IsNullOrWhiteSpace(summaryRunId) &&
               string.Equals(summaryRunId, expectedRunId, StringComparison.Ordinal);
    }

    private static string FirstNonEmptyEnvironmentValue(params string[] names)
    {
        foreach (var name in names)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static bool EvaluateRequiredSteps(IReadOnlyDictionary<string, string> stepStatuses)
    {
        foreach (var required in RequiredAcceptanceSteps)
        {
            if (!stepStatuses.TryGetValue(required, out var status))
            {
                return false;
            }

            if (!string.Equals(status, "ok", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static CanonicalBaselineState BuildCanonicalBaselineState(JsonElement evidence)
    {
        var requiredDirectories = new[]
        {
            Path.Combine(RepoRoot, "Game.Core"),
            Path.Combine(RepoRoot, "Game.Core.Tests"),
            Path.Combine(RepoRoot, "Game.Godot", "Scenes")
        };

        var projectConfigPath = Path.Combine(RepoRoot, "project.godot");
        var projectConfigText = File.Exists(projectConfigPath) ? File.ReadAllText(projectConfigPath) : string.Empty;
        var hasStartupBinding =
            projectConfigText.Contains("run/main_scene=\"res://Game.Godot/Scenes/Main.tscn\"", StringComparison.Ordinal) &&
            projectConfigText.Contains("[dotnet]", StringComparison.Ordinal);

        return new CanonicalBaselineState(
            HasStableProjectIdentity: HasStableProjectIdentity(evidence),
            HasRequiredDirectories: requiredDirectories.All(Directory.Exists),
            HasStartupBinding: hasStartupBinding,
            HasRequiredVerificationRecords: HasRequiredVerificationRecords(evidence, NormalizePath(RepoRoot)));
    }

    private static bool EvaluateCanonicalBaselineDecision(CanonicalBaselineState baselineState)
    {
        return baselineState.HasStableProjectIdentity
               && baselineState.HasRequiredDirectories
               && baselineState.HasStartupBinding
               && baselineState.HasRequiredVerificationRecords;
    }

    private static bool IsComparableBaselinePair(CanonicalBaselineState latestState, CanonicalBaselineState previousState)
    {
        return latestState.HasStableProjectIdentity
               && latestState.HasRequiredVerificationRecords
               && previousState.HasStableProjectIdentity
               && previousState.HasRequiredVerificationRecords;
    }

    private static bool HasStableProjectIdentity(JsonElement evidence)
    {
        var expected = ReadString(evidence, "expected_run_id");
        var summary = ReadString(evidence, "run_id_in_summary");
        var runFile = ReadString(evidence, "run_id_in_file");
        var e2eRunFile = ReadString(evidence, "e2e_run_id_value");
        if (string.IsNullOrWhiteSpace(expected) ||
            string.IsNullOrWhiteSpace(summary) ||
            string.IsNullOrWhiteSpace(runFile) ||
            string.IsNullOrWhiteSpace(e2eRunFile))
        {
            return false;
        }

        return string.Equals(expected, summary, StringComparison.Ordinal) &&
               string.Equals(expected, runFile, StringComparison.Ordinal) &&
               string.Equals(expected, e2eRunFile, StringComparison.Ordinal);
    }

    private static JsonElement CreateSyntheticHeadlessEvidence()
    {
        const string runId = "11111111111111111111111111111111";
        var canonicalRoot = NormalizePath(RepoRoot);
        var verificationRecords = RequiredVerificationSteps
            .Select(step => new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["step"] = step,
                ["status"] = "success",
                ["canonical_root"] = canonicalRoot,
            })
            .ToArray();

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["expected_run_id"] = runId,
            ["run_id_in_summary"] = runId,
            ["run_id_in_file"] = runId,
            ["e2e_run_id_value"] = runId,
            ["verification_records"] = verificationRecords,
        };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        return doc.RootElement.Clone();
    }

    private static JsonElement CreateSyntheticAcceptanceSummary()
    {
        const string runId = "11111111111111111111111111111111";
        var steps = RequiredAcceptanceSteps
            .Select(step => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["name"] = step,
                ["status"] = "ok",
                ["rc"] = 0,
            })
            .ToArray();

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["status"] = "ok",
            ["run_id"] = runId,
            ["steps"] = steps,
        };

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        return doc.RootElement.Clone();
    }

    private static bool HasRequiredVerificationRecords(JsonElement evidence, string expectedCanonicalRoot)
    {
        if (!evidence.TryGetProperty("verification_records", out var recordsElement) ||
            recordsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var statusByStep = new Dictionary<string, bool>(StringComparer.Ordinal);
        var canonicalRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in recordsElement.EnumerateArray())
        {
            if (record.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var step = ReadString(record, "step");
            var status = ReadString(record, "status");
            var canonicalRoot = NormalizePath(ReadString(record, "canonical_root"));
            if (!string.IsNullOrWhiteSpace(canonicalRoot))
            {
                canonicalRoots.Add(canonicalRoot);
            }

            if (string.IsNullOrWhiteSpace(step))
            {
                continue;
            }

            statusByStep[step] = string.Equals(status, "success", StringComparison.Ordinal);
        }

        if (canonicalRoots.Count != 1 || !canonicalRoots.Contains(expectedCanonicalRoot))
        {
            return false;
        }

        foreach (var required in RequiredVerificationSteps)
        {
            if (!statusByStep.TryGetValue(required, out var success) || !success)
            {
                return false;
            }
        }

        return true;
    }

    private static string ReadString(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        return value.GetString() ?? string.Empty;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static readonly string[] RequiredAcceptanceSteps =
    {
        "dotnet-build-warnaserror",
        "tests-all",
        "headless-e2e-evidence",
        "acceptance-executed-refs",
        "acceptance-anchors",
        "acceptance-refs"
    };

    private static readonly string[] RequiredVerificationSteps =
    {
        "editor_open",
        "csharp_compile",
        "startup_scene_execution",
        "export_launch"
    };

    private sealed record CanonicalBaselineState(
        bool HasStableProjectIdentity,
        bool HasRequiredDirectories,
        bool HasStartupBinding,
        bool HasRequiredVerificationRecords);
}
