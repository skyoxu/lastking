using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task43UiWiringAcceptanceGovernanceTests
{
    private const int TaskId = 43;
    private const string MasterTaskPath = ".taskmaster/tasks/tasks.json";
    private static readonly JsonDocumentOptions JsonOptions = new() { MaxDepth = 128 };
    private static readonly Regex RefsRegex = new(@"\bRefs\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Fact]
    public void ShouldKeepSevenAcceptanceItems_ForTask43InBackAndGameplayViews()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            entry.Acceptance.Should().HaveCount(7, $"{viewPath} should keep only semantic acceptance obligations for Task 43.");

            entry.Acceptance[0].Should().Contain("Combat HUD, Pressure, and Camera Feedback");
            entry.Acceptance[0].Should().Contain("docs-only evidence is insufficient");

            entry.Acceptance[1].Should().Contain("CombatHud, PressurePanel, and CameraControlOverlay");
            entry.Acceptance[2].Should().Contain("no hidden state dependency");
            entry.Acceptance[3].Should().Contain("deterministic wave replay");
            entry.Acceptance[4].Should().Contain("both conditions are mandatory");
            entry.Acceptance[5].Should().Contain("changing one channel budget");
            entry.Acceptance[6].Should().Contain("during Fight actions");

            entry.ScopeTaskIds.Should().Equal([4, 5, 6, 20, 22]);
        }
    }

    [Fact]
    public void ShouldKeepRuntimeSurfaceEvidenceRefs_ForTask43SemanticItems()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);

            var refs1 = ParseRefs(entry.Acceptance[0]);
            var refs2 = ParseRefs(entry.Acceptance[1]);
            var refs3 = ParseRefs(entry.Acceptance[2]);
            var refs5 = ParseRefs(entry.Acceptance[4]);

            refs1.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            refs1.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");

            refs2.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            refs2.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");

            refs3.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            refs3.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");

            refs5.Should().Contain("Tests.Godot/tests/Scenes/Combat/test_enemy_ai_navigation_priority.gd");
            refs5.Should().Contain("Tests.Godot/tests/Scenes/Combat/test_enemy_ai_blocked_route_fallback.gd");

            var refs7 = ParseRefs(entry.Acceptance[6]);
            refs7.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");

            entry.TestRefs.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            entry.TestRefs.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");
        }
    }

    [Fact]
    public void ShouldKeepDeterminismAndBudgetEvidenceRefs_ForTask43SemanticItems()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);

            var refs4 = ParseRefs(entry.Acceptance[3]);
            var refs6 = ParseRefs(entry.Acceptance[5]);

            refs4.Should().Contain("Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs");
            refs4.Should().Contain("Game.Core.Tests/Engine/GameEngineCoreDeterminismTests.cs");
            refs4.Should().Contain("Game.Core.Tests/Services/WaveManagerDeterminismTests.cs");

            refs6.Should().Contain("Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs");
            refs6.Should().Contain("Game.Core.Tests/Services/WaveBudgetAllocatorTests.cs");
            refs6.Should().Contain("Game.Core.Tests/Engine/GameEngineCoreDeterminismTests.cs");
            refs6.Should().Contain("Game.Core.Tests/Services/WaveManagerDeterminismTests.cs");

            var refs7 = ParseRefs(entry.Acceptance[6]);
            refs7.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            refs7.Should().Contain("Tests.Godot/tests/Scenes/Combat/test_enemy_ai_blocked_route_fallback.gd");
        }
    }

    [Fact]
    public void ShouldKeepRequirementAndScopeGovernance_ForTask43()
    {
        var master = LoadMasterTask43();
        master.RequirementIds.Should().BeEquivalentTo(
            ["RQ-CAMERA-SCROLL", "RQ-COMBAT-QUEUE-TECH", "RQ-CORE-LOOP-STATE"],
            options => options.WithStrictOrdering());

        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            entry.ScopeTaskIds.Should().Equal([4, 5, 6, 20, 22]);
        }
    }

    [Fact]
    public void ShouldKeepCrossViewClosureStateConsistent_ForTask43Done()
    {
        var master = LoadMasterTask43();
        master.Status.Should().Be("done");
        master.Details.Should().Contain("Current closure evidence status: runtime");
        master.Details.Should().Contain("Current Chapter7 recommendation: done-ready");
        master.Details.Should().Contain("Pending owned surfaces: None");
        master.Details.Should().Contain("Primary closure gaps: None");

        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            entry.Status.Should().Be("done");
            entry.Acceptance.Should().HaveCount(7);
        }
    }

    [Fact]
    public void ShouldKeepDualFrameworkEvidenceRefs_ForTask43()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);

            entry.TestRefs.Should().Contain("Game.Core.Tests/Services/WaveManagerBudgetChannelTests.cs");
            entry.TestRefs.Should().Contain("Game.Core.Tests/Services/WaveManagerDeterminismTests.cs");
            entry.TestRefs.Should().Contain("Tests.Godot/tests/UI/test_hud_scene.gd");
            entry.TestRefs.Should().Contain("Tests.Godot/tests/UI/test_hud_updates_on_events.gd");
            entry.TestRefs.Should().Contain("Tests.Godot/tests/Scenes/Camera/test_camera_controller_scroll_inputs.gd");
            entry.TestRefs.Should().Contain("Tests.Godot/tests/Scenes/Combat/test_enemy_ai_navigation_priority.gd");
            entry.TestRefs.Should().Contain("Tests.Godot/tests/Scenes/Combat/test_enemy_ai_blocked_route_fallback.gd");
            entry.TestRefs.Should().Contain("Game.Core.Tests/Tasks/Task43UiWiringAcceptanceGovernanceTests.cs");
        }
    }

    [Fact]
    public void ShouldProveDeterministicReplayBehavior_ForTask43Semantics()
    {
        var sut = new WaveManager();
        var config = CreateDefaultConfig();

        var firstRun = sut.Generate(dayIndex: 3, channelBudgetConfiguration: config, seed: 4242);
        var replayRun = sut.Generate(dayIndex: 3, channelBudgetConfiguration: config, seed: 4242);
        var driftedSeedRun = sut.Generate(dayIndex: 3, channelBudgetConfiguration: config, seed: 4243);

        BuildWaveSnapshot(replayRun).Should().Be(BuildWaveSnapshot(firstRun));
        BuildWaveSnapshot(driftedSeedRun).Should().NotBe(BuildWaveSnapshot(firstRun));
    }

    [Fact]
    public void ShouldProveSingleChannelBudgetIsolation_ForTask43Semantics()
    {
        var sut = new WaveManager();
        var baselineConfig = CreateDefaultConfig();
        var tunedConfig = baselineConfig with
        {
            Elite = baselineConfig.Elite with { Day1Budget = baselineConfig.Elite.Day1Budget + 40 }
        };

        var baselineResult = sut.Generate(dayIndex: 2, channelBudgetConfiguration: baselineConfig, seed: 3333);
        var tunedResult = sut.Generate(dayIndex: 2, channelBudgetConfiguration: tunedConfig, seed: 3333);

        BuildChannelSnapshot(tunedResult.ChannelResults["elite"]).Should().NotBe(BuildChannelSnapshot(baselineResult.ChannelResults["elite"]));
        BuildChannelSnapshot(tunedResult.ChannelResults["normal"]).Should().Be(BuildChannelSnapshot(baselineResult.ChannelResults["normal"]));
        BuildChannelSnapshot(tunedResult.ChannelResults["boss"]).Should().Be(BuildChannelSnapshot(baselineResult.ChannelResults["boss"]));
    }

    [Fact]
    public void ShouldProveBlockedPathFallbackSelection_ForTask43Semantics()
    {
        var selector = new EnemyAiTargetSelector();
        var candidates = new[]
        {
            EnemyAiTargetCandidate.Unreachable("unit-1", EnemyTargetClass.Unit, 1),
            EnemyAiTargetCandidate.Blocker("gate-1", 2),
            EnemyAiTargetCandidate.Reachable("decor-1", EnemyTargetClass.Decoration, 1)
        };

        var decision = selector.SelectTarget(candidates);

        decision.IsFallbackAttack.Should().BeTrue();
        decision.TargetClass.Should().Be(EnemyTargetClass.BlockingStructure);
        decision.TargetId.Should().Be("gate-1");
        decision.TargetId.Should().NotBe("decor-1");
    }

    [Fact]
    public void ShouldFailSemanticSetValidation_WhenOwnedSurfaceObligationIsRemoved()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            var mutated = entry with
            {
                Acceptance = entry.Acceptance
                    .Select((line, index) => index == 1 ? line.Replace("CombatHud, PressurePanel, and CameraControlOverlay", "owned surfaces") : line)
                    .ToArray(),
            };

            ValidateSemanticSet(mutated).Should().BeFalse($"{viewPath} should fail when owned surface obligation is weakened.");
        }
    }

    [Fact]
    public void ShouldValidateSemanticSet_ForTask43Views()
    {
        foreach (var viewPath in ViewPaths())
        {
            var entry = LoadTaskEntry(viewPath, TaskId);
            ValidateSemanticSet(entry).Should().BeTrue($"{viewPath} should keep Task 43 semantic acceptance obligations complete.");
        }
    }

    private static bool ValidateSemanticSet(Task43TaskEntry entry)
    {
        if (entry.Acceptance.Length != 7)
        {
            return false;
        }

        bool HasToken(int idx, string token) => entry.Acceptance[idx].IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        if (!HasToken(0, "Combat HUD, Pressure, and Camera Feedback") || !HasToken(0, "docs-only evidence is insufficient"))
        {
            return false;
        }

        if (!HasToken(1, "CombatHud") || !HasToken(1, "PressurePanel") || !HasToken(1, "CameraControlOverlay"))
        {
            return false;
        }

        if (!HasToken(2, "no hidden state dependency") ||
            !HasToken(3, "deterministic wave replay") ||
            !HasToken(4, "both conditions are mandatory") ||
            !HasToken(5, "changing one channel budget") ||
            !HasToken(6, "during Fight actions"))
        {
            return false;
        }

        if (!entry.ScopeTaskIds.SequenceEqual([4, 5, 6, 20, 22]))
        {
            return false;
        }

        return true;
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
                var status = item.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? string.Empty : string.Empty;
                return new Task43TaskEntry(scopeIds, acceptance, testRefs, status);
            }
        }

        throw new InvalidOperationException($"Task {taskId} was not found in '{relativePath}'.");
    }

    private static MasterTask43Entry LoadMasterTask43()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ResolveRepositoryPath(MasterTaskPath)), JsonOptions);
        var tasks = doc.RootElement.GetProperty("master").GetProperty("tasks");
        foreach (var item in tasks.EnumerateArray())
        {
            if (item.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number && idProp.GetInt32() == TaskId)
            {
                var details = item.TryGetProperty("details", out var detailsProp) ? detailsProp.GetString() ?? string.Empty : string.Empty;
                var status = item.TryGetProperty("status", out var statusProp) ? statusProp.GetString() ?? string.Empty : string.Empty;
                var requirementIds = ExtractRequirementIds(details);
                return new MasterTask43Entry(status, details, requirementIds);
            }
        }

        throw new InvalidOperationException($"Task {TaskId} was not found in '{MasterTaskPath}'.");
    }

    private static string[] ExtractRequirementIds(string details)
    {
        var line = details.Split('\n')
            .Select(static l => l.Trim())
            .FirstOrDefault(static l => l.StartsWith("Requirement IDs:", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(line))
        {
            return Array.Empty<string>();
        }

        var payload = line.Substring("Requirement IDs:".Length);
        return payload
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim())
            .Where(token => token.Length > 0)
            .ToArray();
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

    private static ChannelBudgetConfiguration CreateDefaultConfig()
    {
        return new ChannelBudgetConfiguration(
            Normal: new ChannelRule(Day1Budget: 50, DailyGrowth: 1.2m, ChannelLimit: 20, CostPerEnemy: 10),
            Elite: new ChannelRule(Day1Budget: 120, DailyGrowth: 1.2m, ChannelLimit: 8, CostPerEnemy: 20),
            Boss: new ChannelRule(Day1Budget: 300, DailyGrowth: 1.2m, ChannelLimit: 3, CostPerEnemy: 100));
    }

    private static string BuildWaveSnapshot(WaveResult waveResult)
    {
        var channelSnapshots = waveResult.ChannelResults
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{BuildChannelSnapshot(pair.Value)}");

        return $"{waveResult.DayIndex}|{waveResult.Seed}|{string.Join("|", channelSnapshots)}";
    }

    private static string BuildChannelSnapshot(ChannelWaveResult channelWaveResult)
    {
        var audit = channelWaveResult.Audit;
        return $"{audit.InputBudget},{audit.Allocated},{audit.Spent},{audit.Remaining}|{string.Join(",", channelWaveResult.SpawnOrder)}";
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

    private sealed record Task43TaskEntry(int[] ScopeTaskIds, string[] Acceptance, string[] TestRefs, string Status);

    private sealed record MasterTask43Entry(string Status, string Details, string[] RequirementIds);
}
