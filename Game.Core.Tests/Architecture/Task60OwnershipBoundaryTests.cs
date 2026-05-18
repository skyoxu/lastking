using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Game.Core.Tests.Architecture;

public class Task60OwnershipBoundaryTests
{
    private const string BattleMapScreenPath = "Game.Godot/Scenes/Screens/BattleMapScreen.tscn";
    private const string BattleMapScreenScriptPath = "Game.Godot/Scripts/Screens/BattleMapScreen.gd";
    private const string BattleMapOperationControllerScriptPath = "Game.Godot/Scripts/Screens/BattleMapOperationController.gd";
    private const string BattleMapPresentationControllerScriptPath = "Game.Godot/Scripts/Screens/BattleMapPresentationController.gd";
    private const string BattleMapNavigationControllerScriptPath = "Game.Godot/Scripts/Screens/BattleMapNavigationController.gd";
    private const string BattleMapRuntimeCoordinatorScriptPath = "Game.Godot/Scripts/Screens/BattleMapRuntimeCoordinator.gd";
    private const string BattleMapRefsProviderScriptPath = "Game.Godot/Scripts/Screens/BattleMapRefsProvider.gd";
    private const string BattleMapOwnershipCoordinatorScriptPath = "Game.Godot/Scripts/Screens/BattleMapOwnershipCoordinator.gd";
    private const string BattleMapBridgeProviderScriptPath = "Game.Godot/Scripts/Screens/BattleMapBridgeProvider.gd";

