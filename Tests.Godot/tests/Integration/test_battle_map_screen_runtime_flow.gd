extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame


func _status_and_summary(screen: Node) -> Dictionary:
	var status: Label = screen.get_node("Margin/VBox/Status")
	var summary: Label = screen.get_node("Margin/VBox/Summary")
	return {
		"status": String(status.text),
		"summary": String(summary.text),
	}


func _bridge_summary_metrics(bridge: Node) -> Dictionary:
	if not bridge.has_method("GetSummary"):
		return {}
	var payload: Variant = bridge.call("GetSummary")
	if payload is Dictionary:
		return payload
	return {}


func _hud_count(main: Node) -> int:
	var runtime_ui := main.get_node("RuntimeUi")
	var count := 0
	for child in runtime_ui.get_children():
		if str(child.name) == "HUD":
			count += 1
	return count


func _is_visible_inside_viewport(control: Control, viewport_size: Vector2) -> bool:
	var top_left := control.global_position
	var bottom_right := top_left + control.size
	return control.visible \
		and top_left.x >= 0.0 \
		and top_left.y >= 0.0 \
		and bottom_right.x <= viewport_size.x \
		and bottom_right.y <= viewport_size.y


func _frame_snapshot(screen: Control) -> Dictionary:
	var background: Control = screen.get_node("Background")
	var title: Control = screen.get_node("Margin/VBox/Title")
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	return {
		"background_pos": background.global_position,
		"background_size": background.size,
		"title_pos": title.global_position,
		"title_size": title.size,
		"metrics_pos": metrics_help.global_position,
		"metrics_size": metrics_help.size,
	}


func _spawn_cue_colors(screen: Control) -> Array[Color]:
	var spawn_a: ColorRect = screen.get_node("Background/EnemySpawnA")
	var spawn_b: ColorRect = screen.get_node("Background/EnemySpawnB")
	return [spawn_a.color, spawn_b.color]


const _TASK56_OWNERSHIP_CONTAINERS: PackedStringArray = [
	"battlefield_presentation",
	"runtime_bridge",
	"legacy_prototype",
]

const _TASK56_NODE_OWNERSHIP_MAP := {
	"Background": "battlefield_presentation",
	"Background/Path": "battlefield_presentation",
	"Background/PlayerCastle": "battlefield_presentation",
	"Background/EnemySpawnA": "battlefield_presentation",
	"Background/EnemySpawnB": "battlefield_presentation",
	"Background/BuildSlotA": "battlefield_presentation",
	"Background/BuildSlotB": "battlefield_presentation",
	"CombatExperienceRuntimeBridge": "runtime_bridge",
	"WaveTimer": "runtime_bridge",
	"Margin": "legacy_prototype",
	"Margin/VBox": "legacy_prototype",
	"Margin/VBox/Title": "legacy_prototype",
	"Margin/VBox/Status": "legacy_prototype",
	"Margin/VBox/Controls": "legacy_prototype",
	"Margin/VBox/Controls/BuildBtn": "legacy_prototype",
	"Margin/VBox/Controls/WaveBtn": "legacy_prototype",
	"Margin/VBox/Controls/AutoWaveBtn": "legacy_prototype",
	"Margin/VBox/Controls/ExchangeBtn": "legacy_prototype",
	"Margin/VBox/Controls/CleanupBtn": "legacy_prototype",
	"Margin/VBox/Controls/FinishBtn": "legacy_prototype",
	"Margin/VBox/Controls/BackBtn": "legacy_prototype",
	"Margin/VBox/Summary": "legacy_prototype",
	"Margin/VBox/Legend": "legacy_prototype",
	"Margin/VBox/MetricsHelp": "legacy_prototype",
}


func _resolve_ownership_container(node: Node) -> String:
	var current: Node = node
	while current != null:
		if current.has_meta("ownership_container"):
			return str(current.get_meta("ownership_container"))
		current = current.get_parent()
	return ""


func _collect_ownership_roots(screen: Control) -> Dictionary:
	var counts: Dictionary = {}
	var pending: Array[Node] = [screen]
	while pending.size() > 0:
		var current: Node = pending.pop_back()
		if current.has_meta("ownership_container"):
			var container := str(current.get_meta("ownership_container"))
			counts[container] = int(counts.get(container, 0)) + 1
		for child_variant in current.get_children():
			var child := child_variant as Node
			if child != null:
				pending.push_back(child)
	return counts


