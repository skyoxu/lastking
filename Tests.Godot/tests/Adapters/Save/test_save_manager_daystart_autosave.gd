extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"
const AUTOSAVE_PATH := "user://autosave.save"
const EXTRA_SLOT_PATH := "user://autosave.save.slot"
const INDEX_SUFFIX := ":index"

func _new_bridge() -> Node:
    var script = load(BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", "task25-daystart-autosave", false, 20250425, 10, 10, 15)
    return bridge

func _save_slot_writes(bridge: Node) -> Array:
    var writes: Array = bridge.call("GetObservedWriteKeys")
    var slots: Array = []
    for key in writes:
        var as_text := str(key)
        if not as_text.ends_with(INDEX_SUFFIX):
            slots.append(as_text)
    return slots

# ACC:T25.16
func test_day_start_triggers_exactly_one_autosave_write_attempt() -> void:
    var bridge = _new_bridge()

    var first = bool(bridge.call("HandleDayStartAutoSave", 1))
    var second = bool(bridge.call("HandleDayStartAutoSave", 1))
    var writes := _save_slot_writes(bridge)

    assert_bool(first).is_true()
    assert_bool(second).is_false()
    assert_that(writes.count(AUTOSAVE_PATH)).is_equal(1)

# ACC:T25.3
func test_repeated_day_start_keeps_same_autosave_path_and_no_extra_slot_files() -> void:
    var bridge = _new_bridge()

    assert_bool(bool(bridge.call("HandleDayStartAutoSave", 1))).is_true()
    assert_bool(bool(bridge.call("HandleDayStartAutoSave", 2))).is_true()
    assert_bool(bool(bridge.call("HandleDayStartAutoSave", 3))).is_true()

    var writes := _save_slot_writes(bridge)
    assert_that(writes.size()).is_equal(3)
    for key in writes:
        assert_that(str(key)).is_equal(AUTOSAVE_PATH)

    assert_bool(bool(bridge.call("SlotExists", AUTOSAVE_PATH))).is_true()
    assert_bool(bool(bridge.call("SlotExists", EXTRA_SLOT_PATH))).is_false()

# ACC:T25.4
func test_first_day_start_creates_user_autosave_save_file() -> void:
    var bridge = _new_bridge()

    assert_bool(bool(bridge.call("HandleDayStartAutoSave", 1))).is_true()
    assert_bool(bool(bridge.call("SlotExists", AUTOSAVE_PATH))).is_true()
    var raw = str(bridge.call("LoadRaw", AUTOSAVE_PATH))
    assert_bool(raw.length() > 0).is_true()
