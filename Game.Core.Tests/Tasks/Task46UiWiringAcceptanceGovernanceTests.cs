using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task46UiWiringAcceptanceGovernanceTests
{
    private const int TaskId = 46;
    private static readonly Regex RefsRegex = new(@"\bRefs\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScopeTokenRegex = new(@"\bT\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] RequiredRequirementIds = ["RQ-CONFIG-CONTRACT-GOV", "RQ-CORE-LOOP-STATE"];
    private static readonly string[] RequiredScopeItems = ["T02", "T31", "T32", "T33", "T34", "T35", "T36", "T37", "T38", "T39", "T40"];
    private const string GovernanceTestRef = "Game.Core.Tests/Tasks/Task46UiWiringAcceptanceGovernanceTests.cs";

    private static readonly Dictionary<string, string[]> RequirementEvidenceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RQ-CONFIG-CONTRACT-GOV"] =
        [
            "Tests.Godot/tests/Security/Hard/test_settings_config_security.gd",
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["RQ-CORE-LOOP-STATE"] =
        [
            "Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd",
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
    };

    private static readonly Dictionary<string, string[]> ScopeEvidenceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["T02"] =
        [
            "Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd",
            "Tests.Godot/tests/Security/Hard/test_settings_config_security.gd",
            "Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd",
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T31"] =
        [
            "Tests.Godot/tests/Security/Hard/test_settings_config_security.gd",
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T32"] =
        [
            "Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd",
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T33"] =
        [
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T34"] =
        [
            "Game.Core.Tests/Domain/GameConfigTests.cs",
            "Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd",
        ],
        ["T35"] =
        [
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T36"] =
        [
            "Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd",
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T37"] =
        [
            "Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd",
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T38"] =
        [
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T39"] =
        [
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
        ["T40"] =
        [
            "Game.Core.Tests/Domain/GameConfigTests.cs",
        ],
    };

    private static readonly string[] AllowedTaskRefs =
    [
        "Tests.Godot/tests/UI/test_hud_config_audit_surfaces.gd",
        "Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd",
        "Game.Core.Tests/Domain/GameConfigTests.cs",
        "Tests.Godot/tests/Security/Hard/test_settings_config_security.gd",
        "Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd",
        GovernanceTestRef,
    ];

    // ACC:T46.7
    [Fact]
    public void ShouldMapRequirementIdsToExecutableEvidence_WhenTask46GovernanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateRequirementIds(entry).Should().BeTrue($"{viewPath} must keep ACC:T46.7 mapping complete.");
        }
    }

    // ACC:T46.8
    [Fact]
    public void ShouldKeepTask46ScopeMappingAndEvidenceInsideDeclaredUiSlice_WhenGovernanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateScopeMapping(entry).Should().BeTrue($"{viewPath} must keep ACC:T46.8 scope-to-evidence mapping complete.");
        }
    }

    // ACC:T46.9
    [Fact]
    public void ShouldRequireTask46ValidationRefsToContainBothGdunitAndXunit_WhenAcceptanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var validationAcceptance = FindAcceptance(entry.Acceptance, "both referenced GdUnit and xUnit evidence paths");
            validationAcceptance.Should().NotBeNull();

            var refs = ParseRefs(validationAcceptance!).ToArray();
            refs.Should().Contain(path => path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase));
            refs.Should().Contain(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
            validationAcceptance.Should().Contain("pass/fail semantics", because: "ACC:T46.9 requires explicit governance-level pass/fail semantics.");
            validationAcceptance.Should().Contain("both frameworks", because: "ACC:T46.9 must keep cross-framework validation semantics auditable.");
        }
    }

    // ACC:T46.6
    [Fact]
    public void ShouldRequireTask46CompletionAcceptanceToKeepArtifactPathChecklist_WhenGovernanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var completionAcceptance = FindAcceptance(
                entry.Acceptance,
                "logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json",
                "logs/ci/<YYYY-MM-DD>/overlay-lint/report.json",
                "logs/ci/<YYYY-MM-DD>/config-governance/report.json");
            completionAcceptance.Should().NotBeNull($"{viewPath} must keep ACC:T46.6 artifact checklist auditable.");
        }
    }

    [Fact]
    public void ShouldKeepTask46ArtifactChecklistStructuredAsCiReportJsonPaths_WhenGovernanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var completionAcceptance = FindAcceptance(
                entry.Acceptance,
                "logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json",
                "logs/ci/<YYYY-MM-DD>/overlay-lint/report.json",
                "logs/ci/<YYYY-MM-DD>/config-governance/report.json");
            completionAcceptance.Should().NotBeNull();

            var refs = ParseRefs(completionAcceptance!).ToArray();
            refs.Should().Contain(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase), because: "ACC:T46.6 checklist semantics are guarded by governance tests.");
            refs.Should().Contain(GovernanceTestRef);
        }
    }

    [Fact]
    public void ShouldFailRequirementMapping_WhenAnyRequirementIdIsMissing()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var requirementAcceptance = FindAcceptance(entry.Acceptance, "Requirement IDs", "RQ-CONFIG-CONTRACT-GOV", "RQ-CORE-LOOP-STATE");
            requirementAcceptance.Should().NotBeNull();

            var mutatedAcceptance = requirementAcceptance!
                .Replace("RQ-CONFIG-CONTRACT-GOV", string.Empty, StringComparison.Ordinal)
                .Replace("RQ-CORE-LOOP-STATE", string.Empty, StringComparison.Ordinal);
            var mutated = entry.ReplaceAcceptance(requirementAcceptance!, mutatedAcceptance);
            ValidateRequirementIds(mutated).Should().BeFalse($"{viewPath} should fail ACC:T46.7 when IDs are missing.");
        }
    }

    [Fact]
    public void ShouldFailScopeMapping_WhenRequiredScopeEvidenceIsRemoved()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var missingScopeEvidence = entry.RemoveTestRef("Tests.Godot/tests/Security/Hard/test_settings_config_security.gd");
            ValidateScopeMapping(missingScopeEvidence).Should().BeFalse($"{viewPath} should fail ACC:T46.8 when required scope evidence is removed.");
        }
    }

    [Fact]
    public void ShouldFailScopeMapping_WhenOutOfScopeTokenIsInjected()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var scopeAcceptance = FindAcceptance(entry.Acceptance, "T02, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40");
            scopeAcceptance.Should().NotBeNull();

            var mutatedScopeAcceptance = $"{scopeAcceptance} Additional mapping: T41.";
            var mutated = entry.ReplaceAcceptance(scopeAcceptance!, mutatedScopeAcceptance);
            ValidateScopeMapping(mutated).Should().BeFalse($"{viewPath} should reject out-of-scope token T41 for ACC:T46.8.");
        }
    }

    [Fact]
    public void ShouldFailCompletionChecklistValidation_WhenAnyArtifactPathIsMissing()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var completionAcceptance = FindAcceptance(
                entry.Acceptance,
                "logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json",
                "logs/ci/<YYYY-MM-DD>/overlay-lint/report.json",
                "logs/ci/<YYYY-MM-DD>/config-governance/report.json");
            completionAcceptance.Should().NotBeNull();

            var mutatedAcceptance = completionAcceptance!
                .Replace("logs/ci/<YYYY-MM-DD>/overlay-lint/report.json", string.Empty, StringComparison.Ordinal);
            var mutated = entry.ReplaceAcceptance(completionAcceptance!, mutatedAcceptance);
            var mutatedCompletion = FindAcceptance(
                mutated.Acceptance,
                "logs/ci/<YYYY-MM-DD>/task-triplet-audit/report.json",
                "logs/ci/<YYYY-MM-DD>/overlay-lint/report.json",
                "logs/ci/<YYYY-MM-DD>/config-governance/report.json");
            mutatedCompletion.Should().BeNull($"{viewPath} should fail ACC:T46.6 when artifact checklist path is missing.");
        }
    }

    [Fact]
    public void ShouldFailValidationEvidence_WhenGdunitRefIsRemoved()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var validationAcceptance = FindAcceptance(entry.Acceptance, "both referenced GdUnit and xUnit evidence paths");
            validationAcceptance.Should().NotBeNull();

            var mutatedAcceptance = validationAcceptance!
                .Replace("Tests.Godot/tests/UI/test_hud_config_audit_surfaces.gd", string.Empty, StringComparison.Ordinal)
                .Replace("Tests.Godot/tests/Adapters/Config/test_settings_persistence.gd", string.Empty, StringComparison.Ordinal);
            mutatedAcceptance = mutatedAcceptance
                .Replace("Tests.Godot/tests/Security/Hard/test_settings_config_security.gd", string.Empty, StringComparison.Ordinal)
                .Replace("Tests.Godot/tests/Integration/test_balance_runtime_config_reload.gd", string.Empty, StringComparison.Ordinal);
            var mutated = entry.ReplaceAcceptance(validationAcceptance!, mutatedAcceptance);
            var mutatedRefs = ParseRefs(mutatedAcceptance).ToArray();
            mutatedRefs.Should().NotContain(path => path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase), $"{viewPath} should fail ACC:T46.9 when GdUnit evidence is removed.");
        }
    }

    [Fact]
    public void ShouldFailValidationEvidence_WhenXunitRefIsRemoved()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var validationAcceptance = FindAcceptance(entry.Acceptance, "both referenced GdUnit and xUnit evidence paths");
            validationAcceptance.Should().NotBeNull();

            var mutatedAcceptance = validationAcceptance!
                .Replace("Game.Core.Tests/Domain/GameConfigTests.cs", string.Empty, StringComparison.Ordinal)
                .Replace("Game.Core.Tests/Tasks/Task46UiWiringAcceptanceGovernanceTests.cs", string.Empty, StringComparison.Ordinal);
            var mutated = entry.ReplaceAcceptance(validationAcceptance!, mutatedAcceptance);
            var mutatedRefs = ParseRefs(mutatedAcceptance).ToArray();
            mutatedRefs.Should().NotContain(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase), $"{viewPath} should fail ACC:T46.9 when xUnit evidence is removed.");
        }
    }

    private static IEnumerable<string> ViewPaths()
    {
        yield return ".taskmaster/tasks/tasks_back.json";
        yield return ".taskmaster/tasks/tasks_gameplay.json";
    }

    private static Task46TaskEntry LoadTaskEntry(string relativePath, int taskId)
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
                return new Task46TaskEntry(scopeIds, acceptance, testRefs);
            }
        }

        throw new InvalidOperationException($"Task {taskId} was not found in '{relativePath}'.");
    }

    private static bool ValidateRequirementIds(Task46TaskEntry entry)
    {
        var requirementAcceptance = FindAcceptance(entry.Acceptance, "Requirement IDs", "RQ-CONFIG-CONTRACT-GOV", "RQ-CORE-LOOP-STATE");
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

    private static bool ValidateScopeMapping(Task46TaskEntry entry)
    {
        if (!entry.ScopeTaskIds.SequenceEqual([2, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40]))
        {
            return false;
        }

        var scopeAcceptance = FindAcceptance(entry.Acceptance, "T02, T31, T32, T33, T34, T35, T36, T37, T38, T39, T40");
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

        throw new DirectoryNotFoundException("Unable to locate repository root from test base directory.");
    }

    private sealed record Task46TaskEntry(int[] ScopeTaskIds, string[] Acceptance, string[] TestRefs)
    {
        public Task46TaskEntry ReplaceAcceptance(string current, string replacement)
        {
            var updated = Acceptance
                .Select(line => string.Equals(line, current, StringComparison.Ordinal) ? replacement : line)
                .ToArray();
            return this with { Acceptance = updated };
        }

        public Task46TaskEntry RemoveTestRef(string testRef)
        {
            var updated = TestRefs
                .Where(path => !string.Equals(path, Normalize(testRef), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return this with { TestRefs = updated };
        }
    }
}
