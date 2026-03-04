using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task1ProjectRootUniquenessTests
{
    // ACC:T1.11 (pure contract check)
    [Fact]
    public void ShouldRequireRunIdConsistencyAcrossAcceptanceEvidenceArtifacts()
    {
        var summary = CreateSyntheticAcceptanceSummary();
        var evidence = CreateSyntheticHeadlessEvidence();
        var runId = summary.GetProperty("run_id").GetString();
        runId.Should().NotBeNullOrWhiteSpace();

        AreRunIdFieldsConsistent(evidence, runId!).Should().BeTrue();
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

    // ACC:T1.24 (pure path contract check)
    [Fact]
    public void ShouldBindEvidencePathsToSingleCanonicalRepoRoot()
    {
        var evidence = CreateSyntheticHeadlessEvidence();
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

    private static bool AreRunIdFieldsConsistent(JsonElement evidence, string expectedRunId)
    {
        if (string.IsNullOrWhiteSpace(expectedRunId))
        {
            return false;
        }

        var fields = new[]
        {
            "expected_run_id",
            "run_id_in_summary",
            "run_id_in_file",
            "e2e_run_id_value"
        };

        foreach (var field in fields)
        {
            if (!evidence.TryGetProperty(field, out var value))
            {
                return false;
            }

            var text = value.GetString();
            if (!string.Equals(text, expectedRunId, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasSingleCanonicalRoot(IEnumerable<string> discoveredRoots)
    {
        var normalized = discoveredRoots
            .Select(NormalizePath)
            .Where(value => !string.IsNullOrWhiteSpace(value))
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

    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
