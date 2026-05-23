using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Tasks;

public sealed class Task66RefsAnchorsSyncTests
{
    // ACC:T66.10
    [Fact]
    public void ShouldKeepTask66AcceptanceRefsAndAnchorsSynchronized_WhenValidatingEvidenceMapping()
    {
        var root = FindRepositoryRoot();
        using var back = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_back.json")));
        using var gameplay = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".taskmaster", "tasks", "tasks_gameplay.json")));
        var integration = File.ReadAllText(Path.Combine(root, "Tests.Godot", "tests", "Integration", "test_battle_map_screen_runtime_flow.gd"));
        var scene = File.ReadAllText(Path.Combine(root, "Tests.Godot", "tests", "Scenes", "BattleMap", "test_battlemap_operation_surface_semantics.gd"));

        var backTask = FindTask66(back);
        var gameplayTask = FindTask66(gameplay);
        backTask.Should().NotBeNull();
        gameplayTask.Should().NotBeNull();

        var backRefs = ExtractStringArray(backTask!.Value.GetProperty("test_refs"));
        var gameplayRefs = ExtractStringArray(gameplayTask!.Value.GetProperty("test_refs"));

        backRefs.Should().Contain("Game.Core.Tests/Tasks/Task66RefsAnchorsSyncTests.cs");
        gameplayRefs.Should().Contain("Game.Core.Tests/Tasks/Task66RefsAnchorsSyncTests.cs");
        backRefs.Should().Contain("Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd");
        gameplayRefs.Should().Contain("Tests.Godot/tests/Scenes/BattleMap/test_battlemap_operation_surface_semantics.gd");

        var requiredAnchors = Enumerable.Range(1, 10).Select(i => $"ACC:T66.{i}").ToArray();
        var combined = integration + "\n" + scene + "\n" + File.ReadAllText(Path.Combine(root, "Game.Core.Tests", "Tasks", "Task66RefsAnchorsSyncTests.cs"));
        foreach (var anchor in requiredAnchors)
        {
            combined.Should().Contain(anchor);
        }

        integration.Should().Contain("func test_legacy_labels_should_not_be_authoritative_source_for_runtime_feedback() -> void:");
        integration.Should().Contain("summary_label.text = \"LEGACY_OVERRIDE_SUMMARY\"");
        integration.Should().Contain("assert_that(after_legacy_override).is_equal(baseline_summary)");
        integration.Should().Contain("String(status_label.text).to_lower().find(wave_text)");
        integration.Should().Contain("String(status_label.text).to_lower().find(finished_text)");
        integration.Should().Contain("assert_object(screen.get_node_or_null(\"LegacyPrototypeRoot/VBox/Legend\")).is_null()");
        integration.Should().Contain("assert_object(screen.get_node_or_null(\"LegacyPrototypeRoot/VBox/MetricsHelp\")).is_null()");

        scene.Should().Contain("func test_t66_feedback_ownership_should_keep_runtime_bridge_as_state_authority() -> void:");
        scene.Should().Contain("func test_t66_state_transition_semantics_should_stay_bridge_driven_after_legacy_text_override() -> void:");
        scene.Should().Contain("summary_label.text = \"LEGACY_TEXT_ONLY_OVERRIDE\"");
        scene.Should().Contain("summary_label.text = \"LEGACY_OVERRIDE_SHOULD_NOT_DRIVE_STATE\"");

        var mapping = new Dictionary<string, (string fileKey, string functionName, string[] patterns)>
        {
            ["ACC:T66.1"] = ("integration", "test_legacy_labels_should_not_be_authoritative_source_for_runtime_feedback", new[]
            {
                "summary_label.text = \"LEGACY_OVERRIDE_SUMMARY\"",
                "assert_that(after_legacy_override).is_equal(baseline_summary)",
            }),
            ["ACC:T66.2"] = ("integration", "test_non_battlefield_layout_perturbation_should_not_shift_header_or_footer", new[]
            {
                "assert_str(String(status_label.text)).is_equal(status_before)",
            }),
            ["ACC:T66.3"] = ("integration", "test_1600x900_frame_keeps_three_player_visible_bands_simultaneously_visible", new[]
            {
                "_is_visible_inside_viewport",
            }),
            ["ACC:T66.4"] = ("integration", "test_reenter_battle_map_keeps_three_band_frame_stable_after_viewport_resize", new[]
            {
                "assert_that(second_snapshot).is_equal(first_snapshot)",
            }),
            ["ACC:T66.5"] = ("scene", "test_t66_feedback_ownership_should_keep_runtime_bridge_as_state_authority", new[]
            {
                "assert_that(after_legacy_override).is_equal(before_summary)",
            }),
            ["ACC:T66.6"] = ("integration", "test_legacy_labels_should_not_be_authoritative_source_for_runtime_feedback", new[]
            {
                "assert_object(screen.get_node_or_null(\"LegacyPrototypeRoot/VBox/Legend\")).is_null()",
                "assert_object(screen.get_node_or_null(\"LegacyPrototypeRoot/VBox/MetricsHelp\")).is_null()",
            }),
            ["ACC:T66.7"] = ("scene", "test_t66_state_transition_semantics_should_stay_bridge_driven_after_legacy_text_override", new[]
            {
                "_request_hud_action(screen, \"finish\")",
                "failure_status.to_lower().find(require_cleanup_text)",
            }),
            ["ACC:T66.8"] = ("scene", "test_t66_state_transition_semantics_should_stay_bridge_driven_after_legacy_text_override", new[]
            {
                "completion_status.to_lower().find(finished_text)",
            }),
            ["ACC:T66.9"] = ("scene", "test_t66_state_transition_semantics_should_stay_bridge_driven_after_legacy_text_override", new[]
            {
                "completion_snapshot.get(\"combat_exchanges\", 0)",
            }),
            ["ACC:T66.10"] = ("sync", "ShouldKeepTask66AcceptanceRefsAndAnchorsSynchronized_WhenValidatingEvidenceMapping", new[]
            {
                "var mapping = new Dictionary<string",
                "ExtractFunctionBody(",
            }),
        };

        foreach (var (anchor, spec) in mapping)
        {
            var source = spec.fileKey switch
            {
                "integration" => integration,
                "scene" => scene,
                _ => File.ReadAllText(Path.Combine(root, "Game.Core.Tests", "Tasks", "Task66RefsAnchorsSyncTests.cs")),
            };
            var body = spec.fileKey == "sync" ? source : ExtractFunctionBody(source, spec.functionName);
            foreach (var pattern in spec.patterns)
            {
                body.Should().Contain(pattern, $"because {anchor} must map to concrete assertion evidence");
            }
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

    private static JsonElement? FindTask66(JsonDocument doc)
    {
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("taskmaster_id", out var id))
            {
                continue;
            }

            if (id.GetInt32() == 66)
            {
                return item;
            }
        }

        return null;
    }
}