func _assert_task56_ownership_map(screen: Control) -> void:
	var root_counts := _collect_ownership_roots(screen)
	assert_int(root_counts.size()).is_equal(_TASK56_OWNERSHIP_CONTAINERS.size())
	for container in _TASK56_OWNERSHIP_CONTAINERS:
		assert_bool(root_counts.has(container)).is_true()
	assert_int(int(root_counts["battlefield_presentation"])).is_equal(1)
	assert_int(int(root_counts["runtime_bridge"])).is_equal(2)
	assert_int(int(root_counts["legacy_prototype"])).is_equal(1)

	for node_path_variant in _TASK56_NODE_OWNERSHIP_MAP.keys():
		var node_path := str(node_path_variant)
		var node := screen.get_node_or_null(node_path)
		assert_object(node).is_not_null()
		var actual_container := _resolve_ownership_container(node)
		var expected_container := str(_TASK56_NODE_OWNERSHIP_MAP[node_path])
		assert_str(actual_container).is_equal(expected_container)
		for other_container in _TASK56_OWNERSHIP_CONTAINERS:
			if other_container == expected_container:
				continue
			assert_str(actual_container).is_not_equal(other_container)

	var legacy_container: Node = screen.get_node("Margin")
	assert_bool(legacy_container.has_meta("migration_only")).is_true()
	assert_bool(bool(legacy_container.get_meta("migration_only"))).is_true()
	assert_bool(screen.get_node("Background").has_meta("migration_only")).is_false()
	assert_bool(screen.get_node("CombatExperienceRuntimeBridge").has_meta("migration_only")).is_false()
	assert_bool(screen.get_node("WaveTimer").has_meta("migration_only")).is_false()


# ACC:T55.1
# ACC:T56.1
# ACC:T58.1
# ACC:T59.1
func test_narrow_layout_keeps_header_footer_fixed_when_only_battlefield_moves() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	screen.size = Vector2(540.0, 320.0)
	await _await_frames(2)

	var background: Control = screen.get_node("Background")
	var title: Control = screen.get_node("Margin/VBox/Title")
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	var title_before := title.global_position
	var metrics_before := metrics_help.global_position
	var background_before := background.global_position

	background.position = background.position + Vector2(-120.0, 0.0)
	await _await_frames(1)

	assert_float(background.global_position.x).is_equal(background_before.x - 120.0)
	assert_that(title.global_position).is_equal(title_before)
	assert_that(metrics_help.global_position).is_equal(metrics_before)
	_assert_task56_ownership_map(screen)


# ACC:T55.1
# ACC:T56.2
# ACC:T58.2
# ACC:T59.2
# ACC:T66.2
func test_non_battlefield_layout_perturbation_should_not_shift_header_or_footer() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	screen.size = Vector2(540.0, 320.0)
	await _await_frames(2)

	var title: Control = screen.get_node("Margin/VBox/Title")
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	var controls: Control = screen.get_node("Margin/VBox/Controls")
	var status_label: Label = screen.get_node("Margin/VBox/Status")
	var summary_label: Label = screen.get_node("Margin/VBox/Summary")
	var title_before := title.global_position
	var metrics_before := metrics_help.global_position
	var status_before := String(status_label.text)
	var summary_before := String(summary_label.text)

	controls.position = controls.position + Vector2(80.0, 0.0)
	await _await_frames(1)

	assert_that(title.global_position).is_equal(title_before)
	assert_that(metrics_help.global_position).is_equal(metrics_before)
	assert_str(String(status_label.text)).is_equal(status_before)
	assert_str(String(summary_label.text)).is_equal(summary_before)


