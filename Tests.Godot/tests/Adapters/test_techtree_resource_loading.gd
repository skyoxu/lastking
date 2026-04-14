extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const TechTreeManagerAdapter = preload("res://Game.Godot/Adapters/TechTreeManager.cs")

func _write_json_resource(content: String) -> String:
    var path = "user://task17_techtree_%s.json" % str(Time.get_ticks_usec())
    var file = FileAccess.open(path, FileAccess.WRITE)
    assert_that(file).is_not_null()
    file.store_string(content)
    file.flush()
    file.close()
    return path

func _new_manager():
    var manager = TechTreeManagerAdapter.new()
    add_child(auto_free(manager))
    return manager

func _valid_techtree_json() -> String:
    return JSON.stringify({
        "nodes": [
            {
                "id": "barracks_i",
                "prerequisites": [],
                "modifiers": {"unit.hp": 10, "unit.attack": 2}
            },
            {
                "id": "barracks_ii",
                "prerequisites": ["barracks_i"],
                "modifiers": {"unit.hp": 25, "unit.attack": 5}
            }
        ]
    })

# ACC:T17.1
# acceptance: load valid tech-tree JSON through Godot resource APIs and preserve exact runtime ids/links/modifiers.
func test_load_valid_techtree_json_resource_exposes_exact_runtime_shape() -> void:
    var resource_path = _write_json_resource(_valid_techtree_json())
    var manager = _new_manager()

    var load_result: Variant = manager.call("LoadTechTreeFromJsonResource", resource_path)
    assert_int(typeof(load_result)).is_equal(TYPE_DICTIONARY)

    var result = Dictionary(load_result)
    assert_bool(bool(result.get("ok", false))).is_true()

    var runtime = Dictionary(result.get("runtime", {}))
    var node_ids = Array(runtime.get("node_ids", []))
    node_ids.sort()
    assert_array(node_ids).is_equal(["barracks_i", "barracks_ii"])

    var prerequisites = Dictionary(runtime.get("prerequisites", {}))
    assert_dict(prerequisites).is_equal({
        "barracks_i": [],
        "barracks_ii": ["barracks_i"]
    })

    var modifiers = Dictionary(runtime.get("modifiers", {}))
    assert_dict(modifiers).is_equal({
        "barracks_i": {"unit.hp": 10, "unit.attack": 2},
        "barracks_ii": {"unit.hp": 25, "unit.attack": 5}
    })

# ACC:T17.19
# acceptance: manager must keep JSON resource loading ownership and reject pre-decoded payload injection.
func test_manager_rejects_in_memory_payload_to_preserve_resource_loading_ownership() -> void:
    var manager = _new_manager()
    var bypass_payload = {
        "nodes": [
            {
                "id": "illegal_bypass",
                "prerequisites": [],
                "modifiers": {"unit.attack": 99}
            }
        ]
    }

    var reject_result: Variant = manager.call("LoadTechTreeFromJsonResource", bypass_payload)
    assert_int(typeof(reject_result)).is_equal(TYPE_DICTIONARY)

    var result = Dictionary(reject_result)
    assert_bool(bool(result.get("ok", true))).is_false()
    assert_str(str(result.get("error_code", ""))).is_equal("TECHTREE_RESOURCE_PATH_REQUIRED")
