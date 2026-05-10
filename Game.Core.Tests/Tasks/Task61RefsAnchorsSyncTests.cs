using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task61RefsAnchorsSyncTests
{
    // ACC:T61.8
    [Fact]
    public void ShouldResolveTaskScopedEvidenceFiles_WhenTask61RefsAndAnchorsAreValidated()
    {
        var root = FindRepositoryRoot();
        var back = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json")));
        var gameplay = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json")));
        var t61Test = File.ReadAllText(Path.Combine(root, "Tests.Godot", "tests", "Scenes", "BattleMap", "test_task61_building_selection_feedback.gd"));
        var thisFile = File.ReadAllText(Path.Combine(root, "Game.Core.Tests", "Tasks", "Task61RefsAnchorsSyncTests.cs"));

        var backTask = FindTask61(back);
        var gameplayTask = FindTask61(gameplay);
        backTask.Should().NotBeNull();
        gameplayTask.Should().NotBeNull();

        var backTaskValue = backTask!.Value;
        var gameplayTaskValue = gameplayTask!.Value;
        var backAcceptance = backTaskValue.GetProperty("acceptance");
        var gameplayAcceptance = gameplayTaskValue.GetProperty("acceptance");
        var backRefs = backTaskValue.GetProperty("test_refs");
        var gameplayRefs = gameplayTaskValue.GetProperty("test_refs");

        backAcceptance[0].GetString().Should().NotContain("test_battle_map_screen_runtime_flow.gd");
        gameplayAcceptance[0].GetString().Should().NotContain("test_battle_map_screen_runtime_flow.gd");
        backRefs.ToString().Should().Contain("Game.Core.Tests/Tasks/Task61RefsAnchorsSyncTests.cs");
        gameplayRefs.ToString().Should().Contain("Game.Core.Tests/Tasks/Task61RefsAnchorsSyncTests.cs");
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
}