# ACC:T55.1
# ACC:T56.3
# ACC:T58.3
# ACC:T59.3
# ACC:T66.3
func test_1440x900_frame_keeps_three_player_visible_bands_simultaneously_visible() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	screen.size = Vector2(1440.0, 900.0)
	await _await_frames(2)

	var viewport := Vector2(1440.0, 900.0)
	var background: Control = screen.get_node("Background")
	var title: Control = screen.get_node("Margin/VBox/Title")
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	var path: Line2D = screen.get_node("Background/Path")
	var background_top_before := background.global_position.y
	var background_height_before := background.size.y
	var title_top_before := title.global_position.y
	var title_height_before := title.size.y
	var metrics_top_before := metrics_help.global_position.y
	var metrics_height_before := metrics_help.size.y

	assert_bool(_is_visible_inside_viewport(title, viewport)).is_true()
	assert_bool(_is_visible_inside_viewport(background, viewport)).is_true()
	assert_bool(_is_visible_inside_viewport(metrics_help, viewport)).is_true()

	var title_mid := title.global_position.y + title.size.y * 0.5
	var midpoint_index := int(path.points.size() * 0.5)
	var battlefield_mid := background.global_position.y + path.points[midpoint_index].y
	var bottom_mid := metrics_help.global_position.y + metrics_help.size.y * 0.5
	assert_float(title_mid).is_less(battlefield_mid)
	assert_float(battlefield_mid).is_less(bottom_mid)

	# Resize stability: top/bottom bands stay anchored and keep their heights.
	screen.size = Vector2(1280.0, 720.0)
	await _await_frames(2)
	screen.size = Vector2(1440.0, 900.0)
	await _await_frames(2)
	assert_float(background.global_position.y).is_equal(background_top_before)
	assert_float(background.size.y).is_equal(background_height_before)
	assert_float(title.global_position.y).is_equal(title_top_before)
	assert_float(title.size.y).is_equal(title_height_before)
	assert_float(metrics_help.global_position.y).is_equal(metrics_top_before)
	assert_float(metrics_help.size.y).is_equal(metrics_height_before)


# ACC:T55.1
# ACC:T56.4
# ACC:T58.4
# ACC:T59.4
# ACC:T66.4
func test_reenter_battle_map_keeps_three_band_frame_stable_after_viewport_resize() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	nav.set("UseFadeTransition", false)
	var screen_root: Node = main.get_node("RuntimeUi/ScreenRoot")

	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)
	var first_screen: Control = screen_root.get_node("BattleMapScreen")
	first_screen.size = Vector2(1440.0, 900.0)
	await _await_frames(2)
	var first_snapshot := _frame_snapshot(first_screen)

	nav.call("ClearCurrentScreen")
	await _await_frames(2)
	ok_enter = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)
	var second_screen: Control = screen_root.get_node("BattleMapScreen")
	second_screen.size = Vector2(1440.0, 900.0)
	await _await_frames(2)
	var second_snapshot := _frame_snapshot(second_screen)
	var second_path: Line2D = second_screen.get_node("Background/Path")
	var second_viewport := Vector2(1440.0, 900.0)
	var second_mid_index := int(second_path.points.size() * 0.5)
	var second_background: Control = second_screen.get_node("Background")
	var second_title: Control = second_screen.get_node("Margin/VBox/Title")
	var second_metrics_help: Control = second_screen.get_node("Margin/VBox/MetricsHelp")

	assert_bool(_is_visible_inside_viewport(second_title, second_viewport)).is_true()
	assert_bool(_is_visible_inside_viewport(second_background, second_viewport)).is_true()
	assert_bool(_is_visible_inside_viewport(second_metrics_help, second_viewport)).is_true()
	var second_title_mid := second_title.global_position.y + second_title.size.y * 0.5
	var second_battlefield_mid := second_background.global_position.y + second_path.points[second_mid_index].y
	var second_bottom_mid := second_metrics_help.global_position.y + second_metrics_help.size.y * 0.5
	assert_float(second_title_mid).is_less(second_battlefield_mid)
	assert_float(second_battlefield_mid).is_less(second_bottom_mid)

	# Re-entry + resize still keeps all three bands visible and ordered.
	second_screen.size = Vector2(1280.0, 720.0)
	await _await_frames(2)
	var resized_viewport := Vector2(1280.0, 720.0)
	assert_bool(_is_visible_inside_viewport(second_title, resized_viewport)).is_true()
	assert_bool(_is_visible_inside_viewport(second_background, resized_viewport)).is_true()
	assert_bool(_is_visible_inside_viewport(second_metrics_help, resized_viewport)).is_true()
	second_title_mid = second_title.global_position.y + second_title.size.y * 0.5
	second_battlefield_mid = second_background.global_position.y + second_path.points[second_mid_index].y
	second_bottom_mid = second_metrics_help.global_position.y + second_metrics_help.size.y * 0.5
	assert_float(second_title_mid).is_less(second_battlefield_mid)
	assert_float(second_battlefield_mid).is_less(second_bottom_mid)

	second_screen.size = Vector2(1440.0, 900.0)
	await _await_frames(2)
	var title_x_before := second_title.global_position.x
	var metrics_x_before := second_metrics_help.global_position.x
	var battlefield_x_before := second_background.global_position.x

	second_background.position = second_background.position + Vector2(-80.0, 0.0)
	await _await_frames(1)

	assert_that(second_snapshot).is_equal(first_snapshot)
	assert_float(second_background.global_position.x).is_equal(battlefield_x_before - 80.0)
	assert_float(second_title.global_position.x).is_equal(title_x_before)
	assert_float(second_metrics_help.global_position.x).is_equal(metrics_x_before)


