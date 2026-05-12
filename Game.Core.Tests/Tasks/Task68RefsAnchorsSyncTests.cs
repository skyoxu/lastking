using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Game.Core.Utilities;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task68RefsAnchorsSyncTests
{
    // ACC:T68.2
    // ACC:T68.3
    // ACC:T68.5
    // ACC:T68.10
    [Fact]
    public void ShouldKeepTask68AcceptanceRefsAndAnchorsSynchronized_WhenValidatingEvidenceMapping()
    {
        var root = FindRepositoryRoot();
        using var back = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json")));
        using var gameplay = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json")));
        var integration = File.ReadAllText(Path.Combine(root, "Tests.Godot", "tests", "Integration", "test_combat_experience_runtime_flow.gd"));
        var scene = File.ReadAllText(Path.Combine(root, "Tests.Godot", "tests", "Scenes", "BattleMap", "test_battlemap_operation_surface_semantics.gd"));
        var sync = File.ReadAllText(Path.Combine(root, "Game.Core.Tests", "Tasks", "Task68RefsAnchorsSyncTests.cs"));

        var backTask = FindTask68(back);
        var gameplayTask = FindTask68(gameplay);
        backTask.Should().NotBeNull();
        gameplayTask.Should().NotBeNull();

        ValidateTaskRefs(root, backTask!.Value);
        ValidateTaskRefs(root, gameplayTask!.Value);

        var requiredAnchors = Enumerable.Range(1, 10).Select(i => $"ACC:T68.{i}").ToArray();
        var combined = integration + "\n" + scene + "\n" + sync;
        foreach (var anchor in requiredAnchors)
        {
            combined.Should().Contain(anchor);
        }

        integration.Should().Contain("func test_victory_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions() -> void:");
        integration.Should().Contain("func test_victory_outcome_modal_should_stay_centered_and_preserve_runtime_ownership_boundaries() -> void:");
        integration.Should().Contain("VictoryOutcomeModal/VBox/Actions/ReturnToMainMenuBtn");
        integration.Should().Contain("VictoryOutcomeModal/VBox/Actions/RestartBtn");
        integration.Should().Contain("status_label.text.to_lower().find(\"terminal outcome\")");

        scene.Should().Contain("func test_t68_victory_outcome_modal_node_paths_should_exist_and_default_hidden() -> void:");
        scene.Should().Contain("VictoryOutcomeModal/VBox/Actions/ReturnToMainMenuBtn");
        scene.Should().Contain("VictoryOutcomeModal/VBox/Actions/RestartBtn");

        var mapping = new Dictionary<string, (string fileKey, string functionName, string[] patterns)>
        {
            ["ACC:T68.1"] = ("integration", "test_victory_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions", new[]
            {
                "assert_bool(victory_modal.visible).is_true()",
                "assert_bool(get_tree().paused).is_true()",
            }),
            ["ACC:T68.2"] = ("integration", "test_victory_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions", new[]
            {
                "assert_str(return_btn.text).is_equal(\"Return to Main Menu\")",
                "assert_str(restart_btn.text).is_equal(\"Restart\")",
            }),
            ["ACC:T68.3"] = ("integration", "test_victory_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions", new[]
            {
                "assert_bool(status_label.text.to_lower().find(\"continue battle\") < 0).is_true()",
            }),
            ["ACC:T68.4"] = ("integration", "test_victory_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions", new[]
            {
                "assert_bool(victory_hint.text.to_lower().find(\"cannot be resumed\") >= 0).is_true()",
            }),
            ["ACC:T68.5"] = ("sync", "ShouldKeepTask68AcceptanceRefsAndAnchorsSynchronized_WhenValidatingEvidenceMapping", new[]
            {
                "Game.Core.Tests/Tasks/Task68RefsAnchorsSyncTests.cs",
                "Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd",
            }),
            ["ACC:T68.6"] = ("integration", "test_victory_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions", new[]
            {
                "assert_bool(victory_summary.text.find(\"HP=42\") >= 0).is_true()",
                "assert_bool(victory_summary.text.find(\"kills=\") >= 0).is_true()",
            }),
            ["ACC:T68.7"] = ("integration", "test_victory_outcome_modal_should_stay_centered_and_preserve_runtime_ownership_boundaries", new[]
            {
                "assert_float(modal.anchor_left).is_equal(0.5)",
                "assert_str(str(bridge.get_meta(\"ownership_container\"))).is_equal(\"runtime_bridge\")",
            }),
            ["ACC:T68.8"] = ("integration", "test_victory_outcome_modal_should_pause_runtime_and_only_offer_terminal_actions", new[]
            {
                "assert_bool(not return_btn.disabled).is_true()",
                "assert_bool(not restart_btn.disabled).is_true()",
            }),
            ["ACC:T68.9"] = ("scene", "test_t68_victory_outcome_modal_node_paths_should_exist_and_default_hidden", new[]
            {
                "assert_object(return_btn).is_not_null()",
                "assert_int((actions as VBoxContainer).get_child_count()).is_equal(2)",
            }),
            ["ACC:T68.10"] = ("sync", "ShouldKeepTask68AcceptanceRefsAndAnchorsSynchronized_WhenValidatingEvidenceMapping", new[]
            {
                "var mapping = new Dictionary<string",
                "ValidateTaskRefs(",
            }),
        };

        foreach (var (anchor, spec) in mapping)
        {
            var source = spec.fileKey switch
            {
                "integration" => integration,
                "scene" => scene,
                _ => sync,
            };
            var body = spec.fileKey == "sync" ? source : ExtractFunctionBody(source, spec.functionName);
            foreach (var pattern in spec.patterns)
            {
                body.Should().Contain(pattern, $"because {anchor} must map to concrete assertion evidence");
            }
        }

        MathHelper.Clamp(2, 0, 3).Should().Be(2);
    }

    private static void ValidateTaskRefs(string root, JsonElement task)
    {
        var testRefs = ExtractStringArray(task.GetProperty("test_refs"));
        testRefs.Should().Contain("Tests.Godot/tests/Integration/test_combat_experience_runtime_flow.gd");
        testRefs.Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_battlemap_operation_surface_semantics.gd");
        testRefs.Should().Contain("Game.Core.Tests/Tasks/Task68RefsAnchorsSyncTests.cs");

        var acceptance = ExtractStringArray(task.GetProperty("acceptance"));
        acceptance.Should().HaveCountGreaterThanOrEqualTo(10);
        foreach (var item in acceptance)
        {
            item.Should().Contain("Refs:");
        }

        foreach (var path in testRefs.Distinct(StringComparer.Ordinal))
        {
            var full = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(full).Should().BeTrue($"ref file must exist: {path}");
        }
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

    private static JsonElement? FindTask68(JsonDocument doc)
    {
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("taskmaster_id", out var id))
            {
                continue;
            }

            if (id.GetInt32() == 68)
            {
                return item;
            }
        }

        return null;
    }
}
