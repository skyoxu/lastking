extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _hud() -> Node:
	var hud = preload("res://Game.Godot/Scenes/UI/HUD.tscn").instantiate()
	add_child(auto_free(hud))
	await get_tree().process_frame
	return hud

func _load_json(path: String) -> Variant:
	if not FileAccess.file_exists(path):
		return null
	var raw := FileAccess.get_file_as_string(path)
	if raw.strip_edges() == "":
		return null
	return JSON.parse_string(raw)

func _find_task_entry(items: Array, task_id: int) -> Dictionary:
	for item in items:
		if item is Dictionary and int(item.get("taskmaster_id", -1)) == task_id:
			return item
	return {}

func _find_master_task_entry(items: Array, task_id: int) -> Dictionary:
	for item in items:
		if item is Dictionary and int(item.get("id", -1)) == task_id:
			return item
	return {}

# acceptance anchor: ACC:T3.16
# acceptance anchor: ACC:T42.8
# Verifies the real HUD scene initializes to a Day1 baseline before any runtime day/night switch event.
func test_hud_scene_initial_state_is_day1_before_any_phase_switch() -> void:
	var hud = await _hud()
	var day_label: Label = hud.get_node("TopBar/HBox/DayLabel")
	var cycle_label: Label = hud.get_node("TopBar/HBox/CycleRemainingLabel")

	assert_str(day_label.text).is_equal("Day: 1")
	assert_bool(cycle_label.text.begins_with("Cycle Remaining:")).is_true()
	assert_bool(cycle_label.text.find("240.0s") >= 0).is_true()

# acceptance anchor: ACC:T42.9
func test_task42_scope_item_mapping_is_explicit_and_complete() -> void:
	var back_path := "res://../.taskmaster/tasks/tasks_back.json"
	var gameplay_path := "res://../.taskmaster/tasks/tasks_gameplay.json"
	var tasks_path := "res://../.taskmaster/tasks/tasks.json"
	var back_view := _load_json(back_path)
	var gameplay_view := _load_json(gameplay_path)
	var tasks_view := _load_json(tasks_path)
	assert_bool(back_view is Array).is_true()
	assert_bool(gameplay_view is Array).is_true()
	assert_bool(tasks_view is Dictionary).is_true()

	var back_entry := _find_task_entry(back_view, 42)
	var gameplay_entry := _find_task_entry(gameplay_view, 42)
	var tasks_master: Array = tasks_view.get("master", {}).get("tasks", [])
	var master_entry := _find_master_task_entry(tasks_master, 42)
	assert_bool(not back_entry.is_empty()).is_true()
	assert_bool(not gameplay_entry.is_empty()).is_true()
	assert_bool(not master_entry.is_empty()).is_true()

	var back_scope_ids: Array = back_entry.get("ui_wiring_candidate", {}).get("scope_task_ids", [])
	var gameplay_scope_ids: Array = gameplay_entry.get("ui_wiring_candidate", {}).get("scope_task_ids", [])
	var master_scope_ids: Array = master_entry.get("dependencies", [])
	var normalized_back: Array[String] = []
	var normalized_gameplay: Array[String] = []
	var normalized_master: Array[String] = []
	for scope_id in back_scope_ids:
		normalized_back.append("T%02d" % int(scope_id))
	for scope_id in gameplay_scope_ids:
		normalized_gameplay.append("T%02d" % int(scope_id))
	for scope_id in master_scope_ids:
		normalized_master.append("T%02d" % int(scope_id))

	normalized_back.sort()
	normalized_gameplay.sort()
	normalized_master.sort()

	assert_int(normalized_master.size()).is_greater(0)
	assert_that(normalized_back).contains_exactly(normalized_master)
	assert_that(normalized_gameplay).contains_exactly(normalized_master)

	var details_text := str(master_entry.get("details", ""))
	for scope_item in normalized_master:
		assert_bool(details_text.find(scope_item) >= 0).is_true()
