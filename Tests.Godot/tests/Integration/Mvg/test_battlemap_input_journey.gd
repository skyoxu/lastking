extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BATTLEMAP := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
const WAVE_BUTTON := "BattleHud/CombatHud/BottomBar/Root/SkillsPanel/VBox/SkillButtons/WaveAction"

func _await_frames(count: int) -> void:
	for _i in range(count):
		await get_tree().process_frame

func _summary(bridge: Node) -> Dictionary:
	var value: Variant = bridge.call("GetSummary")
	return value if value is Dictionary else {}

func _wait_for_wave(bridge: Node, before: int, timeout_ms: int = 3000) -> bool:
	var deadline := Time.get_ticks_msec() + timeout_ms
	while Time.get_ticks_msec() < deadline:
		if int(_summary(bridge).get("enemy_units_spawned", 0)) > before:
			return true
		await get_tree().process_frame
	print("MVG_BATTLEMAP_WAIT_TIMEOUT summary=", _summary(bridge))
	return int(_summary(bridge).get("enemy_units_spawned", 0)) > before

func _activate(button: Button) -> bool:
	if not is_instance_valid(button) or not button.is_visible_in_tree() or button.disabled:
		return false
	button.grab_focus()
	await get_tree().process_frame
	if not button.has_focus():
		return false
	var press := InputEventAction.new()
	press.action = "ui_accept"
	press.pressed = true
	button.get_viewport().push_input(press)
	await get_tree().process_frame
	var release := InputEventAction.new()
	release.action = "ui_accept"
	release.pressed = false
	button.get_viewport().push_input(release)
	await get_tree().process_frame
	return true

func after_test() -> void:
	var release := InputEventAction.new()
	release.action = "ui_accept"
	release.pressed = false
	get_viewport().push_input(release)
	Input.action_release("ui_accept")
	await get_tree().process_frame

func test_focused_wave_action_uses_engine_input_to_spawn_wave() -> void:
	var screen := BATTLEMAP.instantiate()
	add_child(auto_free(screen))
	await _await_frames(3)

	var bridge: Node = screen.get_node("CombatExperienceRuntimeBridge")
	var wave_button: Button = screen.get_node(WAVE_BUTTON)
	var before := int(_summary(bridge).get("enemy_units_spawned", 0))

	if OS.get_environment("MVG_INPUT_CHALLENGE") == "disable-battlemap-wave-input":
		for connection in wave_button.pressed.get_connections():
			wave_button.pressed.disconnect(connection["callable"])

	assert_bool(await _activate(wave_button)).is_true()
	var advanced := await _wait_for_wave(bridge, before)
	assert_bool(advanced).override_failure_message("MVG_BATTLEMAP_INPUT_DID_NOT_SPAWN_WAVE").is_true()
