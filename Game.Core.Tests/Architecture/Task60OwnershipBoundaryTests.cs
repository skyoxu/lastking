using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Architecture;

public class Task60OwnershipBoundaryTests
{
    private const string BattleMapScreenPath = "Game.Godot/Scenes/Screens/BattleMapScreen.tscn";
    private const string BattleMapScreenScriptPath = "Game.Godot/Scripts/Screens/BattleMapScreen.gd";

    // ACC:T60.4
    [Fact]
    public void ShouldKeepProtectedOwnershipAnchorsPresent_WhenPlacementOverlayIsIntroduced()
    {
        var screen = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenPath.Replace('/', Path.DirectorySeparatorChar)));
        screen.Should().Contain("Background");
        screen.Should().Contain("CombatExperienceRuntimeBridge");
        screen.Should().Contain("WaveTimer");
        screen.Should().Contain("Margin");
    }

    // ACC:T60.4
    [Fact]
    public void ShouldNotExposePlacementLegalityResponsibilities_WhenCheckingProtectedComponents()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("register_overlay_controller(path: NodePath, controller: Node) -> void");
        script.Should().Contain("var node_name := String(path.get_concatenated_names()).replace(\"/\", \"_\")");
        script.Should().Contain("var existing := get_node_or_null(NodePath(node_name))");
        script.Should().Contain("if existing != null and existing != controller:");
        script.Should().Contain("existing.queue_free()");
        script.Should().Contain("controller.name = node_name");
        script.Should().Contain("add_child(controller)");
        script.Should().NotContain("apply_legality_overlay");
        script.Should().NotContain("set_placement_context_active");
    }

    // ACC:T60.4
    [Fact]
    public void ShouldKeepOwnershipResponsibilitiesStableInRuntimeFlowEvidence_WhenPlacementOverlayChanges()
    {
        var runtimeFlow = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "Tests.Godot/tests/Integration/test_battle_map_screen_runtime_flow.gd".Replace('/', Path.DirectorySeparatorChar)));

        runtimeFlow.Should().Contain("func test_battle_map_cycle_should_keep_hud_singleton_and_navigator_ownership()");
        runtimeFlow.Should().Contain("assert_int(_hud_count(main)).is_equal(1)");
        runtimeFlow.Should().Contain("assert_object(main.get_node_or_null(\"ScreenNavigator\")).is_not_null()");
        runtimeFlow.Should().Contain("assert_str(str(reopened_screen.get_node(\"CombatExperienceRuntimeBridge\").get_meta(\"ownership_container\"))).is_equal(\"runtime_bridge\")");
        runtimeFlow.Should().Contain("assert_str(str(reopened_screen.get_node(\"Margin\").get_meta(\"ownership_container\"))).is_equal(\"legacy_prototype\")");
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "Lastking.sln")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Repository root not found from test base directory.");
    }
}