# ACC:T55.1
# ACC:T55.3
# ACC:T55.4
# ACC:T55.6
# ACC:T55.8
# ACC:T56.5
# ACC:T57.8
# ACC:T58.7
# ACC:T58.8
# ACC:T58.9
# ACC:T58.10
# ACC:T58.11
# ACC:T59.5
func test_battle_map_screen_minimum_runtime_loop_is_player_visible() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)
	_assert_task56_ownership_map(screen)

	var build_btn := screen.get_node("Margin/VBox/Controls/BuildBtn")
	var wave_btn := screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn := screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn := screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn := screen.get_node("Margin/VBox/Controls/FinishBtn")
	var summary := screen.get_node("Margin/VBox/Summary")
	var status := screen.get_node("Margin/VBox/Status")

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	assert_bool(String(status.text).length() > 0).is_true()
	assert_bool(String(summary.text).find(":") >= 0).is_true()
	assert_bool(String(summary.text).find("HP") >= 0 or String(summary.text).find("生命") >= 0).is_true()
	assert_bool(String(summary.text).find("Friendly") >= 0 or String(summary.text).find("友军") >= 0).is_true()
	assert_bool(String(summary.text).find("Enemy") >= 0 or String(summary.text).find("敌军") >= 0).is_true()
	_assert_task56_ownership_map(screen)


# ACC:T57.1
# ACC:T57.5
# ACC:T59.6
func test_battle_map_coordinator_guards_should_block_out_of_order_actions_and_preserve_runtime_state() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)
	_assert_task56_ownership_map(screen)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var status_label: Label = screen.get_node("Margin/VBox/Status")
	var summary_before := _bridge_summary_metrics(bridge)

	exchange_btn.emit_signal("pressed")
	await _await_frames(1)
	var status_after_exchange := String(status_label.text)
	assert_bool(status_after_exchange.find("wave") >= 0 or status_after_exchange.find("波次") >= 0).is_true()

	cleanup_btn.emit_signal("pressed")
	await _await_frames(1)
	var status_after_cleanup := String(status_label.text)
	var status_after_cleanup_lc := status_after_cleanup.to_lower()
	assert_bool(
		status_after_cleanup_lc.find("exchange") >= 0
		or status_after_cleanup_lc.find("combat") >= 0
		or status_after_cleanup.find("交战") >= 0
	).is_true()

	finish_btn.emit_signal("pressed")
	await _await_frames(1)
	var status_after_finish := String(status_label.text)
	assert_bool(status_after_finish.to_lower().find("cleanup") >= 0 or status_after_finish.find("清理") >= 0).is_true()

	var summary_after_invalid := _bridge_summary_metrics(bridge)
	assert_that(summary_after_invalid).is_equal(summary_before)

	wave_btn.emit_signal("pressed")
	await _await_frames(1)
	exchange_btn.emit_signal("pressed")
	await _await_frames(1)
	cleanup_btn.emit_signal("pressed")
	await _await_frames(1)
	finish_btn.emit_signal("pressed")
	await _await_frames(1)
	var status_after_valid_flow := String(status_label.text)
	assert_bool(status_after_valid_flow.find("finished") >= 0 or status_after_valid_flow.find("结束") >= 0).is_true()
	_assert_task56_ownership_map(screen)


