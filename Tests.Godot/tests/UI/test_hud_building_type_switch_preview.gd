extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BUILDING_RUNTIME_SCENE := "res://Game.Godot/Scenes/Building/BuildingModeRuntime.tscn"

func _new_runtime() -> Node:
	var packed: PackedScene = load(BUILDING_RUNTIME_SCENE)
	return packed.instantiate()

const REQUIRED_TYPES := [
	"castle",
	"residence",
	"mine",
	"barracks",
	"mg tower",
	"wall",
	"mine trap",
]

# ACC:T13.6
func test_build_mode_exposes_all_required_building_types_for_preview() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	var expected = REQUIRED_TYPES.duplicate()
	expected.sort()
	assert_that(runtime.list_building_types()).is_equal(expected)
	runtime.queue_free()

# ACC:T13.15
func test_switching_building_type_immediately_updates_preview_validation_and_cost() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	for type_id in REQUIRED_TYPES:
		assert_that(runtime.select_building_type(type_id)).is_true()
		var preview = runtime.preview_at(Vector2(8, 8))
		assert_that(preview["selected_type"]).is_equal(type_id)
		assert_that(runtime.get_cost(type_id)).is_greater(0)
		assert_that(runtime.get_footprint_size(type_id)).is_greater(0)
	runtime.queue_free()

func test_switching_to_unsupported_type_keeps_preview_state_unchanged() -> void:
	var runtime = _new_runtime()
	add_child(runtime)
	assert_that(runtime.select_building_type("castle")).is_true()
	var snapshotBefore = runtime.preview_at(Vector2(8, 8)).duplicate(true)
	assert_that(runtime.select_building_type("unknown_type")).is_false()
	var snapshotAfter = runtime.preview_at(Vector2(8, 8))
	assert_that(snapshotAfter["selected_type"]).is_equal(snapshotBefore["selected_type"])
	runtime.queue_free()
