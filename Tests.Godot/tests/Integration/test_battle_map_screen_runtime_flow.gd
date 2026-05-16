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


func _local_feedback_state(screen: Node) -> Dictionary:
	var prompt_panel: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel")
	var prompt_label: Label = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel")
	var hit_flash: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/HitFlashOverlay")
	var wall_pressure: Control = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/WallPressureOverlay")
	return {
		"prompt_visible": prompt_panel.visible,
		"prompt_text": String(prompt_label.text),
		"hit_flash_visible": hit_flash.visible,
		"wall_pressure_visible": wall_pressure.visible,
	}


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
	var background: Control = screen.get_node("Background/BattlefieldViewport")
	var main := _resolve_main_root(screen)
	var top_band: Control = main.get_node("RuntimeUi/HUD/TopBar") if main != null else screen.get_node("BattleHud/TopBar")
	var bottom_band: Control = main.get_node("RuntimeUi/HUD/CombatHud/BottomBar") if main != null else screen.get_node("BattleHud/CombatHud/BottomBar")
	return {
		"background_pos": background.global_position,
		"background_size": background.size,
		"title_pos": top_band.global_position,
		"title_size": top_band.size,
		"status_pos": bottom_band.global_position,
		"status_size": bottom_band.size,
	}


func _battlemap_player_bands(screen: Control) -> Dictionary:
	var main := _resolve_main_root(screen)
	if main != null:
		return {
			"top": main.get_node("RuntimeUi/HUD/TopBar"),
			"middle": screen.get_node("Background/BattlefieldViewport"),
			"bottom": main.get_node("RuntimeUi/HUD/CombatHud/BottomBar"),
		}
	return {
		"top": screen.get_node("BattleHud/TopBar"),
		"middle": screen.get_node("Background/BattlefieldViewport"),
		"bottom": screen.get_node("BattleHud/CombatHud/BottomBar"),
	}


func _resolve_main_root(node: Node) -> Node:
	var current: Node = node
	while current != null:
		if String(current.name) == "Main":
			return current
		current = current.get_parent()
	return null


func _spawn_cue_colors(screen: Control) -> Array[Color]:
	var spawn_a: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA")
	var spawn_b: ColorRect = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB")
	return [spawn_a.color, spawn_b.color]


const _T59_SCREEN_BASELINE := Vector2(1600.0, 900.0)
const _T59_BATTLEFIELD_SIZE := Vector2(1584.0, 624.0)
const _T59_SLOT_SIZE := 48.0
const _T59_TOP_HUD_HEIGHT := 80.0
const _T59_BOTTOM_HUD_HEIGHT := 196.0
const _T59_SIDE_GUTTER := 8.0


const _T59_REGION_ORDER: PackedStringArray = [
	"LeftOuterField",
	"LeftWall",
	"InnerCastleRegion",
	"RightWall",
	"RightOuterField",
]

const _T59_REGION_WIDTHS := {
	"LeftOuterField": 576.0,
	"LeftWall": 48.0,
	"InnerCastleRegion": 336.0,
	"RightWall": 48.0,
	"RightOuterField": 576.0,
}

const _T59_SLOT_TOTALS := {
	"LeftOuterSlots": 156,
	"InnerCastleSlots": 91,
	"RightOuterSlots": 156,
}


const _TASK56_OWNERSHIP_CONTAINERS: PackedStringArray = [
	"battlefield_presentation",
	"runtime_bridge",
	"legacy_prototype",
]

