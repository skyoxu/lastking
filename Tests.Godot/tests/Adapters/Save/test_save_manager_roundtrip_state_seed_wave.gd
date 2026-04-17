extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const BRIDGE_PATH := "res://Game.Godot/Adapters/Save/SaveManagerTestBridge.cs"
const AUTOSAVE_PATH := "user://autosave.save"

func _new_bridge() -> Node:
    var script = load(BRIDGE_PATH)
    var bridge = script.new()
    add_child(auto_free(bridge))
    bridge.call("ResetRuntime", "task25-roundtrip-state-seed-wave", false, 20250425, 10, 10, 15)
    return bridge

func _seed_state_json() -> String:
    return JSON.stringify({
        "id": "seed-wave-state",
        "level": 9,
        "score": 405,
        "health": 83,
        "inventory": ["wood", "stone", "iron"],
        "x": 17.5,
        "y": -4.25
    })

func _state_dict(json_text: String) -> Dictionary:
    var parsed = JSON.parse_string(json_text)
    assert_that(parsed).is_not_null()
    return parsed as Dictionary

# acceptance: ACC:T25.6
func test_serialized_payload_includes_version_and_all_runtime_fields() -> void:
    var bridge = _new_bridge()

    assert_bool(bool(bridge.call("SaveToSlot", AUTOSAVE_PATH, _seed_state_json()))).is_true()
    var raw := str(bridge.call("LoadRaw", AUTOSAVE_PATH))
    assert_bool(raw.length() > 0).is_true()

    var payload = JSON.parse_string(raw)
    assert_that(payload).is_not_null()
    var root := payload as Dictionary

    assert_bool(root.has("Metadata")).is_true()
    assert_bool(root.has("State")).is_true()
    assert_bool(root.has("DayNightRuntime")).is_true()

    var metadata := root["Metadata"] as Dictionary
    assert_str(str(metadata.get("Version", ""))).is_equal("1.0.0")

    var runtime := root["DayNightRuntime"] as Dictionary
    assert_bool(runtime.has("Seed")).is_true()
    assert_bool(runtime.has("Tick")).is_true()
    assert_bool(runtime.has("PhaseElapsedSeconds")).is_true()

# acceptance: ACC:T25.10
func test_roundtrip_restores_state_seed_and_wave_timer_exactly() -> void:
    var bridge = _new_bridge()
    bridge.call("AdvanceRuntime", 13.0, true)
    bridge.call("AdvanceRuntime", 7.0, true)

    var tick_before := int(bridge.call("CurrentTick"))
    var elapsed_before := float(bridge.call("CurrentPhaseElapsedSeconds"))
    var day_before := int(bridge.call("CurrentDay"))
    var phase_before := str(bridge.call("CurrentPhase"))

    assert_bool(bool(bridge.call("SaveToSlot", AUTOSAVE_PATH, _seed_state_json()))).is_true()

    var reset_state = JSON.stringify({
        "id": "reset-state",
        "level": 1,
        "score": 1,
        "health": 1,
        "inventory": ["dust"],
        "x": 0.0,
        "y": 0.0
    })
    bridge.call("ResetRuntime", "task25-roundtrip-state-seed-wave", false, 777, 10, 10, 15)
    assert_bool(bool(bridge.call("SaveToSlot", "user://tmp-reset-slot.save", reset_state))).is_true()

    assert_bool(bool(bridge.call("LoadSlot", AUTOSAVE_PATH))).is_true()
    var after_state := _state_dict(str(bridge.call("SnapshotStateJson")))

    assert_that(after_state.get("id")).is_equal("seed-wave-state")
    assert_that(int(after_state.get("level", -1))).is_equal(9)
    assert_that(int(after_state.get("score", -1))).is_equal(405)
    assert_that(int(after_state.get("health", -1))).is_equal(83)

    assert_that(int(bridge.call("CurrentTick"))).is_equal(tick_before)
    assert_float(float(bridge.call("CurrentPhaseElapsedSeconds"))).is_equal(elapsed_before)
    assert_that(int(bridge.call("CurrentDay"))).is_equal(day_before)
    assert_str(str(bridge.call("CurrentPhase"))).is_equal(phase_before)

