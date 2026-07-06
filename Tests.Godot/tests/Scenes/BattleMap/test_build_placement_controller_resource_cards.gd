extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _await_frames(count: int) -> void:
	for _i in range(count):
		await await_idle_frame()

func _screen_runtime() -> Dictionary:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(screen)
	await _await_frames(3)
	return {
		"screen": screen,
		"hud": screen.get_node("BattleHud"),
		"battlefield": screen.get_node("Background"),
		"bridge": screen.get_node("CombatExperienceRuntimeBridge"),
	}

func test_build_cards_should_reflect_runtime_resource_affordability_and_missing_gaps() -> void:
	var runtime := await _screen_runtime()
	var hud: Control = runtime["hud"]
	var bridge: Node = runtime["bridge"]
	var tower_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/TowerSlot")
	var barracks_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BarracksSlot")
	var residence_slot: Button = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/ResidenceSlot")
	var tower_meta: Label = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/TowerSlot/Card/Meta")
	var sniper_tower_meta: Label = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/SniperTowerSlot/Card/Meta")
	var barracks_meta: Label = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/BarracksSlot/Card/Meta")
	var residence_meta: Label = hud.get_node("CombatHud/BottomBar/Root/BuildingsPanel/VBox/BuildButtons/ResidenceSlot/Card/Meta")

	bridge.call("ConfigureResourcesForTest", 40, 44, 26)
	hud.call("RefreshBottomBarFromRuntimeForTest")

	assert_bool(tower_slot.disabled).is_true()
	assert_bool(barracks_slot.disabled).is_true()
	assert_bool(residence_slot.disabled).is_false()
	assert_str(tower_meta.text).contains("20")
	assert_str(sniper_tower_meta.text).contains("50")
	assert_str(barracks_meta.text).contains("40")
	assert_bool(residence_meta.text.find("Missing") < 0).is_true()

	bridge.call("ConfigureResourcesForTest", 120, 44, 26)
	hud.call("RefreshBottomBarFromRuntimeForTest")

	assert_bool(tower_slot.disabled).is_false()
	assert_bool(barracks_slot.disabled).is_false()
	assert_bool(residence_slot.disabled).is_false()
