using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task63RefsAnchorsSyncTests
{
    // ACC:T63.10
    [Fact]
    public void ShouldKeepTask63AcceptanceRefsBoundToTaskScopedEvidence_WhenValidatingAnchorSync()
    {
        var root = FindRepositoryRoot();
        var back = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json")));
        var gameplay = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json")));
        var combatFlowTest = File.ReadAllText(
            Path.Combine(root, "Tests.Godot", "tests", "Integration", "test_combat_experience_runtime_flow.gd"));
        var battleMapTest = File.ReadAllText(
            Path.Combine(root, "Tests.Godot", "tests", "Integration", "test_battle_map_screen_runtime_flow.gd"));

        var backTask = FindTask63(back);
        var gameplayTask = FindTask63(gameplay);
        backTask.Should().NotBeNull();
        gameplayTask.Should().NotBeNull();

        var backRefs = ExtractStringArray(backTask!.Value.GetProperty("test_refs"));
        var gameplayRefs = ExtractStringArray(gameplayTask!.Value.GetProperty("test_refs"));

        backRefs.Should().Contain("Game.Core.Tests/Tasks/Task63RefsAnchorsSyncTests.cs");
        gameplayRefs.Should().Contain("Game.Core.Tests/Tasks/Task63RefsAnchorsSyncTests.cs");
        backRefs.Should().Contain("Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd");
        gameplayRefs.Should().Contain("Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd");

        var requiredAnchors = Enumerable.Range(1, 9).Select(i => $"ACC:T63.{i}").ToArray();
        var combinedTestSource = combatFlowTest + "\n" + battleMapTest;
        foreach (var anchor in requiredAnchors)
        {
            combinedTestSource.Should().Contain(anchor);
        }
        File.ReadAllText(Path.Combine(root, "Game.Core.Tests", "Tasks", "Task63RefsAnchorsSyncTests.cs"))
            .Should().Contain("ACC:T63.10");

        combatFlowTest.Should().Contain("func test_damage_number_toggle_should_hide_then_restore_damage_number_rendering()");
        combatFlowTest.Should().Contain("func test_spawn_cues_and_path_readability_survive_full_battle_loop()");
        combatFlowTest.Should().Contain("ownership_container");
        combatFlowTest.Should().Contain("assert_bool(bridge.has_method(\"SpawnEnemyWavePhase\")).is_true()");
        combatFlowTest.Should().Contain("assert_str(status_label.text.to_lower()).contains(\"wave\")");
        combatFlowTest.Should().Contain("assert_str(pressure_label.text.to_lower()).contains(\"n/a\")");
        combatFlowTest.Should().Contain("assert_bool(pressure_label.text.to_lower().find(\"critical\") < 0).is_true()");
        combatFlowTest.Should().Contain("assert_bool(pressure_label.text.to_lower().find(\"high\") < 0).is_true()");
        combatFlowTest.Should().Contain("assert_bool(pressure_panel.visible).is_true()");
        combatFlowTest.Should().Contain("assert_str(prompt_label.text.to_lower()).contains(\"n/a\")");
        combatFlowTest.Should().Contain("assert_str(outcome_label.text.to_lower()).contains(\"n/a\")");
        combatFlowTest.Should().Contain("assert_bool(feedback_label.visible).is_false()");
        combatFlowTest.Should().Contain("assert_str(feedback_label.text).is_equal(\"\")");
        combatFlowTest.Should().Contain("hit_flash_visible");
        combatFlowTest.Should().Contain("wall_pressure_emphasis_active");
        combatFlowTest.Should().Contain("bridge.call(\"CleanupDeadUnitsPhase\")");
        combatFlowTest.Should().Contain("assert_int(_battlefield_children_with_prefix(bridge, \"DamageNumber\").size()).is_equal(0)");
        combatFlowTest.Should().Contain("bridge.call(\"PublishOutcomePhase\")");
        combatFlowTest.Should().Contain("assert_bool(feedback_label.visible).is_true()");
        combatFlowTest.Should().Contain("assert_bool(prompt_label.text.to_lower().find(\"n/a\") < 0).is_true()");
        combatFlowTest.Should().Contain("_write_damage_numbers_setting(false)");
        combatFlowTest.Should().Contain("_write_damage_numbers_setting(true)");
        combatFlowTest.Should().NotContain("SetDamageNumbersEnabledForTest");

        var anchorAssertionPatterns = new Dictionary<string, string[]>
        {
            ["ACC:T63.1"] = new[]
            {
                "pressure_label.text.to_lower()",
                "prompt_label.text.to_lower()",
                "feedback_label.visible",
                "pressure_label.text.to_lower().find(\"high\") >= 0",
                "feedback_label.text.length() > 0",
                "hit_flash_visible",
                "wall_pressure_emphasis_active",
            },
            ["ACC:T63.2"] = new[]
            {
                "_write_damage_numbers_setting(false)",
                "_write_damage_numbers_setting(true)",
                "ResolveCombatExchangePhase",
                "PublishOutcomePhase",
                "feedback_label.visible",
                "prompt_label.text.to_lower().find(\"n/a\") < 0",
                "CleanupDeadUnitsPhase",
            },
            ["ACC:T63.4"] = new[] { "ownership_container" },
            ["ACC:T63.5"] = new[] { "bridge.has_method(\"SpawnEnemyWavePhase\")" },
            ["ACC:T63.6"] = new[] { "status_label.text.to_lower()).contains(\"wave\")" },
            ["ACC:T63.3"] = new[] { "status_after_wave", "summary_after_wave", "enemy_units_spawned" },
            ["ACC:T63.7"] = new[] { "status_after_decay", "is_equal(status_after_wave)" },
            ["ACC:T63.8"] = new[] { "test_player_visible_combat_experience_runs_from_building_and_training_to_death_cleanup_and_summary" },
            ["ACC:T63.9"] = new[] { "_bridge_summary_metrics(bridge)).is_equal(summary_after_wave)" },
            ["ACC:T63.10"] = new[] { "anchorAssertionPatterns", "requiredAnchors" },
        };
        foreach (var pair in anchorAssertionPatterns)
        {
            foreach (var pattern in pair.Value)
            {
                var source = pair.Key == "ACC:T63.10"
                    ? File.ReadAllText(Path.Combine(root, "Game.Core.Tests", "Tasks", "Task63RefsAnchorsSyncTests.cs"))
                    : combinedTestSource;
                source.Should().Contain(pattern, $"because {pair.Key} must map to assertion patterns, not only anchor strings");
            }
        }

        var mainFlowBody = ExtractFunctionBody(combatFlowTest, "test_player_visible_combat_experience_runs_from_building_and_training_to_death_cleanup_and_summary");
        foreach (var pattern in anchorAssertionPatterns["ACC:T63.1"])
        {
            mainFlowBody.Should().Contain(pattern, "because ACC:T63.1 patterns must live in the scoped behavior test body");
        }
    }

    private static string ExtractFunctionBody(string source, string functionName)
    {
        var signature = $"func {functionName}() -> void:";
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterOrEqualTo(0, $"function `{functionName}` must exist");
        var nextFunc = source.IndexOf("\nfunc ", start + signature.Length, StringComparison.Ordinal);
        if (nextFunc < 0)
        {
            nextFunc = source.Length;
        }

        return source.Substring(start, nextFunc - start);
    }

    private static IReadOnlyList<string> ExtractStringArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
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

    private static JsonElement? FindTask63(JsonDocument doc)
    {
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("taskmaster_id", out var id))
            {
                continue;
            }

            if (id.GetInt32() == 63)
            {
                return item;
            }
        }

        return null;
    }
}
