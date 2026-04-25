using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task43UiWiringAcceptanceGovernanceTests
{
    private const int TaskId = 43;
    private static readonly Regex RefsRegex = new(@"\bRefs\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] RequiredRequirementIds = ["RQ-CAMERA-SCROLL", "RQ-COMBAT-QUEUE-TECH", "RQ-CORE-LOOP-STATE"];
    private static readonly string[] RequiredScopeItems = ["T04", "T05", "T06", "T20", "T22"];
    private const string GovernanceTestRef = "Game.Core.Tests/Tasks/Task43UiWiringAcceptanceGovernanceTests.cs";

    private static readonly Dictionary<string, string[]> RequirementEvidenceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RQ-CAMERA-SCROLL"] =
        [
            "Tests.Godot/tests/Scenes/Camera/test_camera_controller_scroll_inputs.gd",
        ],
        ["RQ-COMBAT-QUEUE-TECH"] =
        [
            "Game.Core.Tests/Services/EnemyAiTargetSelectionTests.cs",
            "Game.Core.Tests/Engine/GameEngineCoreEventTests.cs",
        ],
        ["RQ-CORE-LOOP-STATE"] =
        [
            "Game.Core.Tests/Engine/GameEngineCoreDeterminismTests.cs",
            "Game.Core.Tests/Services/WaveManagerDeterminismTests.cs",
        ],
    };

    private static readonly Dictionary<string, string[]> ScopeEvidenceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["T04"] =
        [
            "Tests.Godot/tests/Scenes/Camera/test_camera_controller_scroll_inputs.gd",
        ],
        ["T05"] =
        [
            "Game.Core.Tests/Services/EnemyAiTargetSelectionTests.cs",
        ],
        ["T06"] =
        [
            "Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs",
            "Game.Core.Tests/Services/WaveBudgetAllocatorTests.cs",
        ],
        ["T20"] =
        [
            "Tests.Godot/tests/UI/test_hud_updates_on_events.gd",
            "Tests.Godot/tests/UI/test_hud_scene.gd",
        ],
        ["T22"] =
        [
            "Game.Core.Tests/Engine/GameEngineCoreDeterminismTests.cs",
            "Game.Core.Tests/Services/WaveManagerDeterminismTests.cs",
        ],
    };

    private static readonly string[] AllowedTaskRefs =
    [
        "Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs",
        "Game.Core.Tests/Services/WaveManagerDeterminismTests.cs",
        "Game.Core.Tests/Engine/GameEngineCoreDeterminismTests.cs",
        "Game.Core.Tests/Services/WaveBudgetAllocatorTests.cs",
        "Game.Core.Tests/Services/EnemyAiTargetSelectionTests.cs",
        "Game.Core.Tests/Engine/GameEngineCoreEventTests.cs",
        "Tests.Godot/tests/UI/test_hud_scene.gd",
        "Tests.Godot/tests/UI/test_hud_updates_on_events.gd",
        "Tests.Godot/tests/Scenes/Camera/test_camera_controller_scroll_inputs.gd",
        GovernanceTestRef,
    ];

    // ACC:T43.7
    [Fact]
    public void ShouldMapRequirementIdsToExecutableEvidenceForTask43()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateRequirementIds(entry).Should().BeTrue($"{viewPath} must keep ACC:T43.7 mapping complete.");
        }
    }

    // ACC:T43.8
    [Fact]
    public void ShouldKeepTask43ScopeMappingAndEvidenceInsideDeclaredUiSlice()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateScopeMapping(entry).Should().BeTrue($"{viewPath} must keep ACC:T43.8 scope-to-evidence mapping complete.");
        }
    }

    // ACC:T43.9
    [Fact]
    public void ShouldRequireBothXunitAndGdunitEvidencePathsInTask43ValidationRefs()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            entry.TestRefs.Should().Contain(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
            entry.TestRefs.Should().Contain(path => path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase));

            var validationAcceptance = FindAcceptance(
                entry.Acceptance,
                "both xUnit and GdUnit evidence paths",
                "governance checks remain distinct");
            validationAcceptance.Should().NotBeNull();

            var validationRefs = ParseRefs(validationAcceptance!).ToArray();
            validationRefs.Should().Contain(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
            validationRefs.Should().Contain(path => path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void ShouldFailRequirementMappingWhenAnyRequirementIdIsMissing()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var requirementAcceptance = FindAcceptance(entry.Acceptance, "Requirement IDs", "RQ-CAMERA-SCROLL", "RQ-COMBAT-QUEUE-TECH", "RQ-CORE-LOOP-STATE");
            requirementAcceptance.Should().NotBeNull();

            var mutatedAcceptance = requirementAcceptance!
                .Replace("RQ-CAMERA-SCROLL", string.Empty, StringComparison.Ordinal)
                .Replace("RQ-COMBAT-QUEUE-TECH", string.Empty, StringComparison.Ordinal)
                .Replace("RQ-CORE-LOOP-STATE", string.Empty, StringComparison.Ordinal);

            var mutated = entry.ReplaceAcceptance(requirementAcceptance!, mutatedAcceptance);
            ValidateRequirementIds(mutated).Should().BeFalse($"{viewPath} should fail ACC:T43.7 when IDs are missing.");
        }
    }

    [Fact]
    public void ShouldFailScopeMappingWhenScopeEvidenceIsMissing()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var missingCameraEvidence = entry.RemoveTestRef("Tests.Godot/tests/Scenes/Camera/test_camera_controller_scroll_inputs.gd");
            ValidateScopeMapping(missingCameraEvidence).Should().BeFalse($"{viewPath} should fail ACC:T43.8 when T04 evidence is removed.");
        }
    }

    private static IEnumerable<string> ViewPaths()
    {
        yield return ".taskmaster/tasks/tasks_back.json";
        yield return ".taskmaster/tasks/tasks_gameplay.json";
    }

    private static Task43TaskEntry LoadTaskEntry(string relativePath, int taskId)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ResolveRepositoryPath(relativePath)));
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (item.TryGetProperty("taskmaster_id", out var taskmasterIdProperty) &&
                taskmasterIdProperty.ValueKind == JsonValueKind.Number &&
                taskmasterIdProperty.GetInt32() == taskId)
            {
                var scopeIds = item.GetProperty("ui_wiring_candidate").GetProperty("scope_task_ids")
                    .EnumerateArray()
                    .Select(scope => scope.GetInt32())
                    .ToArray();
                var acceptance = item.GetProperty("acceptance").EnumerateArray().Select(line => line.GetString() ?? string.Empty).ToArray();
                var testRefs = item.GetProperty("test_refs").EnumerateArray().Select(path => Normalize(path.GetString())).ToArray();
                return new Task43TaskEntry(scopeIds, acceptance, testRefs);
            }
        }

        throw new InvalidOperationException($"Task {taskId} was not found in '{relativePath}'.");
    }

    private static bool ValidateRequirementIds(Task43TaskEntry entry)
    {
        var requirementAcceptance = FindAcceptance(entry.Acceptance, "Requirement IDs", "RQ-CAMERA-SCROLL", "RQ-COMBAT-QUEUE-TECH", "RQ-CORE-LOOP-STATE");
        if (requirementAcceptance is null)
        {
            return false;
        }

        var requirementRefs = ParseRefs(requirementAcceptance).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var requirementId in RequiredRequirementIds)
        {
            if (!ContainsToken(requirementAcceptance, requirementId))
            {
                return false;
            }

            var expectedRefs = RequirementEvidenceMap[requirementId];
            var hasMappedEvidence = expectedRefs.Any(expected =>
                requirementRefs.Contains(expected) &&
                entry.TestRefs.Contains(expected, StringComparer.OrdinalIgnoreCase));
            if (!hasMappedEvidence)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateScopeMapping(Task43TaskEntry entry)
    {
        if (!entry.ScopeTaskIds.SequenceEqual([4, 5, 6, 20, 22]))
        {
            return false;
        }

        var scopeAcceptance = FindAcceptance(entry.Acceptance, "T04/T05/T06/T20/T22", "scope items");
        if (scopeAcceptance is null)
        {
            return false;
        }

        foreach (var scopeId in RequiredScopeItems)
        {
            if (!ContainsToken(scopeAcceptance, scopeId))
            {
                return false;
            }

            var mappedRefs = ScopeEvidenceMap[scopeId];
            var hasMappedEvidence = mappedRefs.Any(mapped => entry.TestRefs.Contains(mapped, StringComparer.OrdinalIgnoreCase));
            if (!hasMappedEvidence)
            {
                return false;
            }
        }

        var scopeRefs = ParseRefs(scopeAcceptance);
        if (!scopeRefs.Contains(GovernanceTestRef, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var taskRefs = entry.TestRefs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return taskRefs.SetEquals(AllowedTaskRefs);
    }

    private static string? FindAcceptance(IEnumerable<string> acceptance, params string[] requiredTokens)
    {
        foreach (var line in acceptance)
        {
            var matchesAll = requiredTokens.All(token => ContainsToken(line, token));
            if (matchesAll)
            {
                return line;
            }
        }

        return null;
    }

    private static bool ContainsToken(string text, string token)
    {
        return text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static IReadOnlyList<string> ParseRefs(string acceptanceText)
    {
        var match = RefsRegex.Match(acceptanceText ?? string.Empty);
        if (!match.Success)
        {
            return Array.Empty<string>();
        }

        return match.Groups[1].Value
            .Replace("`", " ")
            .Replace(",", " ")
            .Replace(";", " ")
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .ToArray();
    }

    private static string Normalize(string? path)
    {
        return (path ?? string.Empty).Replace('\\', '/').Trim();
    }

    private static string ResolveRepositoryPath(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var marker = Path.Combine(current.FullName, ".taskmaster", "tasks", "tasks_back.json");
            if (File.Exists(marker))
            {
                return Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root from test base directory.");
    }

    private sealed record Task43TaskEntry(int[] ScopeTaskIds, string[] Acceptance, string[] TestRefs)
    {
        public Task43TaskEntry ReplaceAcceptance(string current, string replacement)
        {
            var updated = Acceptance
                .Select(line => string.Equals(line, current, StringComparison.Ordinal) ? replacement : line)
                .ToArray();
            return this with { Acceptance = updated };
        }

        public Task43TaskEntry RemoveTestRef(string testRef)
        {
            var updated = TestRefs
                .Where(path => !string.Equals(path, Normalize(testRef), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return this with { TestRefs = updated };
        }
    }
}
