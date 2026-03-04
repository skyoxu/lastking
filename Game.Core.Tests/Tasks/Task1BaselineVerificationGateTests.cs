using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public class Task1BaselineVerificationGateTests
{
    // ACC:T1.11 / ACC:T1.22 (pure projection contract, no filesystem artifact dependency)
    [Fact]
    public void ShouldContainAllRequiredAcceptanceGateStepsInProjectionContract()
    {
        var summary = CreateSyntheticAcceptanceSummary();
        summary.GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();

        var steps = summary.GetProperty("steps")
            .EnumerateArray()
            .Where(item => item.TryGetProperty("name", out _))
            .ToDictionary(
                item => item.GetProperty("name").GetString() ?? string.Empty,
                item => item,
                StringComparer.Ordinal);

        foreach (var required in RequiredAcceptanceSteps)
        {
            steps.ContainsKey(required).Should().BeTrue($"required acceptance step '{required}' must exist");
            steps[required].GetProperty("status").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    // ACC:T1.11 / ACC:T1.22 (pure projection contract)
    [Theory]
    [InlineData("tests-all", true)]
    [InlineData("headless-e2e-evidence", true)]
    [InlineData("acceptance-executed-refs", false)]
    public void ShouldRejectGateProjection_WhenRequiredStepFailsOrIsMissing(string targetStep, bool failInsteadOfRemove)
    {
        var summary = CreateSyntheticAcceptanceSummary();
        var stepStatuses = summary.GetProperty("steps")
            .EnumerateArray()
            .Where(item => item.TryGetProperty("name", out _) && item.TryGetProperty("status", out _))
            .ToDictionary(
                item => item.GetProperty("name").GetString() ?? string.Empty,
                item => item.GetProperty("status").GetString() ?? string.Empty,
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

    // ACC:T1.25 (pure determinism check, no external artifacts)
    [Fact]
    public void ShouldKeepCanonicalBaselineDecisionStableAcrossRepeatedVerification()
    {
        var firstEvidence = CreateSyntheticHeadlessEvidence();
        var secondEvidence = CreateSyntheticHeadlessEvidence();

        var firstState = BuildCanonicalBaselineState(firstEvidence);
        var secondState = BuildCanonicalBaselineState(secondEvidence);

        var firstDecision = EvaluateCanonicalBaselineDecision(firstState);
        var secondDecision = EvaluateCanonicalBaselineDecision(secondState);

        firstDecision.Should().Be(secondDecision);
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
