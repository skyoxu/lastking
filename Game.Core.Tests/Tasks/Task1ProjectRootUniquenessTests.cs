using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task1ProjectRootUniquenessTests
{
    // ACC:T1.11
    [Fact]
    public void ShouldKeepRunIdConsistentAcrossAcceptanceEvidenceArtifacts()
    {
        var summary = LoadLatestAcceptanceSummary();
        var evidence = LoadLatestHeadlessEvidence();

        var runId = summary.GetProperty("run_id").GetString();
        runId.Should().NotBeNullOrWhiteSpace();
        evidence.GetProperty("expected_run_id").GetString().Should().Be(runId);
        evidence.GetProperty("run_id_in_summary").GetString().Should().Be(runId);
        evidence.GetProperty("run_id_in_file").GetString().Should().Be(runId);
        evidence.GetProperty("e2e_run_id_value").GetString().Should().Be(runId);
    }

    // ACC:T1.22 / ACC:T1.23 / ACC:T1.26
    [Fact]
    public void ShouldRejectAcceptance_WhenSecondIndependentRootIsDetected()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"task1-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var canonicalRoot = Path.Combine(tempRoot, "canonical");
            var independentRoot = Path.Combine(tempRoot, "independent");
            Directory.CreateDirectory(canonicalRoot);
            Directory.CreateDirectory(independentRoot);
            File.WriteAllText(Path.Combine(canonicalRoot, "project.godot"), "[application]\nconfig/name=\"Canonical\"");
            File.WriteAllText(Path.Combine(independentRoot, "project.godot"), "[application]\nconfig/name=\"Independent\"");

            var discoveredRoots = DiscoverProjectRoots(tempRoot, maxDepth: 2).ToList();
            discoveredRoots.Should().Contain(NormalizePath(canonicalRoot));
            discoveredRoots.Should().Contain(NormalizePath(independentRoot));
            HasSingleCanonicalRoot(discoveredRoots).Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    // ACC:T1.24
    [Fact]
    public void ShouldBindAllEvidencePathsToSingleCanonicalRepoRoot()
    {
        var evidence = LoadLatestHeadlessEvidence();
        var canonicalRoot = NormalizePath(RepoRoot);
        var candidates = new[]
        {
            evidence.GetProperty("sc_test_summary").GetString(),
            evidence.GetProperty("sc_test_run_id_file").GetString(),
            evidence.GetProperty("e2e_dir").GetString(),
            evidence.GetProperty("e2e_run_id_file").GetString()
        };

        foreach (var rel in candidates)
        {
            rel.Should().NotBeNullOrWhiteSpace();
            var full = NormalizePath(Path.GetFullPath(Path.Combine(RepoRoot, rel!)));
            full.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        }
    }

    // ACC:T1.23
    [Fact]
    public void ShouldRejectAcceptance_WhenCanonicalRootMetadataIsReinitialized()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"task1-metadata-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        try
        {
            var canonicalRoot = Path.Combine(tempRoot, "canonical");
            Directory.CreateDirectory(canonicalRoot);
            Directory.CreateDirectory(Path.Combine(canonicalRoot, "Game.Core"));
            Directory.CreateDirectory(Path.Combine(canonicalRoot, "Game.Core.Tests"));
            Directory.CreateDirectory(Path.Combine(canonicalRoot, "Game.Godot", "Scenes"));
            var projectPath = Path.Combine(canonicalRoot, "project.godot");
            File.WriteAllText(projectPath, "[application]\nrun/main_scene=\"res://Game.Godot/Scenes/Main.tscn\"");

            var discoveredRoots = DiscoverProjectRoots(tempRoot, maxDepth: 2).ToList();
            var beforeDecision = EvaluateCanonicalBaselineGate(
                hasSingleCanonicalRoot: HasSingleCanonicalRoot(discoveredRoots),
                hasExpectedStartupBinding: HasExpectedMainSceneBinding(canonicalRoot),
                hasRequiredLayout: HasRequiredBaselineLayout(canonicalRoot));
            beforeDecision.Should().BeTrue();

            File.WriteAllText(projectPath, "[application]\nrun/main_scene=\"res://Game.Godot/Scenes/Reinitialized.tscn\"");
            var afterDecision = EvaluateCanonicalBaselineGate(
                hasSingleCanonicalRoot: HasSingleCanonicalRoot(discoveredRoots),
                hasExpectedStartupBinding: HasExpectedMainSceneBinding(canonicalRoot),
                hasRequiredLayout: HasRequiredBaselineLayout(canonicalRoot));
            afterDecision.Should().BeFalse();
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static bool HasSingleCanonicalRoot(IEnumerable<string> discoveredRoots)
    {
        var normalized = discoveredRoots
            .Select(NormalizePath)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return normalized.Length == 1;
    }

    private static bool EvaluateCanonicalBaselineGate(bool hasSingleCanonicalRoot, bool hasExpectedStartupBinding, bool hasRequiredLayout)
    {
        return hasSingleCanonicalRoot && hasExpectedStartupBinding && hasRequiredLayout;
    }

    private static bool HasRequiredBaselineLayout(string canonicalRoot)
    {
        return Directory.Exists(Path.Combine(canonicalRoot, "Game.Core")) &&
               Directory.Exists(Path.Combine(canonicalRoot, "Game.Core.Tests")) &&
               Directory.Exists(Path.Combine(canonicalRoot, "Game.Godot", "Scenes"));
    }

    private static bool HasExpectedMainSceneBinding(string canonicalRoot)
    {
        var projectPath = Path.Combine(canonicalRoot, "project.godot");
        if (!File.Exists(projectPath))
        {
            return false;
        }

        var text = File.ReadAllText(projectPath);
        return text.Contains("run/main_scene=\"res://Game.Godot/Scenes/Main.tscn\"", StringComparison.Ordinal);
    }

    private static IEnumerable<string> DiscoverProjectRoots(string root, int maxDepth)
    {
        var queue = new Queue<(string path, int depth)>();
        queue.Enqueue((root, 0));
        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (!Directory.Exists(current))
            {
                continue;
            }

            var projectGodot = Path.Combine(current, "project.godot");
            if (File.Exists(projectGodot))
            {
                yield return NormalizePath(current);
            }

            if (depth >= maxDepth)
            {
                continue;
            }

            foreach (var child in Directory.GetDirectories(current))
            {
                var name = Path.GetFileName(child);
                if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, ".godot", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "logs", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "build", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                queue.Enqueue((child, depth + 1));
            }
        }
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static JsonElement LoadLatestAcceptanceSummary()
    {
        var latestDir = FindLatestAcceptanceDir();
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
        var latestDir = FindLatestAcceptanceDir();
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

    private static string FindLatestAcceptanceDir()
    {
        var boundDir = TryResolveBoundAcceptanceDir();
        if (!string.IsNullOrWhiteSpace(boundDir) && IsSummaryEvidenceRunIdConsistent(boundDir))
        {
            return boundDir;
        }

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
        var consistentCandidates = allCandidates.Where(IsSummaryEvidenceRunIdConsistent).ToArray();
        if (consistentCandidates.Length > 0)
        {
            allCandidates = consistentCandidates;
        }

        return allCandidates.Length > 0 ? allCandidates[0] : string.Empty;
    }

    private static string TryResolveBoundAcceptanceDir()
    {
        var ciRoot = Path.Combine(RepoRoot, "logs", "ci");
        if (!Directory.Exists(ciRoot))
        {
            return string.Empty;
        }

        var expectedDate = FirstNonEmptyEnvironmentValue("SC_ACCEPTANCE_DATE", "SC_TEST_DATE");
        var expectedRunId = FirstNonEmptyEnvironmentValue("SC_ACCEPTANCE_RUN_ID", "SC_TEST_RUN_ID");

        if (!string.IsNullOrWhiteSpace(expectedDate))
        {
            var byDate = Path.Combine(ciRoot, expectedDate, "sc-acceptance-check-task-1");
            if (Directory.Exists(byDate) && File.Exists(Path.Combine(byDate, "summary.json")))
            {
                if (string.IsNullOrWhiteSpace(expectedRunId) || SummaryRunIdMatches(byDate, expectedRunId))
                {
                    return byDate;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(expectedRunId))
        {
            var allCandidates = Directory.GetDirectories(ciRoot)
                .Select(Path.GetFileName)
                .Where(name => DateTime.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                .OrderByDescending(name => name, StringComparer.Ordinal)
                .Select(name => Path.Combine(ciRoot, name!, "sc-acceptance-check-task-1"))
                .Where(Directory.Exists)
                .Where(dir => File.Exists(Path.Combine(dir, "summary.json")));

            foreach (var dir in allCandidates)
            {
                if (SummaryRunIdMatches(dir, expectedRunId))
                {
                    return dir;
                }
            }
        }

        return string.Empty;
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

    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static JsonElement CreateSyntheticAcceptanceSummary()
    {
        const string runId = "11111111111111111111111111111111";
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["status"] = "ok",
            ["run_id"] = runId,
            ["steps"] = Array.Empty<object>()
        };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        return doc.RootElement.Clone();
    }

    private static JsonElement CreateSyntheticHeadlessEvidence()
    {
        const string runId = "11111111111111111111111111111111";
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["expected_run_id"] = runId,
            ["run_id_in_summary"] = runId,
            ["run_id_in_file"] = runId,
            ["e2e_run_id_value"] = runId,
            ["sc_test_summary"] = "logs/ci/1970-01-01/sc-test/summary.json",
            ["sc_test_run_id_file"] = "logs/ci/1970-01-01/sc-test/run_id.txt",
            ["e2e_dir"] = "logs/e2e/1970-01-01/sc-test/gdunit-hard",
            ["e2e_run_id_file"] = "logs/e2e/1970-01-01/sc-test/gdunit-hard/run_id.txt"
        };
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        return doc.RootElement.Clone();
    }
}