# ACC:T55.3
# ACC:T55.7
# ACC:T56.6
# ACC:T57.3
# ACC:T58.5
# ACC:T59.7
# ACC:T63.3
# ACC:T63.7
# ACC:T63.9
func test_battle_map_runtime_summary_should_distinguish_empty_progressed_and_completion_states() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var build_btn: Button = screen.get_node("Margin/VBox/Controls/BuildBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")
	var status_label: Label = screen.get_node("Margin/VBox/Status")
	var summary_label: Label = screen.get_node("Margin/VBox/Summary")

	var empty_status := String(status_label.text)
	var empty_summary := String(summary_label.text)
	var empty_metrics := _bridge_summary_metrics(bridge)
	assert_bool(empty_status.find("loaded") >= 0 or empty_status.find("加载") >= 0).is_true()
	assert_bool(empty_status.find("finished") < 0 and empty_status.find("结束") < 0).is_true()
	assert_bool(empty_status.length() > 0).is_true()
	assert_bool(empty_summary.length() > 0).is_true()
	assert_int(int(empty_metrics["friendly_units_deployed"])).is_equal(0)
	assert_int(int(empty_metrics["enemy_units_spawned"])).is_equal(0)
	assert_int(int(empty_metrics["combat_exchanges"])).is_equal(0)
	assert_int(int(empty_metrics["dead_units_retired"])).is_equal(0)
	await _await_frames(3)
	assert_str(status_label.text).is_equal(empty_status)
	assert_str(summary_label.text).is_equal(empty_summary)

	finish_btn.emit_signal("pressed")
	await _await_frames(1)
	var failure_status := String(status_label.text)
	var failure_status_lc := failure_status.to_lower()
	assert_bool(failure_status.length() > 0).is_true()
	assert_bool(failure_status != empty_status).is_true()
	assert_bool(failure_status_lc.find("cleanup") >= 0 or failure_status.find("清理") >= 0).is_true()
	assert_bool(failure_status_lc.find("finished") < 0 and failure_status.find("结束") < 0).is_true()
	await _await_frames(2)
	assert_str(status_label.text).is_equal(failure_status)

	wave_btn.emit_signal("pressed")
	await _await_frames(1)
	for _i in range(120):
		if bridge.has_method("AdvanceSimulation"):
			bridge.call("AdvanceSimulation", 0.1)
	await _await_frames(1)

	var progressed_status := String(status_label.text)
	var progressed_summary := String(summary_label.text)
	var progressed_metrics := _bridge_summary_metrics(bridge)
	assert_int(int(progressed_metrics["enemy_units_spawned"])).is_greater_equal(2)
	assert_int(int(progressed_metrics["castle_hp"])).is_less(int(empty_metrics["castle_hp"]))
	assert_int(int(progressed_metrics["friendly_units_deployed"])).is_equal(0)
	assert_bool(progressed_status != empty_status).is_true()
	assert_bool(progressed_status != failure_status).is_true()
	assert_bool(progressed_summary != empty_summary).is_true()
	await _await_frames(3)
	assert_str(status_label.text).is_equal(progressed_status)
	assert_str(summary_label.text).is_equal(progressed_summary)

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	var completion_status := String(status_label.text)
	var completion_summary := String(summary_label.text)
	var completion_metrics := _bridge_summary_metrics(bridge)
	assert_int(int(completion_metrics["friendly_units_deployed"])).is_greater_equal(1)
	assert_int(int(completion_metrics["enemy_units_spawned"])).is_greater_equal(2)
	assert_int(int(completion_metrics["combat_exchanges"])).is_greater_equal(1)
	assert_bool(completion_status.find("finished") >= 0 or completion_status.find("结束") >= 0).is_true()
	assert_bool(completion_status.find("cleanup") < 0 and completion_status.find("清理") < 0).is_true()
	assert_bool(completion_status != progressed_status).is_true()
	assert_bool(completion_status != failure_status).is_true()
	assert_bool(completion_summary != progressed_summary).is_true()
	assert_bool(completion_summary.find("HP") >= 0 or completion_summary.find("生命") >= 0).is_true()
	assert_bool(completion_summary.find("Friendly") >= 0 or completion_summary.find("友军") >= 0).is_true()
	assert_bool(completion_summary.find("Enemy") >= 0 or completion_summary.find("敌军") >= 0).is_true()
	await _await_frames(3)
	assert_str(status_label.text).is_equal(completion_status)
	assert_str(summary_label.text).is_equal(completion_summary)

	assert_bool(progressed_metrics != completion_metrics).is_true()
	assert_bool(empty_metrics != progressed_metrics).is_true()


# ACC:T57.6
# ACC:T57.10
# ACC:T59.8
func test_battle_map_back_action_should_handoff_exit_to_navigator_and_restore_main_menu() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	get_tree().get_root().add_child(main)
	auto_free(main)
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	var menu: Node = main.get_node("RuntimeUi/MainMenu")
	assert_object(nav).is_not_null()
	assert_object(menu).is_not_null()
	nav.set("UseFadeTransition", false)

	if menu.has_method("HideMenu"):
		menu.call("HideMenu")
		await _await_frames(1)
		assert_bool(bool(menu.get("visible"))).is_false()

	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)

	var screen_root: Node = main.get_node("RuntimeUi/ScreenRoot")
	var screen: Control = screen_root.get_node("BattleMapScreen")
	var back_btn: Button = screen.get_node("Margin/VBox/Controls/BackBtn")
	back_btn.emit_signal("pressed")
	await _await_frames(2)

	assert_object(screen_root.get_node_or_null("BattleMapScreen")).is_null()
	assert_bool(bool(menu.get("visible"))).is_true()


