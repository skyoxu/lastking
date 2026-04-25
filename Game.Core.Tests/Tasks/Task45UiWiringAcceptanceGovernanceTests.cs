using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task45UiWiringAcceptanceGovernanceTests
{
    private const int TaskId = 45;
    private static readonly Regex RefsRegex = new(@"\bRefs\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScopeTokenRegex = new(@"\bT\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] RequiredScopeItems = ["T25", "T26", "T27", "T28", "T29", "T30"];
    private static readonly string[] RequiredRequirementIds = ["RQ-I18N-LANG-SWITCH", "RQ-AUDIO-CHANNEL-SETTINGS", "RQ-PERF-GATE", "RQ-SAVE-MIGRATION-CLOUD"];
    private const string GovernanceTestRef = "Game.Core.Tests/Tasks/Task45UiWiringAcceptanceGovernanceTests.cs";

    private static readonly Dictionary<string, string[]> RequirementEvidenceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RQ-I18N-LANG-SWITCH"] =
        [
            "Tests.Godot/tests/UI/test_settings_locale.gd",
        ],
        ["RQ-AUDIO-CHANNEL-SETTINGS"] =
        [
            "Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd",
        ],
        ["RQ-PERF-GATE"] =
        [
            "Game.Core.Tests/Services/FrameTimeHotspotRankingTests.cs",
        ],
        ["RQ-SAVE-MIGRATION-CLOUD"] =
        [
            "Tests.Godot/tests/Integration/test_save_manager_steam_cloud_sync_flow.gd",
            "Tests.Godot/tests/Integration/test_backup_restore_savegame.gd",
        ],
    };

    private static readonly Dictionary<string, string[]> ScopeEvidenceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["T25"] =
        [
            "Tests.Godot/tests/Adapters/Save/test_save_manager_autosave_slot_path.gd",
            "Tests.Godot/tests/Adapters/Save/test_save_manager_daystart_autosave.gd",
            "Tests.Godot/tests/Adapters/Save/test_save_manager_load_failures_feedback.gd",
        ],
        ["T26"] =
        [
            "Tests.Godot/tests/Integration/test_save_manager_steam_cloud_sync_flow.gd",
            "Tests.Godot/tests/Integration/test_backup_restore_savegame.gd",
            "Game.Core.Tests/Services/CloudSaveConflictResolverTests.cs",
            "Game.Core.Tests/Services/SaveManagerWorkflowTests.cs",
        ],
        ["T27"] =
        [
            "Tests.Godot/tests/Integration/test_achievements_end_to_end_unlock_and_sync.gd",
            "Tests.Godot/tests/UI/test_achievements_list_visible_on_session_start.gd",
        ],
        ["T28"] =
        [
            "Tests.Godot/tests/UI/test_settings_locale.gd",
            "Tests.Godot/tests/UI/test_hud_error_dialog.gd",
            "Tests.Godot/tests/Integration/test_load_failure_feedback_flow.gd",
        ],
        ["T29"] =
        [
            "Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd",
        ],
        ["T30"] =
        [
            "Game.Core.Tests/Services/FrameTimeHotspotRankingTests.cs",
        ],
    };

    private static readonly string[] AllowedTaskRefs =
    [
        "Tests.Godot/tests/Adapters/Save/test_save_manager_autosave_slot_path.gd",
        "Tests.Godot/tests/Adapters/Save/test_save_manager_daystart_autosave.gd",
        "Tests.Godot/tests/Adapters/Save/test_save_manager_load_failures_feedback.gd",
        "Tests.Godot/tests/Adapters/Db/test_savegame_update_overwrite_cross_restart.gd",
        "Tests.Godot/tests/Adapters/Db/test_savegame_persistence_cross_restart.gd",
        "Tests.Godot/tests/Integration/test_save_manager_steam_cloud_sync_flow.gd",
        "Tests.Godot/tests/Integration/test_backup_restore_savegame.gd",
        "Tests.Godot/tests/Integration/test_achievements_end_to_end_unlock_and_sync.gd",
        "Tests.Godot/tests/UI/test_achievements_list_visible_on_session_start.gd",
        "Tests.Godot/tests/UI/test_settings_locale.gd",
        "Tests.Godot/tests/UI/test_hud_scene.gd",
        "Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd",
        "Game.Core.Tests/Services/CloudSaveConflictResolverTests.cs",
        "Game.Core.Tests/Services/FrameTimeHotspotRankingTests.cs",
        "Game.Core.Tests/Services/SaveManagerWorkflowTests.cs",
        GovernanceTestRef,
    ];

    // ACC:T45.7
    [Fact]
    public void ShouldMapRequirementIdsToExecutableEvidence_WhenTask45GovernanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateRequirementIds(entry).Should().BeTrue($"{viewPath} must keep ACC:T45.7 mapping complete.");
        }
    }

    // ACC:T45.8
    [Fact]
    public void ShouldKeepTask45ScopeMappingAndEvidenceInsideDeclaredUiSlice_WhenGovernanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateScopeMapping(entry).Should().BeTrue($"{viewPath} must keep ACC:T45.8 scope-to-evidence mapping complete.");
        }
    }

    // ACC:T45.9
    [Fact]
    public void ShouldRequireTask45ValidationRefsToDeclareBothGdUnitAndXunitNa_WhenAcceptanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var validationAcceptance = FindAcceptance(entry.Acceptance, "GdUnit suite", "xUnit N/A");
            if (validationAcceptance is null)
            {
                validationAcceptance = FindAcceptance(entry.Acceptance, "GdUnit and xUnit suites", "N/A");
            }
            validationAcceptance.Should().NotBeNull();

            var validationRefs = ParseRefs(validationAcceptance!).ToArray();
            validationRefs.Should().Contain(path => path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase));
            validationRefs.Should().Contain(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
            validationAcceptance.Should().Contain("xUnit", because: "Task 45 requires explicit xUnit N/A semantics.");
            validationAcceptance.Should().Contain("N/A", because: "Task 45 acceptance explicitly requires N/A rationale for non-applicable framework.");
        }
    }

    [Fact]
    public void ShouldFailRequirementMapping_WhenAnyRequirementIdIsMissing()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var requirementAcceptance = FindAcceptance(
                entry.Acceptance,
                "Requirement ID",
                "RQ-I18N-LANG-SWITCH",
                "RQ-AUDIO-CHANNEL-SETTINGS",
                "RQ-PERF-GATE",
                "RQ-SAVE-MIGRATION-CLOUD");
            requirementAcceptance.Should().NotBeNull();

            var mutatedAcceptance = requirementAcceptance!
                .Replace("RQ-I18N-LANG-SWITCH", string.Empty, StringComparison.Ordinal)
                .Replace("RQ-AUDIO-CHANNEL-SETTINGS", string.Empty, StringComparison.Ordinal)
                .Replace("RQ-PERF-GATE", string.Empty, StringComparison.Ordinal)
                .Replace("RQ-SAVE-MIGRATION-CLOUD", string.Empty, StringComparison.Ordinal);

            var mutated = entry.ReplaceAcceptance(requirementAcceptance!, mutatedAcceptance);
            ValidateRequirementIds(mutated).Should().BeFalse($"{viewPath} should fail ACC:T45.7 when IDs are missing.");
        }
    }

    [Fact]
    public void ShouldFailScopeMapping_WhenOutOfScopeTokenIsInjected()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var scopeAcceptance = FindAcceptance(entry.Acceptance, "T25, T26, T27, T28, T29, T30", "scope mapping");
            scopeAcceptance.Should().NotBeNull();

            var mutatedScopeAcceptance = $"{scopeAcceptance} Additional mapping: T31.";
            var mutated = entry.ReplaceAcceptance(scopeAcceptance!, mutatedScopeAcceptance);
            ValidateScopeMapping(mutated).Should().BeFalse($"{viewPath} should reject out-of-scope token T31 for ACC:T45.8.");
        }
    }

    [Fact]
    public void ShouldFailScopeMapping_WhenRequiredScopeEvidenceIsRemoved()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var missingScopeEvidence = entry.RemoveTestRef("Tests.Godot/tests/Adapters/Save/test_save_manager_load_failures_feedback.gd");
            ValidateScopeMapping(missingScopeEvidence).Should().BeFalse($"{viewPath} should fail ACC:T45.8 when required scope evidence is removed.");
        }
    }

    [Fact]
    public void ShouldRequireTask45CompletionAcceptanceToKeepArtifactPathChecklist()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var completionAcceptance = FindAcceptance(
                entry.Acceptance,
                "save-migration/report.json",
                "steam-cloud/report.json",
                "achievements/report.json",
                "settings/summary.json");

            completionAcceptance.Should().NotBeNull($"{viewPath} must keep ACC:T45.6 artifact checklist auditable.");
            completionAcceptance.Should().Contain("logs/ci/<YYYY-MM-DD>/save-migration/report.json");
            completionAcceptance.Should().Contain("logs/ci/<YYYY-MM-DD>/steam-cloud/report.json");
            completionAcceptance.Should().Contain("logs/ci/<YYYY-MM-DD>/achievements/report.json");
            completionAcceptance.Should().Contain("logs/e2e/<YYYY-MM-DD>/settings/summary.json");

            var refs = ParseRefs(completionAcceptance!).ToArray();
            refs.Should().Contain(path => path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static IEnumerable<string> ViewPaths()
    {
        yield return ".taskmaster/tasks/tasks_back.json";
        yield return ".taskmaster/tasks/tasks_gameplay.json";
    }

    private static Task45TaskEntry LoadTaskEntry(string relativePath, int taskId)
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
                return new Task45TaskEntry(scopeIds, acceptance, testRefs);
            }
        }

        throw new InvalidOperationException($"Task {taskId} was not found in '{relativePath}'.");
    }

    private static bool ValidateRequirementIds(Task45TaskEntry entry)
    {
        var requirementAcceptance = FindAcceptance(
            entry.Acceptance,
            "Requirement ID",
            "RQ-I18N-LANG-SWITCH",
            "RQ-AUDIO-CHANNEL-SETTINGS",
            "RQ-PERF-GATE",
            "RQ-SAVE-MIGRATION-CLOUD");
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

    private static bool ValidateScopeMapping(Task45TaskEntry entry)
    {
        if (!entry.ScopeTaskIds.SequenceEqual([25, 26, 27, 28, 29, 30]))
        {
            return false;
        }

        var scopeAcceptance = FindAcceptance(entry.Acceptance, "T25, T26, T27, T28, T29, T30", "scope mapping");
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

        var scopeTokenSet = ScopeTokenRegex.Matches(scopeAcceptance)
            .Select(match => match.Value.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowedScopeTokenSet = RequiredScopeItems.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!scopeTokenSet.SetEquals(allowedScopeTokenSet))
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

        throw new InvalidOperationException("Unable to locate repository root from test output directory.");
    }

    private sealed record Task45TaskEntry(int[] ScopeTaskIds, string[] Acceptance, string[] TestRefs)
    {
        public Task45TaskEntry ReplaceAcceptance(string original, string replacement)
        {
            var cloned = Acceptance.ToArray();
            var index = Array.FindIndex(cloned, line => string.Equals(line, original, StringComparison.Ordinal));
            if (index >= 0)
            {
                cloned[index] = replacement;
            }

            return this with { Acceptance = cloned };
        }

        public Task45TaskEntry RemoveTestRef(string testRef)
        {
            var updated = TestRefs
                .Where(path => !string.Equals(path, Normalize(testRef), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return this with { TestRefs = updated };
        }
    }
}
