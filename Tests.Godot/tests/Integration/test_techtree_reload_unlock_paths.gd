extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# Red-first harness: reload intentionally does not rebuild caches yet.
class TechTreeReloadHarness:
    extends RefCounted

    var _runtime_config: Dictionary = {}
    var _unlock_cache: Dictionary = {}
    var _multiplier_cache: Dictionary = {}
    var _unlocked: Dictionary = {}

    func set_unlocked(tech_ids: Array) -> void:
        _unlocked.clear()
        for tech_id in tech_ids:
            _unlocked[String(tech_id)] = true

    func load_json(json_text: String) -> void:
        var parsed: Variant = JSON.parse_string(json_text)
        if typeof(parsed) != TYPE_DICTIONARY:
            push_error("Invalid tech-tree JSON.")
            return
        _runtime_config = parsed
        _rebuild_caches()

    func reload_json(json_text: String) -> void:
        var parsed: Variant = JSON.parse_string(json_text)
        if typeof(parsed) != TYPE_DICTIONARY:
            push_error("Invalid tech-tree JSON.")
            return
        _runtime_config = parsed
        _rebuild_caches()

    func is_unlock_available(unit_id: String) -> bool:
        var required: Array = _unlock_cache.get(unit_id, [])
        for tech_id in required:
            if not _unlocked.has(String(tech_id)):
                return false
        return true

    func compute_barracks_attack(unit_id: String, base_attack: int) -> int:
        var multiplier: float = float(_multiplier_cache.get(unit_id, 1.0))
        return int(round(float(base_attack) * multiplier))

    func _rebuild_caches() -> void:
        _unlock_cache.clear()
        _multiplier_cache.clear()

        var nodes: Dictionary = _runtime_config.get("nodes", {})
        for unit_id in nodes.keys():
            var node: Dictionary = nodes[unit_id]
            var required: Array = node.get("requires", [])
            _unlock_cache[String(unit_id)] = required.duplicate()

        var multipliers: Dictionary = _runtime_config.get("multipliers", {})
        for unit_id in multipliers.keys():
            _multiplier_cache[String(unit_id)] = float(multipliers[unit_id])


func _build_techtree_json(prerequisites: Array, multiplier: float) -> String:
    var payload = {
        "nodes": {
            "unit_swordsman": {
                "requires": prerequisites
            }
        },
        "multipliers": {
            "unit_swordsman": multiplier
        }
    }
    return JSON.stringify(payload)


# ACC:T17.14
func test_reload_updates_unlock_availability_from_json_data() -> void:
    var runtime = TechTreeReloadHarness.new()
    runtime.set_unlocked([])
    runtime.load_json(_build_techtree_json(["tech_smithing"], 1.0))

    assert_that(runtime.is_unlock_available("unit_swordsman")).is_false()

    runtime.reload_json(_build_techtree_json([], 1.0))

    # Expected after reload: no prerequisites, unlock becomes available.
    assert_that(runtime.is_unlock_available("unit_swordsman")).is_true()


# ACC:T17.15
func test_reload_only_json_changes_multiplier_and_barracks_outcome() -> void:
    var runtime = TechTreeReloadHarness.new()
    runtime.set_unlocked(["tech_smithing"])
    runtime.load_json(_build_techtree_json(["tech_smithing"], 1.0))

    var before_reload: int = runtime.compute_barracks_attack("unit_swordsman", 10)
    runtime.reload_json(_build_techtree_json(["tech_smithing"], 1.5))
    var after_reload: int = runtime.compute_barracks_attack("unit_swordsman", 10)

    # Negative path guard: outcome must not stay unchanged after JSON multiplier reload.
    assert_that(after_reload).is_not_equal(before_reload)
    assert_that(after_reload).is_equal(15)


# ACC:T17.8
func test_switching_active_techtree_resource_changes_unlocks_and_stats() -> void:
    var runtime = TechTreeReloadHarness.new()
    runtime.set_unlocked([])

    var resource_a = _build_techtree_json(["tech_smithing"], 1.0)
    var resource_b = _build_techtree_json([], 2.0)

    runtime.load_json(resource_a)
    var unlock_a: bool = runtime.is_unlock_available("unit_swordsman")
    var attack_a: int = runtime.compute_barracks_attack("unit_swordsman", 10)

    runtime.reload_json(resource_b)
    var unlock_b: bool = runtime.is_unlock_available("unit_swordsman")
    var attack_b: int = runtime.compute_barracks_attack("unit_swordsman", 10)

    assert_that(unlock_a).is_false()
    assert_that(attack_a).is_equal(10)

    # Expected after switching valid resource: unlock and stats follow active resource.
    assert_that(unlock_b).is_true()
    assert_that(attack_b).is_equal(20)