# ACC:T57.10
# ACC:T59.9
func test_back_action_without_main_navigator_should_not_mutate_runtime_summary() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var before := _bridge_summary_metrics(bridge)
	var before_children := bridge.get_node("Battlefield").get_child_count()
	var status_before := String((screen.get_node("Margin/VBox/Status") as Label).text)
	var summary_before := String((screen.get_node("Margin/VBox/Summary") as Label).text)

	var back_btn: Button = screen.get_node("Margin/VBox/Controls/BackBtn")
	back_btn.emit_signal("pressed")
	await _await_frames(2)

	var after := _bridge_summary_metrics(bridge)
	assert_int(int(after.get("friendly_units_deployed", -1))).is_equal(int(before.get("friendly_units_deployed", -1)))
	assert_int(int(after.get("enemy_units_spawned", -1))).is_equal(int(before.get("enemy_units_spawned", -1)))
	assert_int(int(after.get("combat_exchanges", -1))).is_equal(int(before.get("combat_exchanges", -1)))
	assert_int(int(after.get("dead_units_retired", -1))).is_equal(int(before.get("dead_units_retired", -1)))
	assert_int(bridge.get_node("Battlefield").get_child_count()).is_equal(before_children)
	assert_str(String((screen.get_node("Margin/VBox/Status") as Label).text)).is_equal(status_before)
	assert_str(String((screen.get_node("Margin/VBox/Summary") as Label).text)).is_equal(summary_before)


# ACC:T55.3
# ACC:T55.7
# ACC:T55.10
# ACC:T56.8
# ACC:T57.7
# ACC:T58.6
# ACC:T59.10
# ACC:T62.3
func test_battle_map_terminal_summary_should_stay_stable_without_state_change() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var build_btn: Button = screen.get_node("Margin/VBox/Controls/BuildBtn")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	var snapshot_before := _status_and_summary(screen)
	assert_bool(String(snapshot_before["status"]).length() > 0).is_true()
	assert_bool(String(snapshot_before["summary"]).length() > 0).is_true()

	await _await_frames(5)
	var snapshot_after := _status_and_summary(screen)
	assert_that(snapshot_after).is_equal(snapshot_before)


# ACC:T62.1
# ACC:T62.5
# ACC:T62.6
# ACC:T62.7
# ACC:T62.8
# ACC:T62.9
func test_spawn_side_glow_and_wave_pulse_decay_back_to_weak_state() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var status_label: Label = screen.get_node("Margin/VBox/Status")

	var before := _spawn_cue_colors(screen)
	assert_bool(before[0].a < 0.7 and before[1].a < 0.7).is_true()
	var status_before := String(status_label.text)
	var summary_before := _bridge_summary_metrics(bridge)
	assert_int(int(summary_before.get("enemy_units_spawned", 0))).is_equal(0)

	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	wave_btn.emit_signal("pressed")
	await _await_frames(1)
	var pulse := _spawn_cue_colors(screen)
	var status_after_wave := String(status_label.text)
	var summary_after_wave := _bridge_summary_metrics(bridge)
	assert_bool(pulse[0].a > before[0].a and pulse[1].a > before[1].a).is_true()
	assert_bool(pulse[0].a >= 0.9 and pulse[1].a >= 0.9).is_true()
	assert_bool(status_after_wave != status_before).is_true()
	assert_int(int(summary_after_wave.get("enemy_units_spawned", 0))).is_equal(2)

	await get_tree().create_timer(4.5).timeout
	var after := _spawn_cue_colors(screen)
	var status_after_decay := String(status_label.text)
	assert_bool(after[0].a <= before[0].a + 0.05 and after[1].a <= before[1].a + 0.05).is_true()
	# ACC:T63.7 / ACC:T63.9: transient cue decays without mutating runtime progression counters.
	assert_str(status_after_decay).is_equal(status_after_wave)
	assert_that(_bridge_summary_metrics(bridge)).is_equal(summary_after_wave)

