using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task44UiWiringAcceptanceGovernanceTests
{
    private const int TaskId = 44;
    private static readonly Regex RefsRegex = new(@"\bRefs\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScopeTokenRegex = new(@"\bT\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] RequiredScopeItems = ["T12", "T13", "T14", "T15", "T16", "T17"];
    private static readonly string[] RequiredRequirementIds = ["RQ-COMBAT-QUEUE-TECH", "RQ-ECONOMY-BUILD-RULES"];
    private const string GovernanceTestRef = "Game.Core.Tests/Tasks/Task44UiWiringAcceptanceGovernanceTests.cs";

    private static readonly string[] AllowedTaskRefs =
    [
        "Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs",
        "Game.Core.Tests/Engine/GameEngineCoreEventTests.cs",
        "Game.Core.Tests/Services/ResourceManagerTests.cs",
        "Game.Core.Tests/Services/ResourceManagerEventTests.cs",
        GovernanceTestRef,
    ];

    private static readonly Dictionary<string, string[]> RequirementEvidenceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RQ-COMBAT-QUEUE-TECH"] =
        [
            "Game.Core.Tests/Engine/GameEngineCoreEventTests.cs",
            "Game.Core.Tests/Services/ResourceManagerEventTests.cs",
        ],
        ["RQ-ECONOMY-BUILD-RULES"] =
        [
            "Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs",
            "Game.Core.Tests/Services/ResourceManagerTests.cs",
        ],
    };

    private static readonly Dictionary<string, string[]> ScopeEvidenceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["T12"] =
        [
            "Game.Core.Tests/Services/ResourceManagerTests.cs",
            "Game.Core.Tests/Services/ResourceManagerEventTests.cs",
        ],
        ["T13"] =
        [
            "Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs",
        ],
        ["T14"] =
        [
            "Game.Core.Tests/Services/ResourceManagerEventTests.cs",
        ],
        ["T15"] =
        [
            "Game.Core.Tests/Engine/GameEngineCoreEventTests.cs",
        ],
        ["T16"] =
        [
            "Game.Core.Tests/Services/ResourceManagerTests.cs",
        ],
        ["T17"] =
        [
            "Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs",
            "Game.Core.Tests/Services/ResourceManagerEventTests.cs",
        ],
    };

    // ACC:T44.7
    [Fact]
    public void ShouldMapRequirementIdsToExecutableEvidence_WhenTask44GovernanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateRequirementIds(entry).Should().BeTrue($"{viewPath} must keep ACC:T44.7 mapping complete.");
        }
    }

    // ACC:T44.8
    [Fact]
    public void ShouldKeepTask44ScopeMappingAndEvidenceInsideDeclaredUiSlice_WhenGovernanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateScopeMapping(entry).Should().BeTrue($"{viewPath} must keep ACC:T44.8 scope-to-evidence mapping complete.");
        }
    }

    // ACC:T44.9
    [Fact]
    public void ShouldRequireTask44ValidationRefsToStayXunitOnlyAndDeclareGdunitNa_WhenAcceptanceIsValidated()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var validationAcceptance = FindAcceptance(
                entry.Acceptance,
                "xUnit suite",
                "GdUnit N/A record");
            validationAcceptance.Should().NotBeNull();

            var validationRefs = ParseRefs(validationAcceptance!).ToArray();
            validationRefs.Should().Contain(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
            validationRefs.Should().NotContain(path => path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase));
            validationAcceptance.Should().Contain("GdUnit", because: "Task 44 uses xUnit refs and requires explicit GdUnit N/A semantics.");
            validationAcceptance.Should().Contain("N/A", because: "Task 44 acceptance explicitly requires N/A rationale for non-applicable framework.");
            validationAcceptance.Should().NotContain(
                "artifact evidence must record auditable pass/fail outcomes",
                because: "Task 44 governance only enforces deterministic xUnit+GdUnit N/A evidence semantics at task-view level.");
        }
    }

    [Fact]
    public void ShouldFailRequirementMapping_WhenAnyRequirementIdIsMissing()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var requirementAcceptance = FindAcceptance(entry.Acceptance, "Requirement ID", "RQ-COMBAT-QUEUE-TECH", "RQ-ECONOMY-BUILD-RULES");
            requirementAcceptance.Should().NotBeNull();

            var mutatedAcceptance = requirementAcceptance!
                .Replace("RQ-COMBAT-QUEUE-TECH", string.Empty, StringComparison.Ordinal)
                .Replace("RQ-ECONOMY-BUILD-RULES", string.Empty, StringComparison.Ordinal);

            var mutated = entry.ReplaceAcceptance(requirementAcceptance!, mutatedAcceptance);
            ValidateRequirementIds(mutated).Should().BeFalse($"{viewPath} should fail ACC:T44.7 when IDs are missing.");
        }
    }

    [Fact]
    public void ShouldFailScopeMapping_WhenScopeEvidenceIsMissing()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var missingScopeEvidence = entry.RemoveTestRef("Game.Core.Tests/Services/ResourceManagerIntegerSafetyTests.cs");
            ValidateScopeMapping(missingScopeEvidence).Should().BeFalse($"{viewPath} should fail ACC:T44.8 when required scope evidence is removed.");
        }
    }

    [Fact]
    public void ShouldFailScopeMapping_WhenOutOfScopeTokenIsInjected()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var scopeAcceptance = FindAcceptance(entry.Acceptance, "T12, T13, T14, T15, T16, T17", "scope mapping");
            scopeAcceptance.Should().NotBeNull();

            var mutatedScopeAcceptance = $"{scopeAcceptance} Additional mapping: T18.";
            var mutated = entry.ReplaceAcceptance(scopeAcceptance!, mutatedScopeAcceptance);
            ValidateScopeMapping(mutated).Should().BeFalse($"{viewPath} should reject out-of-scope token T18 for ACC:T44.8.");
        }
    }

    private static IEnumerable<string> ViewPaths()
    {
        yield return ".taskmaster/tasks/tasks_back.json";
        yield return ".taskmaster/tasks/tasks_gameplay.json";
    }

    private static Task44TaskEntry LoadTaskEntry(string relativePath, int taskId)
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
                return new Task44TaskEntry(scopeIds, acceptance, testRefs);
            }
        }

        throw new InvalidOperationException($"Task {taskId} was not found in '{relativePath}'.");
    }

    private static bool ValidateRequirementIds(Task44TaskEntry entry)
    {
        var requirementAcceptance = FindAcceptance(entry.Acceptance, "Requirement ID", "RQ-COMBAT-QUEUE-TECH", "RQ-ECONOMY-BUILD-RULES");
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

    private static bool ValidateScopeMapping(Task44TaskEntry entry)
    {
        if (!entry.ScopeTaskIds.SequenceEqual([12, 13, 14, 15, 16, 17]))
        {
            return false;
        }

        var scopeAcceptance = FindAcceptance(entry.Acceptance, "T12, T13, T14, T15, T16, T17", "scope mapping");
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

    private sealed record Task44TaskEntry(int[] ScopeTaskIds, string[] Acceptance, string[] TestRefs)
    {
        public Task44TaskEntry ReplaceAcceptance(string current, string replacement)
        {
            var updated = Acceptance
                .Select(line => string.Equals(line, current, StringComparison.Ordinal) ? replacement : line)
                .ToArray();
            return this with { Acceptance = updated };
        }

        public Task44TaskEntry RemoveTestRef(string testRef)
        {
            var updated = TestRefs
                .Where(path => !string.Equals(path, Normalize(testRef), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return this with { TestRefs = updated };
        }
    }
}
