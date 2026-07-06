using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Services;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task61RefsAnchorsSyncTests
{
    // ACC:T61.8
    [Fact]
    public void ShouldResolveTaskScopedEvidenceFiles_WhenTask61RefsAndAnchorsAreValidated()
    {
        var root = FindRepositoryRoot();
        var master = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks.json")));
        var back = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json")));
        var gameplay = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json")));
        var t61Test = File.ReadAllText(Path.Combine(root, "Tests.Godot", "tests", "Scenes", "BattleMap", "test_task61_building_selection_feedback.gd"));
        var thisFile = File.ReadAllText(Path.Combine(root, "Game.Core.Tests", "Tasks", "Task61RefsAnchorsSyncTests.cs"));

        var masterTask = FindMasterTask(master, 61);
        var backTask = FindTask61(back);
        var gameplayTask = FindTask61(gameplay);
        masterTask.Should().NotBeNull();
        backTask.Should().NotBeNull();
        gameplayTask.Should().NotBeNull();

        var masterTaskValue = masterTask!.Value;
        var backTaskValue = backTask!.Value;
        var gameplayTaskValue = gameplayTask!.Value;
        var masterRefs = masterTaskValue.GetProperty("testRefs");
        var backAcceptance = backTaskValue.GetProperty("acceptance");
        var gameplayAcceptance = gameplayTaskValue.GetProperty("acceptance");
        var backRefs = backTaskValue.GetProperty("test_refs");
        var gameplayRefs = gameplayTaskValue.GetProperty("test_refs");

        backAcceptance[0].GetString().Should().NotContain("test_battle_map_screen_runtime_flow.gd");
        gameplayAcceptance[0].GetString().Should().NotContain("test_battle_map_screen_runtime_flow.gd");
        masterRefs.ToString().Should().Contain("Game.Core.Tests/Tasks/Task61RefsAnchorsSyncTests.cs");
        backRefs.ToString().Should().Contain("Game.Core.Tests/Tasks/Task61RefsAnchorsSyncTests.cs");
        gameplayRefs.ToString().Should().Contain("Game.Core.Tests/Tasks/Task61RefsAnchorsSyncTests.cs");
        masterRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller.gd");
        masterRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_resource_cards.gd");
        masterRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_runtime_actions.gd");
        masterRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_click_actions.gd");
        masterRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_invalid_click_actions.gd");
        backRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller.gd");
        backRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_resource_cards.gd");
        backRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_runtime_actions.gd");
        backRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_click_actions.gd");
        backRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_invalid_click_actions.gd");
        gameplayRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller.gd");
        gameplayRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_resource_cards.gd");
        gameplayRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_runtime_actions.gd");
        gameplayRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_click_actions.gd");
        gameplayRefs.ToString().Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_build_placement_controller_invalid_click_actions.gd");
        backAcceptance[0].GetString().Should().Contain("test_task61_building_selection_feedback.gd");
        gameplayAcceptance[0].GetString().Should().Contain("test_task61_building_selection_feedback.gd");

        t61Test.Should().Contain("ACC:T61.1");
        t61Test.Should().Contain("ACC:T61.11");
        thisFile.Should().Contain("ACC:T61.8");
    }

    private static string FindRepositoryRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(dir))
        {
            if (Directory.Exists(Path.Combine(dir, ".taskmaster")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing .taskmaster directory.");
    }

    private static JsonElement? FindTask61(JsonDocument doc)
    {
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("taskmaster_id", out var id))
            {
                continue;
            }

            if (id.GetInt32() == 61)
            {
                return item;
            }
        }

        return null;
    }

    private static JsonElement? FindMasterTask(JsonDocument doc, int taskId)
    {
        if (!doc.RootElement.TryGetProperty("master", out var master) ||
            !master.TryGetProperty("tasks", out var tasks))
        {
            return null;
        }

        foreach (var item in tasks.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var id))
            {
                continue;
            }

            if (id.GetInt32() == taskId)
            {
                return item;
            }
        }

        return null;
    }

    private static string FindAcceptanceLine(JsonElement acceptanceArray, string accTag)
    {
        foreach (var line in acceptanceArray.EnumerateArray())
        {
            var text = line.GetString() ?? string.Empty;
            if (text.Contains(accTag, StringComparison.Ordinal))
            {
                return text;
            }
        }

        throw new InvalidDataException($"Could not find acceptance line tagged with {accTag}.");
    }


    // ACC:T62.10
    [Fact]
    public void ShouldResolveTask62AcceptanceRefs_WhenValidatingTask62AnchorBinding()
    {
        var root = FindRepositoryRoot();
        var back = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json")));
        var gameplay = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json")));

        var backTask = FindTask62(back);
        var gameplayTask = FindTask62(gameplay);
        backTask.Should().NotBeNull();
        gameplayTask.Should().NotBeNull();

        var backTaskValue = backTask!.Value;
        var gameplayTaskValue = gameplayTask!.Value;
        var backAcceptance = backTaskValue.GetProperty("acceptance");
        var gameplayAcceptance = gameplayTaskValue.GetProperty("acceptance");

        FindAcceptanceLine(backAcceptance, "spawn-side glow").Should().Contain("test_battle_map_screen_runtime_flow.gd");
        FindAcceptanceLine(backAcceptance, "wave pulse").Should().Contain("test_battle_map_screen_runtime_flow.gd");
        FindAcceptanceLine(gameplayAcceptance, "behavior-led path readability").Should().Contain("test_combat_experience_runtime_flow.gd");
        FindAcceptanceLine(gameplayAcceptance, "route-line UI").Should().Contain("test_combat_experience_runtime_flow.gd");
    }



    // ACC:T62.12
    [Fact]
    public void ShouldBindTask62SemanticAnchorsToTaskScopedIntegrationEvidence()
    {
        var root = FindRepositoryRoot();
        var back = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json")));
        var gameplay = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json")));

        var backTask = FindTask62(back);
        var gameplayTask = FindTask62(gameplay);
        backTask.Should().NotBeNull();
        gameplayTask.Should().NotBeNull();

        var backAcceptance = backTask!.Value.GetProperty("acceptance");
        var gameplayAcceptance = gameplayTask!.Value.GetProperty("acceptance");

        FindAcceptanceLine(backAcceptance, "spawn-side glow").Should().Contain("test_battle_map_screen_runtime_flow.gd");
        FindAcceptanceLine(backAcceptance, "wave pulse").Should().Contain("test_battle_map_screen_runtime_flow.gd");
        FindAcceptanceLine(gameplayAcceptance, "retargeting behavior").Should().Contain("test_combat_experience_runtime_flow.gd");
        FindAcceptanceLine(gameplayAcceptance, "behavior-led path readability").Should().Contain("test_combat_experience_runtime_flow.gd");
    }


    // ACC:T62.12
    [Fact]
    public void ShouldKeepRetargetingDeterministic_WhenTask62SemanticAnchorsNeedCoreEvidence()
    {
        var service = new EnemyAiRetargetingService();
        var candidates = new[]
        {
            new EnemyAiRetargetCandidate("castle", 3, true),
            new EnemyAiRetargetCandidate("barracks", 2, true),
            new EnemyAiRetargetCandidate("blocked_gate", 5, false),
        };

        var keepCurrent = service.SelectNextReachableTarget(candidates, "barracks");
        keepCurrent.Should().NotBeNull();
        keepCurrent!.TargetId.Should().Be("barracks");

        var switchWhenCurrentUnreachable = service.SelectNextReachableTarget(candidates, "blocked_gate");
        switchWhenCurrentUnreachable.Should().NotBeNull();
        switchWhenCurrentUnreachable!.TargetId.Should().Be("castle");
    }

    private static JsonElement? FindTask62(JsonDocument doc)
    {
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("taskmaster_id", out var id))
            {
                continue;
            }

            if (id.GetInt32() == 62)
            {
                return item;
            }
        }

        return null;
    }

}