const _TASK56_NODE_OWNERSHIP_MAP := {
	"Background": "battlefield_presentation",
	"Background/BattlefieldViewport": "battlefield_presentation",
	"Background/BattlefieldViewport/BattlefieldRoot": "battlefield_presentation",
	"Background/BattlefieldViewport/BattlefieldRoot/MapBaseLayer": "battlefield_presentation",
	"Background/BattlefieldViewport/BattlefieldRoot/BoundaryLayer": "battlefield_presentation",
	"Background/BattlefieldViewport/BattlefieldRoot/SlotOverlayLayer": "battlefield_presentation",
	"Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer": "battlefield_presentation",
	"Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/Path": "battlefield_presentation",
	"Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA": "battlefield_presentation",
	"Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB": "battlefield_presentation",
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


func _battlefield_viewport(screen: Control) -> Control:
	return screen.get_node_or_null("Background/BattlefieldViewport") as Control


func _battlefield_root(screen: Control) -> Control:
	return screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot") as Control


func _battlefield_slot_layer(screen: Control) -> Node:
	return screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/SlotOverlayLayer")


func _battlefield_slot(screen: Control, slot_root: String, slot_name: String) -> ColorRect:
	return screen.get_node_or_null(
		"Background/BattlefieldViewport/BattlefieldRoot/SlotOverlayLayer/%s/%s" % [slot_root, slot_name]
	) as ColorRect


func _runtime_slot_overlay_snapshot(slot: ColorRect) -> Dictionary:
	return {
		"color": slot.color,
		"overlay_state": str(slot.get_meta("overlay_state", "overlay_hidden")),
		"overlay_tint": str(slot.get_meta("overlay_tint", "none")),
		"marker": str(slot.get_meta("marker", "none")),
		"frame": str(slot.get_meta("frame", "none")),
		"reason_text": str(slot.get_meta("reason_text", "")),
	}


func _assert_t59_slot_grid(slot_root: Control, expected_count: int) -> void:
	assert_object(slot_root).is_not_null()
	assert_int(slot_root.get_child_count()).is_equal(expected_count)
	for child_variant in slot_root.get_children():
		var slot := child_variant as Control
		assert_object(slot).is_not_null()
		assert_that(slot.size).is_equal(Vector2(_T59_SLOT_SIZE, _T59_SLOT_SIZE))
		assert_float(fmod(slot.position.x, _T59_SLOT_SIZE)).is_equal(0.0)
		assert_float(fmod(slot.position.y, _T59_SLOT_SIZE)).is_equal(0.0)
		assert_bool(bool(slot.get_meta("buildable", false))).is_true()
		assert_bool(bool(slot.get_meta("slot_available", false))).is_true()
		assert_bool(slot.position.x >= 0.0).is_true()
		assert_bool(slot.position.y >= 0.0).is_true()
		assert_bool(slot.position.x + slot.size.x <= slot_root.size.x).is_true()
		assert_bool(slot.position.y + slot.size.y <= slot_root.size.y).is_true()


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

	var background: Control = screen.get_node("Background/BattlefieldViewport")
	var title: Control = screen.get_node("Margin/VBox/Title")
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	var title_before := title.global_position
	var metrics_before := metrics_help.global_position
	var background_before := background.global_position

	background.position = background.position + Vector2(-120.0, 0.0)
	await _await_frames(1)

	assert_float(background.global_position.x).is_equal(background_before.x - 120.0)
	assert_that(title.global_position).is_equal(title_before)
	assert_bool(metrics_help.visible).is_false()
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
	assert_bool(metrics_help.visible).is_false()
	assert_that(metrics_help.global_position).is_equal(metrics_before)
	assert_str(String(status_label.text)).is_equal(status_before)
	assert_str(String(summary_label.text)).is_equal(summary_before)


# ACC:T55.1
# ACC:T56.3
# ACC:T58.3
# ACC:T59.3
# ACC:T66.3
func test_1600x900_frame_keeps_three_player_visible_bands_simultaneously_visible() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	screen.size = _T59_SCREEN_BASELINE
	await _await_frames(2)

	var viewport := _T59_SCREEN_BASELINE
	var bands := _battlemap_player_bands(screen)
	var background: Control = bands["middle"]
	var title: Control = bands["top"]
	var status: Control = bands["bottom"]
	var metrics_help: Control = screen.get_node("Margin/VBox/MetricsHelp")
	var _path: Line2D = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/Path")
	var background_top_before := background.global_position.y
	var background_height_before := background.size.y
	var title_top_before := title.global_position.y
	var title_height_before := title.size.y
	var status_top_before := status.global_position.y
	var status_height_before := status.size.y

	assert_bool(title.visible).is_true()
	assert_bool(_is_visible_inside_viewport(background, viewport)).is_true()
	assert_bool(status.visible).is_true()
	assert_bool(metrics_help.visible).is_false()
	assert_that(background.size).is_equal(_T59_BATTLEFIELD_SIZE)

	var battlefield_top := background.global_position.y
	var battlefield_bottom := battlefield_top + background.size.y
	assert_float(background.global_position.x).is_equal(_T59_SIDE_GUTTER)
	assert_float(battlefield_top).is_equal(_T59_TOP_HUD_HEIGHT)
	assert_float(battlefield_bottom).is_equal(_T59_TOP_HUD_HEIGHT + _T59_BATTLEFIELD_SIZE.y)
	assert_float(title.global_position.y).is_equal(0.0)
	assert_float(title.size.y).is_equal(_T59_TOP_HUD_HEIGHT)
	assert_float(status.global_position.y).is_equal(_T59_TOP_HUD_HEIGHT + _T59_BATTLEFIELD_SIZE.y)
	assert_float(status.size.y).is_equal(_T59_BOTTOM_HUD_HEIGHT)
	assert_bool(title.global_position.y < battlefield_bottom).is_true()
	assert_bool(status.global_position.y > title.global_position.y).is_true()

	# Fixed 1600x900 formal frame: refreshing the same baseline should keep all three bands stable.
	screen.size = _T59_SCREEN_BASELINE
	await _await_frames(2)
	assert_float(background.global_position.y).is_equal(background_top_before)
	assert_float(background.size.y).is_equal(background_height_before)
	assert_float(title.global_position.y).is_equal(title_top_before)
	assert_float(title.size.y).is_equal(title_height_before)
	assert_float(status.global_position.y).is_equal(status_top_before)
	assert_float(status.size.y).is_equal(status_height_before)


# ACC:T55.1
# ACC:T56.4
# ACC:T58.4
# ACC:T59.4
# ACC:T66.4
func test_reenter_battle_map_keeps_three_band_frame_stable_after_viewport_resize() -> void:
	var main := preload("res://Game.Godot/Scenes/Main.tscn").instantiate()
	get_tree().root.add_child(auto_free(main))
	await _await_frames(2)
	main.size = _T59_SCREEN_BASELINE
	await _await_frames(2)

	var nav: Node = main.get_node("ScreenNavigator")
	nav.set("UseFadeTransition", false)
	var screen_root: Node = main.get_node("RuntimeUi/ScreenRoot")

	var ok_enter: bool = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)
	var first_screen: Control = screen_root.get_node("BattleMapScreen")
	first_screen.size = _T59_SCREEN_BASELINE
	await _await_frames(2)
	var first_snapshot := _frame_snapshot(first_screen)

	nav.call("ClearCurrentScreen")
	await _await_frames(2)
	ok_enter = nav.call("SwitchTo", "res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
	assert_bool(ok_enter).is_true()
	await _await_frames(2)
	var second_screen: Control = screen_root.get_node("BattleMapScreen")
	second_screen.size = _T59_SCREEN_BASELINE
	await _await_frames(2)
	var second_snapshot := _frame_snapshot(second_screen)
	var second_bands := _battlemap_player_bands(second_screen)
	var second_background: Control = second_bands["middle"]
	var second_title: Control = second_bands["top"]
	var second_status: Control = second_bands["bottom"]
	var second_metrics_help: Control = second_screen.get_node("Margin/VBox/MetricsHelp")

	assert_bool(second_title.visible).is_true()
	assert_bool(second_status.visible).is_true()
	assert_bool(second_metrics_help.visible).is_false()
	assert_that(second_background.size).is_equal(_T59_BATTLEFIELD_SIZE)

	# Re-entry under the fixed 1600x900 formal frame should restore the same three-band snapshot.
	second_screen.size = _T59_SCREEN_BASELINE
	await _await_frames(2)
	assert_that(second_snapshot).is_equal(first_snapshot)
	var title_x_before := second_title.global_position.x
	var status_x_before := second_status.global_position.x
	var battlefield_x_before := second_background.global_position.x

	second_background.position = second_background.position + Vector2(-80.0, 0.0)
	await _await_frames(1)

	assert_float(second_background.global_position.x).is_equal(battlefield_x_before - 80.0)
	assert_float(second_title.global_position.x).is_equal(title_x_before)
	assert_float(second_status.global_position.x).is_equal(status_x_before)


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
	var summary: Label = screen.get_node("Margin/VBox/Summary")
	var status: Label = screen.get_node("Margin/VBox/Status")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")

	build_btn.emit_signal("pressed")
	wave_btn.emit_signal("pressed")
	exchange_btn.emit_signal("pressed")
	cleanup_btn.emit_signal("pressed")
	finish_btn.emit_signal("pressed")
	await _await_frames(2)

	assert_bool(String(status.text).length() > 0).is_true()
	assert_bool(summary.visible).is_false()
	var runtime_metrics := _bridge_summary_metrics(bridge)
	assert_int(int(runtime_metrics.get("friendly_units_deployed", 0))).is_greater_equal(1)
	assert_int(int(runtime_metrics.get("enemy_units_spawned", 0))).is_greater_equal(2)
	assert_int(int(runtime_metrics.get("combat_exchanges", 0))).is_greater_equal(1)
	var feedback_state := _local_feedback_state(screen)
	assert_bool(bool(feedback_state["prompt_visible"])).is_true()
	assert_bool(String(feedback_state["prompt_text"]).length() > 0).is_true()
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
	assert_bool(status_after_exchange.find("wave") >= 0 or status_after_exchange.find("娉㈡") >= 0).is_true()

	cleanup_btn.emit_signal("pressed")
	await _await_frames(1)
	var status_after_cleanup := String(status_label.text)
	var status_after_cleanup_lc := status_after_cleanup.to_lower()
	assert_bool(
		status_after_cleanup_lc.find("exchange") >= 0
		or status_after_cleanup_lc.find("combat") >= 0
		or status_after_cleanup.find("浜ゆ垬") >= 0
	).is_true()

	finish_btn.emit_signal("pressed")
	await _await_frames(1)
	var status_after_finish := String(status_label.text)
	assert_bool(status_after_finish.to_lower().find("cleanup") >= 0 or status_after_finish.find("娓呯悊") >= 0).is_true()

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
	assert_bool(status_after_valid_flow.find("finished") >= 0 or status_after_valid_flow.find("缁撴潫") >= 0).is_true()
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
func test_battle_map_runtime_state_should_distinguish_empty_progressed_and_completion_states() -> void:
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
	var empty_feedback := _local_feedback_state(screen)
	assert_bool(empty_status.find("loaded") >= 0 or empty_status.find("鍔犺浇") >= 0).is_true()
	assert_bool(empty_status.find("finished") < 0 and empty_status.find("缁撴潫") < 0).is_true()
	assert_bool(empty_status.length() > 0).is_true()
	assert_bool(empty_summary.length() > 0).is_true()
	assert_bool(bool(empty_feedback["prompt_visible"])).is_false()
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
	assert_bool(failure_status_lc.find("cleanup") >= 0 or failure_status.find("娓呯悊") >= 0).is_true()
	assert_bool(failure_status_lc.find("finished") < 0 and failure_status.find("缁撴潫") < 0).is_true()
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
	var progressed_feedback := _local_feedback_state(screen)
	assert_int(int(progressed_metrics["enemy_units_spawned"])).is_greater_equal(2)
	assert_int(int(progressed_metrics["castle_hp"])).is_less_equal(int(empty_metrics["castle_hp"]))
	assert_int(int(progressed_metrics["friendly_units_deployed"])).is_equal(0)
	assert_bool(progressed_status != empty_status).is_true()
	assert_bool(progressed_status != failure_status).is_true()
	assert_str(progressed_summary).is_equal(empty_summary)
	assert_bool(
		bool(progressed_feedback["prompt_visible"])
		or bool(progressed_feedback["hit_flash_visible"])
		or bool(progressed_feedback["wall_pressure_visible"])
	).is_true()
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
	var completion_feedback := _local_feedback_state(screen)
	assert_int(int(completion_metrics["friendly_units_deployed"])).is_greater_equal(1)
	assert_int(int(completion_metrics["enemy_units_spawned"])).is_greater_equal(2)
	assert_int(int(completion_metrics["combat_exchanges"])).is_greater_equal(1)
	assert_bool(completion_status.find("finished") >= 0 or completion_status.find("缁撴潫") >= 0).is_true()
	assert_bool(completion_status.find("cleanup") < 0 and completion_status.find("娓呯悊") < 0).is_true()
	assert_bool(completion_status != progressed_status).is_true()
	assert_bool(completion_status != failure_status).is_true()
	assert_str(completion_summary).is_equal(progressed_summary)
	assert_bool(bool(completion_feedback["prompt_visible"])).is_true()
	assert_bool(String(completion_feedback["prompt_text"]).length() > 0).is_true()
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
	var before_metrics := _bridge_summary_metrics(bridge)
	var before_children := bridge.get_node("Battlefield").get_child_count()
	var status_before := String((screen.get_node("Margin/VBox/Status") as Label).text)
	var summary_before := String((screen.get_node("Margin/VBox/Summary") as Label).text)

	var back_btn: Button = screen.get_node("Margin/VBox/Controls/BackBtn")
	back_btn.emit_signal("pressed")
	await _await_frames(2)

	var after_metrics := _bridge_summary_metrics(bridge)
	assert_int(int(after_metrics.get("friendly_units_deployed", -1))).is_equal(int(before_metrics.get("friendly_units_deployed", -1)))
	assert_int(int(after_metrics.get("enemy_units_spawned", -1))).is_equal(int(before_metrics.get("enemy_units_spawned", -1)))
	assert_int(int(after_metrics.get("combat_exchanges", -1))).is_equal(int(before_metrics.get("combat_exchanges", -1)))
	assert_int(int(after_metrics.get("dead_units_retired", -1))).is_equal(int(before_metrics.get("dead_units_retired", -1)))
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


# ACC:T59.1
func test_battlefield_layout_should_define_five_regions_in_gdd_order() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var viewport := _battlefield_viewport(screen)
	assert_object(viewport).is_not_null()
	assert_that(viewport.size).is_equal(_T59_BATTLEFIELD_SIZE)

	var root := _battlefield_root(screen)
	assert_object(root).is_not_null()
	var map_base := root.get_node_or_null("MapBaseLayer")
	assert_object(map_base).is_not_null()

	var region_names: Array[String] = []
	for child_variant in map_base.get_children():
		var child := child_variant as Node
		if child != null:
			region_names.append(String(child.name))
	assert_that(PackedStringArray(region_names)).is_equal(_T59_REGION_ORDER)

	var expected_x := 0.0
	for region_name in _T59_REGION_ORDER:
		var region := map_base.get_node_or_null(region_name) as Control
		assert_object(region).is_not_null()
		assert_float(region.position.x).is_equal(expected_x)
		assert_float(region.position.y).is_equal(0.0)
		assert_float(region.size.x).is_equal(float(_T59_REGION_WIDTHS[region_name]))
		assert_float(region.size.y).is_equal(_T59_BATTLEFIELD_SIZE.y)
		expected_x += float(_T59_REGION_WIDTHS[region_name])
	assert_float(expected_x).is_equal(_T59_BATTLEFIELD_SIZE.x)


# ACC:T59.2
func test_battlefield_layout_should_keep_walls_non_buildable_and_slots_within_regions() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var root := _battlefield_root(screen)
	assert_object(root).is_not_null()
	var map_base := root.get_node("MapBaseLayer")
	var slot_layer := _battlefield_slot_layer(screen)
	assert_object(slot_layer).is_not_null()

	for wall_name in ["LeftWall", "RightWall"]:
		var wall := map_base.get_node_or_null(wall_name) as Control
		assert_object(wall).is_not_null()
		assert_bool(bool(wall.get_meta("buildable", true))).is_false()
		assert_object(slot_layer.get_node_or_null("%sSlots" % wall_name)).is_null()

	for slot_root_name_variant in _T59_SLOT_TOTALS.keys():
		var slot_root_name := String(slot_root_name_variant)
		var slot_root := slot_layer.get_node_or_null(slot_root_name) as Control
		_assert_t59_slot_grid(slot_root, int(_T59_SLOT_TOTALS[slot_root_name]))


# ACC:T59.3
func test_battlefield_layout_should_visually_distinguish_buildable_and_non_buildable_regions() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var root := _battlefield_root(screen)
	assert_object(root).is_not_null()
	var map_base := root.get_node("MapBaseLayer")
	var slot_layer := _battlefield_slot_layer(screen)
	assert_object(slot_layer).is_not_null()

	var left_outer := map_base.get_node("LeftOuterField") as ColorRect
	var inner_castle := map_base.get_node("InnerCastleRegion") as ColorRect
	var left_wall := map_base.get_node("LeftWall") as ColorRect
	var right_wall := map_base.get_node("RightWall") as ColorRect
	assert_bool(left_outer.color != left_wall.color).is_true()
	assert_bool(inner_castle.color != left_wall.color).is_true()
	assert_bool(left_wall.color == right_wall.color).is_true()

	for slot_root_name_variant in _T59_SLOT_TOTALS.keys():
		var slot_root_name := String(slot_root_name_variant)
		var slot_root := slot_layer.get_node(slot_root_name) as Control
		assert_bool(bool(slot_root.get_meta("buildable_region", false))).is_true()
		assert_bool(slot_root.modulate.a > 0.0).is_true()


# ACC:T60.1
# ACC:T60.2
func test_placement_overlay_should_drive_runtime_slot_visuals_on_battlefield_scene() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var selection_controller: Node = screen.get_node("SelectionController")
	var inner_slot := _battlefield_slot(screen, "InnerCastleSlots", "InnerCastleRegionSlot_00_00")
	var outer_slot := _battlefield_slot(screen, "LeftOuterSlots", "LeftOuterFieldSlot_00_00")
	var temp_slot := _battlefield_slot(screen, "RightOuterSlots", "RightOuterFieldSlot_00_00")
	var wall_slot := _battlefield_slot(screen, "LeftOuterSlots", "LeftOuterFieldSlot_01_00")
	assert_object(inner_slot).is_not_null()
	assert_object(outer_slot).is_not_null()
	assert_object(temp_slot).is_not_null()
	assert_object(wall_slot).is_not_null()

	selection_controller.call("apply_legality_overlay", {
		"InnerCastleRegionSlot_00_00": "valid_inner",
		"LeftOuterFieldSlot_00_00": "valid_outer",
		"RightOuterFieldSlot_00_00": "temp_invalid",
		"LeftOuterFieldSlot_01_00": "wall",
	})
	await _await_frames(1)

	var inner := _runtime_slot_overlay_snapshot(inner_slot)
	var outer := _runtime_slot_overlay_snapshot(outer_slot)
	var temp := _runtime_slot_overlay_snapshot(temp_slot)
	var wall := _runtime_slot_overlay_snapshot(wall_slot)

	assert_str(str(inner["overlay_state"])).is_equal("overlay_legal")
	assert_str(str(inner["overlay_tint"])).is_equal("warm")
	assert_str(str(outer["overlay_state"])).is_equal("overlay_legal")
	assert_str(str(outer["overlay_tint"])).is_equal("cool")
	assert_str(str(temp["overlay_state"])).is_equal("overlay_illegal")
	assert_str(str(temp["frame"])).is_equal("red")
	assert_str(str(temp["reason_text"])).is_equal("")
	assert_str(str(wall["overlay_state"])).is_equal("overlay_illegal")
	assert_str(str(wall["overlay_tint"])).is_equal("red")
	assert_str(str(wall["marker"])).is_equal("blocker")


# ACC:T60.3
func test_inactive_placement_context_should_clear_runtime_slot_overlay_from_battlefield_scene() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var selection_controller: Node = screen.get_node("SelectionController")
	var slot := _battlefield_slot(screen, "InnerCastleSlots", "InnerCastleRegionSlot_00_01")
	assert_object(slot).is_not_null()

	selection_controller.call("apply_legality_overlay", {
		"InnerCastleRegionSlot_00_01": "valid_inner",
	})
	await _await_frames(1)
	var before_snapshot := _runtime_slot_overlay_snapshot(slot)

	selection_controller.call("set_placement_context_active", false)
	await _await_frames(1)
	var after_snapshot := _runtime_slot_overlay_snapshot(slot)

	assert_str(str(before_snapshot["overlay_state"])).is_equal("overlay_legal")
	assert_str(str(after_snapshot["overlay_state"])).is_equal("overlay_hidden")
	assert_str(str(after_snapshot["overlay_tint"])).is_equal("none")


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

	var before_pulse := _spawn_cue_colors(screen)
	assert_bool(before_pulse[0].a < 0.7 and before_pulse[1].a < 0.7).is_true()
	var status_before := String(status_label.text)
	var summary_before := _bridge_summary_metrics(bridge)
	assert_int(int(summary_before.get("enemy_units_spawned", 0))).is_equal(0)

	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	wave_btn.emit_signal("pressed")
	await _await_frames(1)
	var pulse := _spawn_cue_colors(screen)
	var status_after_wave := String(status_label.text)
	var summary_after_wave := _bridge_summary_metrics(bridge)
	assert_bool(pulse[0].a > before_pulse[0].a and pulse[1].a > before_pulse[1].a).is_true()
	assert_bool(pulse[0].a >= 0.9 and pulse[1].a >= 0.9).is_true()
	assert_bool(status_after_wave != status_before).is_true()
	assert_int(int(summary_after_wave.get("enemy_units_spawned", 0))).is_equal(2)

	await get_tree().create_timer(2.0).timeout
	var pulse_midway := _spawn_cue_colors(screen)
	assert_bool(pulse_midway[0].a >= 0.9 and pulse_midway[1].a >= 0.9).is_true()

	await get_tree().create_timer(4.5).timeout
	var after_pulse := _spawn_cue_colors(screen)
	var status_after_decay := String(status_label.text)
	assert_bool(after_pulse[0].a <= before_pulse[0].a + 0.05 and after_pulse[1].a <= before_pulse[1].a + 0.05).is_true()
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

	var background: Control = screen.get_node("Background/BattlefieldViewport")
	var path: Line2D = screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/Path")
	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var initial_positions := {
		"EnemySpawnA": (screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA") as Control).global_position,
		"EnemySpawnB": (screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB") as Control).global_position,
	}

	assert_bool(path.visible).is_false()
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
	assert_that((screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA") as Control).global_position).is_equal(initial_positions["EnemySpawnA"])
	assert_that((screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB") as Control).global_position).is_equal(initial_positions["EnemySpawnB"])
	assert_float((screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA") as Control).size.x).is_equal(48.0)
	assert_float((screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA") as Control).size.y).is_equal(_T59_BATTLEFIELD_SIZE.y)
	assert_float((screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB") as Control).size.x).is_equal(48.0)
	assert_float((screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB") as Control).size.y).is_equal(_T59_BATTLEFIELD_SIZE.y)


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
	assert_bool((screen.get_node("Margin/VBox/Summary") as Label).visible).is_false()


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
	var baseline_static_summary := String(summary_label.text)

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
	var wave_feedback := _local_feedback_state(screen)
	assert_int(int(after_wave.get("enemy_units_spawned", 0))).is_greater_equal(int(baseline_summary.get("enemy_units_spawned", 0)) + 2)
	assert_bool(String(status_label.text).to_lower().find("wave") >= 0).is_true()
	assert_bool(bool(wave_feedback["prompt_visible"])).is_true()
	assert_bool(String(wave_feedback["prompt_text"]).length() > 0).is_true()

	exchange_btn.emit_signal("pressed")
	await _await_frames(1)
	cleanup_btn.emit_signal("pressed")
	await _await_frames(1)
	finish_btn.emit_signal("pressed")
	await _await_frames(1)
	var terminal_summary := _bridge_summary_metrics(bridge)
	assert_int(int(terminal_summary.get("combat_exchanges", 0))).is_greater_equal(1)
	assert_bool(String(status_label.text).to_lower().find("finished") >= 0).is_true()
	assert_str(String(summary_label.text)).is_equal(baseline_static_summary)

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
	assert_bool(status_zh.length() > 0).is_true()
	assert_bool(summary_en.length() > 0).is_true()
	assert_bool(summary_zh.length() > 0).is_true()
	assert_bool((screen_en.get_node("Margin/VBox/Summary") as Label).visible).is_false()
	assert_bool((screen_zh.get_node("Margin/VBox/Summary") as Label).visible).is_false()

	TranslationServer.set_locale(original_locale)


# ACC:T70.11
func test_t70_designated_integration_flow_should_cover_outcome_evidence_and_transition_behavior() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await _await_frames(2)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var wave_btn: Button = screen.get_node("Margin/VBox/Controls/WaveBtn")
	var exchange_btn: Button = screen.get_node("Margin/VBox/Controls/ExchangeBtn")
	var cleanup_btn: Button = screen.get_node("Margin/VBox/Controls/CleanupBtn")
	var finish_btn: Button = screen.get_node("Margin/VBox/Controls/FinishBtn")
	var status_label: Label = screen.get_node("Margin/VBox/Status")
	var modal: PanelContainer = screen.get_node("DailySettlementModal")
	var runtime_payload: Label = screen.get_node("DailySettlementModal/VBox/EvidencePanel/RuntimeContextPayload")
	var expand_btn: Button = screen.get_node("DailySettlementModal/VBox/EvidencePanel/ExpandContextBtn")
	var reward_a: Button = screen.get_node("DailySettlementModal/VBox/Rewards/RewardA")

	assert_bool(bridge.has_method("ForceOutcomeForTest")).is_true()
	bridge.call("ForceOutcomeForTest", "settlement", 42)
	wave_btn.emit_signal("pressed")
	await _await_frames(1)
	exchange_btn.emit_signal("pressed")
	await _await_frames(1)
	cleanup_btn.emit_signal("pressed")
	await _await_frames(1)
	finish_btn.emit_signal("pressed")
	await _await_frames(1)

	assert_bool(modal.visible).is_true()
	assert_bool(get_tree().paused).is_true()
	assert_bool(runtime_payload.visible).is_false()
	expand_btn.emit_signal("pressed")
	await _await_frames(1)
	assert_bool(runtime_payload.visible).is_true()
	var payload_text := runtime_payload.text
	assert_bool(payload_text.find("\"outcome\":\"settlement\"") >= 0).is_true()
	assert_bool(payload_text.find("\"castle_hp\":42") >= 0).is_true()

	var summary_before_resolve: Dictionary = bridge.call("GetSummary")
	assert_str(String(summary_before_resolve.get("outcome", ""))).is_equal("settlement")
	reward_a.emit_signal("pressed")
	await _await_frames(1)
	assert_bool(modal.visible).is_false()
	assert_bool(get_tree().paused).is_false()
	assert_bool(String(status_label.text).to_lower().find("settlement resolved") >= 0).is_true()
	assert_that(bridge.call("GetSummary")).is_equal(summary_before_resolve)