# ACC:T62.4
func test_battle_map_cycle_should_keep_hud_singleton_and_navigator_ownership() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	add_child(auto_free(main))
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	assert_object(nav).is_not_null()
	nav.set("UseFadeTransition", false)
	assert_int(_hud_count(main)).is_equal(1)

	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(1)
	assert_object(main.get_node_or_null("RuntimeUi/ScreenRoot/BattleMapScreen")).is_not_null()
	assert_int(_hud_count(main)).is_equal(1)

	nav.call("ClearCurrentScreen")
	await _await_frames(1)
	assert_object(main.get_node_or_null("RuntimeUi/ScreenRoot/BattleMapScreen")).is_null()
	assert_int(_hud_count(main)).is_equal(1)
	assert_object(main.get_node_or_null("ScreenNavigator")).is_not_null()
	var reopen_ok: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(reopen_ok).is_true()
	await _await_frames(1)
	var reopened_screen: Control = main.get_node("RuntimeUi/ScreenRoot/BattleMapScreen")
	assert_str(str(reopened_screen.get_node("Background").get_meta("ownership_container"))).is_equal("battlefield_presentation")
	assert_str(str(reopened_screen.get_node("CombatExperienceRuntimeBridge").get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(reopened_screen.get_node("WaveTimer").get_meta("ownership_container"))).is_equal("runtime_bridge")
	assert_str(str(reopened_screen.get_node("Margin").get_meta("ownership_container"))).is_equal("legacy_prototype")


# ACC:T62.10
func test_path_readability_stays_behavior_driven_without_arrow_or_route_ui() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var background: Control = screen.get_node("Background")
	var path: Line2D = screen.get_node("Background/Path")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var initial_positions := {
		"EnemySpawnA": (background.get_node("EnemySpawnA") as Control).global_position,
		"EnemySpawnB": (background.get_node("EnemySpawnB") as Control).global_position,
	}

	assert_bool(path.visible).is_true()
	assert_int(background.get_children().filter(func(n): return str((n as Node).name).find("Arrow") >= 0 or str((n as Node).name).find("Route") >= 0).size()).is_equal(0)

	if bridge.has_method("SpawnEnemyWavePhase"):
		bridge.call("SpawnEnemyWavePhase")
	await _await_frames(1)
	if bridge.has_method("AdvanceSimulation"):
		for _i in range(10):
			bridge.call("AdvanceSimulation", 0.2)
	await _await_frames(1)

	var snapshots: Array = bridge.call("GetActorSnapshots") if bridge.has_method("GetActorSnapshots") else []
	var found_progress := false
	for item in snapshots:
		var snapshot := item as Dictionary
		if snapshot != null and bool(snapshot.get("is_moving_enemy", false)) and float(snapshot.get("path_progress", 0.0)) > 0.0:
			found_progress = true
	assert_bool(found_progress).is_true()
	assert_that((background.get_node("EnemySpawnA") as Control).global_position).is_equal(initial_positions["EnemySpawnA"])
	assert_that((background.get_node("EnemySpawnB") as Control).global_position).is_equal(initial_positions["EnemySpawnB"])


# ACC:T57.9
func test_bridge_unavailable_should_keep_coordinator_path_recoverable_without_counter_drift() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var baseline_summary := _bridge_summary_metrics(bridge)
	var baseline_children := bridge.get_node("Battlefield").get_child_count()
	var bridge_stub := Node.new()
	bridge_stub.name = "BridgeStubNoMethods"
	screen.add_child(auto_free(bridge_stub))
	screen.set("_bridge", bridge_stub)

	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	var after_summary := _bridge_summary_metrics(bridge)
	assert_that(after_summary).is_equal(baseline_summary)
	assert_int(bridge.get_node("Battlefield").get_child_count()).is_equal(baseline_children)
	assert_bool(is_instance_valid(screen)).is_true()
	assert_bool(String((screen.get_node("Margin/VBox/Status") as Label).text).length() > 0).is_true()
	assert_bool(String((screen.get_node("Margin/VBox/Summary") as Label).text).length() > 0).is_true()


# ACC:T66.1
# ACC:T66.6
func test_legacy_labels_should_not_be_authoritative_source_for_runtime_feedback() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var status_label: Label = screen.get_node("Margin/VBox/Status")
	var summary_label: Label = screen.get_node("Margin/VBox/Summary")
	var legend_label: Label = screen.get_node("Margin/VBox/Legend")
	var metrics_help_label: Label = screen.get_node("Margin/VBox/MetricsHelp")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")

	var baseline_summary := _bridge_summary_metrics(bridge)
	var baseline_status_text := String(status_label.text)

	# Negative path: tampering legacy text must not mutate runtime counters/state.
	summary_label.text = "LEGACY_OVERRIDE_SUMMARY"
	legend_label.text = "LEGACY_OVERRIDE_LEGEND"
	metrics_help_label.text = "LEGACY_OVERRIDE_METRICS_HELP"
	await _await_frames(1)

	var after_legacy_override := _bridge_summary_metrics(bridge)
	assert_that(after_legacy_override).is_equal(baseline_summary)
	assert_str(String(status_label.text)).is_equal(baseline_status_text)

	# Positive path: runtime progression should still be driven by bridge flow, not legacy labels.
	wave_btn.emit_signal("pressed")
	await _await_frames(1)
	var after_wave := _bridge_summary_metrics(bridge)
	assert_int(int(after_wave.get("enemy_units_spawned", 0))).is_greater_equal(int(baseline_summary.get("enemy_units_spawned", 0)) + 2)
	assert_bool(String(status_label.text).to_lower().find("wave") >= 0).is_true()

	exchange_btn.emit_signal("pressed")
	await _await_frames(1)
	cleanup_btn.emit_signal("pressed")
	await _await_frames(1)
	finish_btn.emit_signal("pressed")
	await _await_frames(1)
	var terminal_summary := _bridge_summary_metrics(bridge)
	assert_int(int(terminal_summary.get("combat_exchanges", 0))).is_greater_equal(1)
	assert_bool(String(status_label.text).to_lower().find("finished") >= 0).is_true()

func test_combat_bridge_single_source_updates_actor_snapshots_and_castle_hp() -> void:
	var bridge := preload("res://Game.Godot/Scripts/Combat/CombatExperienceRuntimeBridge.cs").new()
	add_child(auto_free(bridge))
	await _await_frames(2)

	if bridge.has_method("ResetForInteractiveRun"):
		bridge.call("ResetForInteractiveRun")
		bridge.call("BuildPhase")
		bridge.call("TrainFriendlyUnitPhase")
		bridge.call("SpawnEnemyWavePhase")
	else:
		bridge.call("RunCompleteCombatExperienceForTest")

	var summary_before: Dictionary = {}
	if bridge.has_method("GetSummary"):
		summary_before = bridge.call("GetSummary")
	else:
		summary_before = bridge.call("RunCompleteCombatExperienceForTest")
	var before_hp := int(summary_before.get("castle_hp", -1))
	assert_int(before_hp).is_greater_equal(0)
	var required_summary_keys := [
		"friendly_units_deployed",
		"enemy_units_spawned",
		"combat_exchanges",
		"dead_units_retired",
		"active_combat_nodes_after_cleanup",
		"castle_hp",
	]
	for key_variant in required_summary_keys:
		var key := str(key_variant)
		assert_bool(summary_before.has(key)).is_true()

	if bridge.has_method("AdvanceSimulation"):
		for _i in range(120):
			bridge.call("AdvanceSimulation", 0.1)

	if bridge.has_method("GetActorSnapshots"):
		var snapshots: Array = bridge.call("GetActorSnapshots")
		assert_int(snapshots.size()).is_greater_equal(1)

	var summary_after: Dictionary = {}
	if bridge.has_method("GetSummary"):
		summary_after = bridge.call("GetSummary")
	else:
		summary_after = bridge.call("RunCompleteCombatExperienceForTest")
	var after_hp := int(summary_after.get("castle_hp", -1))
	assert_int(after_hp).is_less_equal(before_hp)
	for key_variant in required_summary_keys:
		var key := str(key_variant)
		assert_bool(summary_after.has(key)).is_true()


# ACC:T58.7
func test_locale_switch_between_en_us_and_zh_cn_should_keep_player_visible_status_resolvable() -> void:
	var original_locale := str(TranslationServer.get_locale())

	TranslationServer.set_locale("en-US")
	var screen_en := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen_en))
	await _await_frames(2)
	var status_en := String((screen_en.get_node("Margin/VBox/Status") as Label).text)
	var summary_en := String((screen_en.get_node("Margin/VBox/Summary") as Label).text)

	TranslationServer.set_locale("zh-CN")
	var screen_zh := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen_zh))
	await _await_frames(2)
	var status_zh := String((screen_zh.get_node("Margin/VBox/Status") as Label).text)
	var summary_zh := String((screen_zh.get_node("Margin/VBox/Summary") as Label).text)

	# Locale switch must keep status/summary readable for players in both locales.
	assert_bool(status_en.length() > 0).is_true()
	assert_bool(summary_en.length() > 0).is_true()
	assert_bool(status_zh.length() > 0).is_true()
	assert_bool(summary_zh.length() > 0).is_true()

	TranslationServer.set_locale(original_locale)


