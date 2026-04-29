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
    private static readonly JsonDocumentOptions JsonOptions = new() { MaxDepth = 128 };
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
        "Tests.Godot/tests/Scenes/Combat/test_enemy_ai_navigation_priority.gd",
        "Tests.Godot/tests/Scenes/Combat/test_enemy_ai_blocked_route_fallback.gd",
        GovernanceTestRef,
    ];

    // ACC:T43.7
    [Fact]
    public void ShouldMapRequirementIdsToExecutableEvidence_WhenTask43AcceptanceIsEvaluated()
    {
        ValidateMasterTaskMetadataForTask43().Should().BeTrue("tasks.json master metadata must stay coherent for Task 43.");

        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateRequirementIds(entry).Should().BeTrue($"{viewPath} must keep ACC:T43.7 mapping complete.");
        }
    }

    // ACC:T43.8
    [Fact]
    public void ShouldKeepTask43ScopeMappingAndEvidenceInsideDeclaredUiSlice_WhenAcceptanceIsEvaluated()
    {
        ValidateMasterTaskMetadataForTask43().Should().BeTrue("tasks.json master metadata must stay coherent for Task 43 scope mapping.");

        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateScopeMapping(entry).Should().BeTrue($"{viewPath} must keep ACC:T43.8 scope-to-evidence mapping complete.");
        }
    }

    // ACC:T43.9
    [Fact]
    public void ShouldRequireBothXunitAndGdunitEvidencePaths_WhenTask43ValidationRefsAreEvaluated()
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

    // ACC:T43.10
    [Fact]
    public void ShouldRequireCombatHudPressureAndCameraOverlayOwnershipEvidence_WhenTask43ClosureIsEvaluated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var closureAcceptance = FindAcceptance(entry.Acceptance, "CombatHud", "PressurePanel", "CameraControlOverlay");
            closureAcceptance.Should().NotBeNull();

            var refs = ParseRefs(closureAcceptance!);
            refs.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            refs.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");
            refs.Should().Contain(GovernanceTestRef);
        }
    }

    // ACC:T43.11
    [Fact]
    public void ShouldRequireRuntimeEvidenceUpgradeFromDocsOnly_WhenChapter7EvidenceIsEvaluated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var runtimeAcceptance = FindAcceptance(entry.Acceptance, "docs-only", "runtime", "planning-only documentation is insufficient");
            runtimeAcceptance.Should().NotBeNull();

            var refs = ParseRefs(runtimeAcceptance!);
            refs.Should().Contain(path => path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase));
            refs.Should().Contain(GovernanceTestRef);
        }
    }

    // ACC:T43.12
    [Fact]
    public void ShouldKeepPendingSurfacesAndGapToCloseEvidenceBackedByRuntimeValidation_WhenClosureArtifactsAreEvaluated()
    {
        var closure = LoadLatestChapter7ClosureContract(taskId: TaskId);
        closure.RequiredEvidenceStatus.Should().NotBeNullOrWhiteSpace();
        closure.RequiredEvidenceStatus.Should().Be("runtime");

        if (closure.ReadyForDone)
        {
            closure.PendingSurfaces.Should().BeEmpty("done-ready closure must not keep unresolved owned surfaces.");
            closure.GapToClose.Should().BeEmpty("done-ready closure must not keep unresolved closure gaps.");
        }
        else
        {
            (closure.PendingSurfaces.Length > 0 || closure.GapToClose.Length > 0)
                .Should()
                .BeTrue("an open closure contract must expose at least one unresolved surface or closure gap.");
        }

        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var closureArtifactsAcceptance = FindAcceptance(entry.Acceptance, "pending_surfaces", "gap_to_close", "runtime validation");
            closureArtifactsAcceptance.Should().NotBeNull();

            var refs = ParseRefs(closureArtifactsAcceptance!);
            refs.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            refs.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");
            refs.Should().Contain(GovernanceTestRef);
        }
    }

    // ACC:T43.13
    [Fact]
    public void ShouldRequireEvidenceBackedStatusTransitionsAcrossTaskViews_WhenTask43StatusIsEvaluated()
    {
        var closure = LoadLatestChapter7ClosureContract(taskId: TaskId);
        var patchStatuses = LoadLatestChapter7StatusPatchStatuses(taskId: TaskId);
        if (patchStatuses.Count > 0)
        {
            patchStatuses.Keys.Should().Contain("tasks_json");
            patchStatuses.Keys.Should().Contain("tasks_back");
            patchStatuses.Keys.Should().Contain("tasks_gameplay");
        }

        var viewStatuses = LoadTask43StatusesFromTaskViews();
        var distinctStatuses = viewStatuses.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        distinctStatuses.Should().HaveCount(1);
        var normalizedStatus = distinctStatuses[0].Trim().ToLowerInvariant();
        normalizedStatus.Should().BeOneOf("pending", "review", "done");
        if (closure.ReadyForDone)
        {
            normalizedStatus.Should().Be("done", "done-ready closure evidence should promote task views into done status.");
        }
        else
        {
            normalizedStatus.Should().NotBe("done", "open closure evidence should not claim done status.");
        }

        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var statusAcceptance = FindAcceptance(entry.Acceptance, "status transitions", "runtime evidence", "task views");
            statusAcceptance.Should().NotBeNull();

            var refs = ParseRefs(statusAcceptance!);
            refs.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            refs.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");
            refs.Should().Contain(GovernanceTestRef);
        }
    }

    [Fact]
    public void ShouldFailRequirementMapping_WhenAnyRequirementIdIsMissing()
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
    public void ShouldFailScopeMapping_WhenScopeEvidenceIsMissing()
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
        using var doc = JsonDocument.Parse(File.ReadAllText(ResolveRepositoryPath(relativePath)), JsonOptions);
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

    private static string ResolveRepositoryRoot()
    {
        var markerPath = ResolveRepositoryPath(".taskmaster/tasks/tasks_back.json");
        return Path.GetFullPath(Path.Combine(markerPath, "..", "..", ".."));
    }

    private static Chapter7ClosureContract LoadLatestChapter7ClosureContract(int taskId)
    {
        var closurePath = ResolveLatestChapter7ArtifactPath("closure-summary.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(closurePath), JsonOptions);
        if (!doc.RootElement.TryGetProperty("slices", out var slices) || slices.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"'{closurePath}' does not contain slices[].");
        }

        foreach (var slice in slices.EnumerateArray())
        {
            if (!slice.TryGetProperty("write_back_contract", out var contract) || contract.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!contract.TryGetProperty("task_id", out var taskIdProperty) || taskIdProperty.ValueKind != JsonValueKind.Number || taskIdProperty.GetInt32() != taskId)
            {
                continue;
            }

            var pendingSurfaces = contract.TryGetProperty("missing_surface_owners", out var missingSurfacesElement) && missingSurfacesElement.ValueKind == JsonValueKind.Array
                ? missingSurfacesElement.EnumerateArray().Select(value => value.GetString() ?? string.Empty).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray()
                : Array.Empty<string>();
            var requiredEvidenceStatus = contract.TryGetProperty("done_when", out var doneWhenElement) &&
                                         doneWhenElement.ValueKind == JsonValueKind.Object &&
                                         doneWhenElement.TryGetProperty("required_evidence_status", out var requiredStatusElement)
                ? (requiredStatusElement.GetString() ?? string.Empty)
                : string.Empty;

            var gapToClose = slice.TryGetProperty("gap_to_close", out var gapElement) && gapElement.ValueKind == JsonValueKind.Array
                ? gapElement.EnumerateArray().Select(value => value.GetString() ?? string.Empty).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray()
                : Array.Empty<string>();

            return new Chapter7ClosureContract(
                ReadyForDone: contract.TryGetProperty("ready_for_done", out var readyElement) && readyElement.ValueKind == JsonValueKind.True,
                PendingSurfaces: pendingSurfaces,
                GapToClose: gapToClose,
                RequiredEvidenceStatus: requiredEvidenceStatus);
        }

        throw new InvalidOperationException($"Task {taskId} write_back_contract was not found in '{closurePath}'.");
    }

    private static Dictionary<string, string> LoadLatestChapter7StatusPatchStatuses(int taskId)
    {
        var statusPatchPath = ResolveLatestChapter7ArtifactPath("task-status-patch.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(statusPatchPath), JsonOptions);
        if (!doc.RootElement.TryGetProperty("operations", out var operations) || operations.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"'{statusPatchPath}' does not contain operations[].");
        }

        var statuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var operation in operations.EnumerateArray())
        {
            if (!operation.TryGetProperty("task_id", out var taskIdElement) || taskIdElement.ValueKind != JsonValueKind.Number || taskIdElement.GetInt32() != taskId)
            {
                continue;
            }

            var view = operation.TryGetProperty("view", out var viewElement) ? (viewElement.GetString() ?? string.Empty).Trim() : string.Empty;
            var toStatus = operation.TryGetProperty("to_status", out var statusElement) ? (statusElement.GetString() ?? string.Empty).Trim() : string.Empty;
            if (!string.IsNullOrWhiteSpace(view) && !string.IsNullOrWhiteSpace(toStatus))
            {
                statuses[view] = toStatus;
            }
        }

        return statuses;
    }

    private static Dictionary<string, string> LoadTask43StatusesFromTaskViews()
    {
        var root = ResolveRepositoryRoot();
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tasks_json"] = LoadTasksJsonStatus(Path.Combine(root, ".taskmaster", "tasks", "tasks.json"), TaskId),
            ["tasks_back"] = LoadTaskViewStatus(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json"), TaskId),
            ["tasks_gameplay"] = LoadTaskViewStatus(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json"), TaskId),
        };
    }

    private static string LoadTasksJsonStatus(string path, int taskId)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), JsonOptions);
        if (!doc.RootElement.TryGetProperty("master", out var master) ||
            master.ValueKind != JsonValueKind.Object ||
            !master.TryGetProperty("tasks", out var tasks) ||
            tasks.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"'{path}' does not contain master.tasks[].");
        }

        foreach (var task in tasks.EnumerateArray())
        {
            if (task.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number && idElement.GetInt32() == taskId)
            {
                return task.TryGetProperty("status", out var statusElement) ? (statusElement.GetString() ?? string.Empty) : string.Empty;
            }
        }

        throw new InvalidOperationException($"Task {taskId} was not found in '{path}'.");
    }

    private static string LoadTaskViewStatus(string path, int taskId)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), JsonOptions);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException($"'{path}' is not a task array.");
        }

        foreach (var task in doc.RootElement.EnumerateArray())
        {
            if (!task.TryGetProperty("taskmaster_id", out var idElement) || idElement.ValueKind != JsonValueKind.Number || idElement.GetInt32() != taskId)
            {
                continue;
            }

            return task.TryGetProperty("status", out var statusElement) ? (statusElement.GetString() ?? string.Empty) : string.Empty;
        }

        throw new InvalidOperationException($"Task {taskId} was not found in '{path}'.");
    }

    private static string ResolveLatestChapter7ArtifactPath(string fileName)
    {
        var root = ResolveRepositoryRoot();
        var logsRoot = new DirectoryInfo(Path.Combine(root, "logs", "ci"));
        if (!logsRoot.Exists)
        {
            throw new DirectoryNotFoundException($"logs/ci was not found under '{root}'.");
        }

        var candidates = logsRoot
            .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
            .Select(dir => Path.Combine(dir.FullName, "chapter7-ui-wiring", fileName))
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new FileNotFoundException($"No chapter7 artifact '{fileName}' was found under logs/ci/*/chapter7-ui-wiring.");
        }

        return candidates[0];
    }

    private static bool ValidateMasterTaskMetadataForTask43()
    {
        var path = ResolveRepositoryPath(".taskmaster/tasks/tasks.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path), JsonOptions);
        if (!doc.RootElement.TryGetProperty("master", out var master) ||
            master.ValueKind != JsonValueKind.Object ||
            !master.TryGetProperty("tasks", out var tasks) ||
            tasks.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var task in tasks.EnumerateArray())
        {
            if (!task.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.Number || idElement.GetInt32() != TaskId)
            {
                continue;
            }

            var dependencySet = (task.TryGetProperty("dependencies", out var dependenciesElement) && dependenciesElement.ValueKind == JsonValueKind.Array)
                ? dependenciesElement.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Number).Select(item => item.GetInt32()).ToHashSet()
                : new HashSet<int>();

            var details = task.TryGetProperty("details", out var detailsElement) ? (detailsElement.GetString() ?? string.Empty) : string.Empty;
            var strategy = task.TryGetProperty("testStrategy", out var strategyElement) ? (strategyElement.GetString() ?? string.Empty) : string.Empty;

            return dependencySet.SetEquals([4, 5, 6, 20, 22]) &&
                   details.Contains("CombatHud", StringComparison.OrdinalIgnoreCase) &&
                   details.Contains("PressurePanel", StringComparison.OrdinalIgnoreCase) &&
                   details.Contains("CameraControlOverlay", StringComparison.OrdinalIgnoreCase) &&
                   strategy.Contains("runtime evidence", StringComparison.OrdinalIgnoreCase);
        }

        return false;
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

    private sealed record Chapter7ClosureContract(
        bool ReadyForDone,
        string[] PendingSurfaces,
        string[] GapToClose,
        string RequiredEvidenceStatus);
}