    // ACC:T60.4
    [Fact]
    public void ShouldKeepProtectedOwnershipAnchorsPresent_WhenPlacementOverlayIsIntroduced()
    {
        var screen = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenPath.Replace('/', Path.DirectorySeparatorChar)));
        screen.Should().Contain("Background");
        screen.Should().Contain("CombatExperienceRuntimeBridge");
        screen.Should().Contain("WaveTimer");
        screen.Should().Contain("Margin");
        screen.Should().Contain("PresentationController");
        screen.Should().Contain("NavigationController");
        screen.Should().Contain("RuntimeCoordinator");
        screen.Should().Contain("RefsProvider");
        screen.Should().Contain("OwnershipCoordinator");
        screen.Should().Contain("BridgeProvider");
    }

    // ACC:T60.4
    [Fact]
    public void ShouldNotExposePlacementLegalityResponsibilities_WhenCheckingProtectedComponents()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().NotContain("register_overlay_controller(path: NodePath, controller: Node) -> void");
        script.Should().NotContain("_selection_controller.call(\"register_overlay_controller\", path, controller)");
        script.Should().NotContain("apply_legality_overlay");
        script.Should().NotContain("set_placement_context_active");
        script.Should().NotContain("SetSettlementOptionsForTest");
        script.Should().NotContain("_on_settlement_reward_selected");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldNotKeepOperationToFeedbackOrOutcomeBridgeHelpersOnBattleMapScreen()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().NotContain("_render_from_controller");
        script.Should().NotContain("_open_outcome_from_controller");
        script.Should().NotContain("_sync_outcome_from_controller");
        script.Should().NotContain("_is_settlement_open");
        script.Should().NotContain("_is_terminal_outcome_modal_visible");
        script.Should().NotContain("_mark_spawn_pulse");
        script.Should().NotContain("_clear_spawn_pulse");
        script.Should().NotContain("_reset_runtime_flags");
        script.Should().NotContain("_restart_from_terminal_outcome");
        script.Should().NotContain("_try_bridge_reset");
        script.Should().NotContain("_try_bridge_summary");
        script.Should().NotContain("\"render_callback\"");
        script.Should().NotContain("\"outcome_open_callback\"");
        script.Should().NotContain("\"outcome_sync_callback\"");
        script.Should().NotContain("\"outcome_is_settlement_open\"");
        script.Should().NotContain("\"outcome_is_terminal_visible\"");
        script.Should().NotContain("\"feedback_mark_spawn_pulse\"");
        script.Should().NotContain("\"feedback_clear_spawn_pulse\"");
        script.Should().NotContain("\"restart_callback\"");
        script.Should().NotContain("\"close_terminal_callback\"");
        script.Should().Contain("\"feedback_controller\": _feedback_controller");
        script.Should().Contain("\"outcome_controller\": _outcome_controller");
        script.Should().Contain("\"runtime_coordinator\": _runtime_coordinator");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldWireOperationControllerDirectlyToFeedbackAndOutcomeControllers()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapOperationControllerScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("var _feedback_controller: Node = null");
        script.Should().Contain("var _outcome_controller: Node = null");
        script.Should().Contain("_feedback_controller = refs[\"feedback_controller\"]");
        script.Should().Contain("_outcome_controller = refs[\"outcome_controller\"]");
        script.Should().NotContain("_render_callback");
        script.Should().NotContain("_outcome_open_callback");
        script.Should().NotContain("_outcome_sync_callback");
        script.Should().NotContain("_outcome_is_settlement_open");
        script.Should().NotContain("_outcome_is_terminal_visible");
        script.Should().NotContain("_feedback_mark_spawn_pulse");
        script.Should().NotContain("_feedback_clear_spawn_pulse");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldUseDedicatedBridgeProviderForBattleMapDynamicBridgeResolution()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().NotContain("func _current_bridge() -> Node:");
        script.Should().Contain("@onready var _bridge_provider: Node = $BridgeProvider");
        script.Should().Contain("\"screen\": self");
        script.Should().Contain("Callable(_bridge_provider, \"resolve_current_bridge\")");
        script.Should().NotContain("if _bridge.has_method(\"AdvanceSimulation\")");
        script.Should().NotContain("_try_bridge_reset");
        script.Should().NotContain("_try_bridge_summary");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldProvideBridgeProviderForBattleMapRuntimeBridgeSwaps()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapBridgeProviderScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("func configure(refs: Dictionary) -> void:");
        script.Should().Contain("func resolve_current_bridge() -> Node:");
        script.Should().Contain("_screen.get(\"_bridge\")");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldNotKeepLocalizationOrStaticTextHelpersOnBattleMapScreen()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().NotContain("_i18n");
        script.Should().NotContain("_apply_static_texts");
        script.Should().NotContain("_normalize_locale");
        script.Should().NotContain("func _t(key: String) -> String:");
        script.Should().Contain("\"presentation_controller\": _presentation_controller");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldProvidePresentationControllerForBattleMapStaticTextAndLocaleUpdates()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapPresentationControllerScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("func configure(refs: Dictionary) -> void:");
        script.Should().Contain("func sync_locale_and_texts() -> void:");
        script.Should().Contain("func translate(key: String) -> String:");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldNotKeepBackNavigationLogicOnBattleMapScreen()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().NotContain("func _on_back() -> void:");
        script.Should().NotContain("ShowMenu()");
        script.Should().NotContain("ClearCurrentScreen()");
        script.Should().Contain("\"navigation_controller\": _navigation_controller");
        script.Should().Contain("Callable(_navigation_controller, \"navigate_back_to_main_menu\")");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldProvideNavigationControllerForBattleMapBackFlow()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapNavigationControllerScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("func configure(refs: Dictionary) -> void:");
        script.Should().Contain("func navigate_back_to_main_menu() -> void:");
        script.Should().Contain("menu.call(\"ShowMenu\")");
        script.Should().Contain("nav.call(\"ClearCurrentScreen\")");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldNotKeepRuntimeBootstrapOrFrameOrchestrationOnBattleMapScreen()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("\"runtime_coordinator\": _runtime_coordinator");
        script.Should().NotContain("bridge.call(\"ResetForInteractiveRun\")");
        script.Should().NotContain("_feedback_controller.call(\"render_loaded_summary\"");
        script.Should().NotContain("_outcome_controller.call(\"close_all\")");
        script.Should().NotContain("_presentation_controller.call(\"sync_locale_and_texts\")");
        script.Should().NotContain("_feedback_controller.call(\"process_frame\", delta)");
        script.Should().NotContain("_outcome_controller.call(\"sync_terminal_outcome_from_bridge\"");
        script.Should().Contain("_runtime_coordinator.call(\"initialize_runtime\")");
        script.Should().Contain("_runtime_coordinator.call(\"process_runtime_frame\", delta)");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldProvideRuntimeCoordinatorForBattleMapBootstrapAndPerFrameFlow()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapRuntimeCoordinatorScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("func configure(refs: Dictionary) -> void:");
        script.Should().Contain("func initialize_runtime() -> void:");
        script.Should().Contain("func process_runtime_frame(delta: float) -> void:");
        script.Should().Contain("bridge.call(\"ResetForInteractiveRun\")");
        script.Should().Contain("_outcome_controller.call(\"close_all\")");
        script.Should().Contain("_feedback_controller.call(\"render_loaded_summary\"");
        script.Should().Contain("_outcome_controller.call(\"sync_terminal_outcome_from_bridge\"");
        script.Should().Contain("_feedback_controller.call(\"process_frame\", delta)");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldNotKeepInputSignalWiringOnBattleMapScreen()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().NotContain("func _connect_input_signals() -> void:");
        script.Should().NotContain("_build_btn.pressed.connect");
        script.Should().NotContain("_wave_btn.pressed.connect");
        script.Should().NotContain("_auto_wave_btn.pressed.connect");
        script.Should().NotContain("_exchange_btn.pressed.connect");
        script.Should().NotContain("_cleanup_btn.pressed.connect");
        script.Should().NotContain("_finish_btn.pressed.connect");
        script.Should().NotContain("_back_btn.pressed.connect");
        script.Should().NotContain("_wave_timer.timeout.connect");
        script.Should().Contain("_operation_controller.call(\"connect_signals\")");
        script.Should().Contain("_navigation_controller.call(\"connect_signals\")");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldProvideControllerOwnedSignalWiringForBattleMapInputs()
    {
        var operationScript = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapOperationControllerScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        operationScript.Should().Contain("func connect_signals() -> void:");
        operationScript.Should().Contain("_wave_timer.timeout.connect");
        operationScript.Should().NotContain("_build_btn.pressed.connect");
        operationScript.Should().NotContain("_wave_btn.pressed.connect");
        operationScript.Should().NotContain("_exchange_btn.pressed.connect");
        operationScript.Should().NotContain("_cleanup_btn.pressed.connect");
        operationScript.Should().NotContain("_finish_btn.pressed.connect");
        operationScript.Should().Contain("func on_auto_wave() -> void:");

        var navigationScript = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapNavigationControllerScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        navigationScript.Should().Contain("func connect_signals() -> void:");
        navigationScript.Should().NotContain("_back_btn.pressed.connect");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldNotKeepMostBattleMapUiRefsOnBattleMapScreen()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().NotContain("@onready var _status:");
        script.Should().NotContain("@onready var _summary:");
        script.Should().NotContain("@onready var _legend:");
        script.Should().NotContain("@onready var _metrics_help:");
        script.Should().NotContain("@onready var _build_btn:");
        script.Should().NotContain("@onready var _wave_btn:");
        script.Should().NotContain("@onready var _auto_wave_btn:");
        script.Should().NotContain("@onready var _exchange_btn:");
        script.Should().NotContain("@onready var _cleanup_btn:");
        script.Should().NotContain("@onready var _finish_btn:");
        script.Should().NotContain("@onready var _back_btn:");
        script.Should().NotContain("@onready var _enemy_spawn_a:");
        script.Should().NotContain("@onready var _enemy_spawn_b:");
        script.Should().Contain("@onready var _refs_provider: Node = $RefsProvider");
        script.Should().Contain("var refs: Dictionary = _refs_provider.call(\"build_refs\")");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldProvideBattleMapRefsProviderForControllerRefAggregation()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapRefsProviderScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("func configure(refs: Dictionary) -> void:");
        script.Should().Contain("func build_refs() -> Dictionary:");
        script.Should().Contain("\"status\": _screen.get_node(\"LegacyPrototypeRoot/VBox/Status\")");
        script.Should().NotContain("\"auto_wave_btn\":");
        script.Should().Contain("\"enemy_spawn_a\": _screen.get_node(\"Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA\")");
        script.Should().Contain("\"victory_restart_btn\": _screen.get_node(\"VictoryOutcomeModal/VBox/Actions/RestartBtn\")");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldNotKeepOwnershipMarkerConfigurationOnBattleMapScreen()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapScreenScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().NotContain("func _configure_ownership_markers() -> void:");
        script.Should().NotContain("_background.set_meta(\"ownership_container\", \"battlefield_presentation\")");
        script.Should().NotContain("_bridge.set_meta(\"ownership_container\", \"runtime_bridge\")");
        script.Should().NotContain("_wave_timer.set_meta(\"ownership_container\", \"runtime_bridge\")");
        script.Should().NotContain("$Margin.set_meta(\"ownership_container\", \"legacy_prototype\")");
        script.Should().Contain("_ownership_coordinator.call(\"apply_ownership_markers\")");
    }

    // ACC:T57.4
    [Fact]
    public void ShouldProvideOwnershipCoordinatorForBattleMapProtectedMetaMarkers()
    {
        var script = File.ReadAllText(Path.Combine(FindRepositoryRoot(), BattleMapOwnershipCoordinatorScriptPath.Replace('/', Path.DirectorySeparatorChar)));
        script.Should().Contain("func configure(refs: Dictionary) -> void:");
        script.Should().Contain("func apply_ownership_markers() -> void:");
        script.Should().Contain("_background.set_meta(\"ownership_container\", \"battlefield_presentation\")");
        script.Should().Contain("_bridge.set_meta(\"ownership_container\", \"runtime_bridge\")");
        script.Should().Contain("_wave_timer.set_meta(\"ownership_container\", \"runtime_bridge\")");
        script.Should().Contain("_margin.set_meta(\"ownership_container\", \"legacy_prototype\")");
        script.Should().Contain("_margin.set_meta(\"migration_only\", true)");
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
        runtimeFlow.Should().Contain("assert_str(str(reopened_screen.get_node(\"LegacyPrototypeRoot\").get_meta(\"ownership_container\"))).is_equal(\"legacy_prototype\")");
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
