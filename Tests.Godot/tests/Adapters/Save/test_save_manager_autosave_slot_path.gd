extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"
const AUTOSAVE_PATH := "user://autosave.save"

static func _unique_paths(paths: Array[String]) -> Array[String]:
    var seen := {}
    var unique: Array[String] = []
    for path in paths:
        if not seen.has(path):
            seen[path] = true
            unique.append(path)
    return unique

# ACC:T45.5
# acceptance: ACC:T25.1
func test_rejects_write_to_alternate_slot_path() -> void:
    var bridge_script = load(BRIDGE_PATH)
    var sut = bridge_script.new()
    add_child(auto_free(sut))
    sut.call("ResetRuntime", "task45-autosave-slot-path", false, 20250425, 10, 10, 15)

    var accepted := bool(sut.call("SaveToSlot", "user://slot_2.save", JSON.stringify({
        "id": "slot-2",
        "level": 2,
        "score": 10,
        "health": 99,
        "inventory": ["wood"],
        "x": 1.0,
        "y": 2.0
    })))

    assert_bool(accepted).is_false()
    var writes: Array = sut.call("GetObservedWriteKeys")
    assert_that(writes.size()).is_equal(0)
    assert_bool(bool(sut.call("SlotExists", "user://slot_2.save"))).is_false()
    assert_bool(bool(sut.call("SlotExists", AUTOSAVE_PATH))).is_false()

# ACC:T45.2
# acceptance: ACC:T25.4
func test_day_start_autosave_keeps_single_fixed_path_and_no_extra_slot_files() -> void:
    var bridge_script = load(BRIDGE_PATH)
    var sut = bridge_script.new()
    add_child(auto_free(sut))
    sut.call("ResetRuntime", "task45-daystart-autosave", false, 20250425, 10, 10, 15)

    assert_bool(bool(sut.call("HandleDayStartAutoSave", 1))).is_true()
    assert_bool(bool(sut.call("HandleDayStartAutoSave", 2))).is_true()
    assert_bool(bool(sut.call("HandleDayStartAutoSave", 3))).is_true()

    var writes: Array = sut.call("GetObservedWriteKeys")
    var slot_writes: Array[String] = []
    for raw in writes:
        var key := str(raw)
        if not key.ends_with(":index"):
            slot_writes.append(key)

    var unique := _unique_paths(slot_writes)
    assert_that(slot_writes.size()).is_equal(3)
    assert_that(unique.size()).is_equal(1)
    assert_str(unique[0]).is_equal(AUTOSAVE_PATH)
    assert_bool(bool(sut.call("SlotExists", AUTOSAVE_PATH))).is_true()
    assert_bool(bool(sut.call("SlotExists", "user://autosave_day_2.save"))).is_false()
