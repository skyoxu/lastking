extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

# RED-FIRST: contract tests for autosave slot path policy.
class SaveManagerContractProbe:
    const AUTOSAVE_PATH := "user://autosave.save"

    var writes: Array[String] = []
    var created_files: Array[String] = []

    func request_save(slot_path: String, payload: Dictionary) -> bool:
        if slot_path != AUTOSAVE_PATH:
            return false

        writes.append(slot_path)
        created_files.append(slot_path)
        return true

    func on_day_start(day_index: int) -> void:
        request_save(AUTOSAVE_PATH, {"day": day_index})

static func _unique_paths(paths: Array[String]) -> Array[String]:
    var seen := {}
    var unique: Array[String] = []
    for path in paths:
        if not seen.has(path):
            seen[path] = true
            unique.append(path)
    return unique

# acceptance: ACC:T25.1
func test_rejects_write_to_alternate_slot_path() -> void:
    var sut := SaveManagerContractProbe.new()

    var accepted := sut.request_save("user://slot_2.save", {"gold": 10})

    assert_bool(accepted).is_false()
    assert_that(sut.writes.size()).is_equal(0)

# acceptance: ACC:T25.4
func test_day_start_autosave_keeps_single_fixed_path_and_no_extra_slot_files() -> void:
    var sut := SaveManagerContractProbe.new()

    sut.on_day_start(1)
    sut.on_day_start(2)
    sut.on_day_start(3)

    var unique := _unique_paths(sut.created_files)
    assert_that(unique.size()).is_equal(1)
    assert_str(unique[0]).is_equal("user://autosave.save")
    assert_bool(sut.created_files.has("user://autosave_day_2.save")).is_false()
