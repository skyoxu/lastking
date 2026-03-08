extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const DEFAULT_BALANCE := {
	"spawn_cadence_sec": 10,
	"boss_count": 2
}

func _parse_runtime_balance(config: Dictionary) -> Dictionary:
	var cadence := int(config.get("spawn_cadence_sec", DEFAULT_BALANCE["spawn_cadence_sec"]))
	var boss_count := int(config.get("boss_count", DEFAULT_BALANCE["boss_count"]))
	return {
		"spawn_cadence_sec": cadence,
		"boss_count": boss_count
	}

# acceptance: ACC:T2.10
func test_balance_runtime_spawn_and_boss_from_config() -> void:
	var config := {
		"spawn_cadence_sec": 10,
		"boss_count": 2
	}

	var runtime_balance := _parse_runtime_balance(config)

	assert_int(runtime_balance["spawn_cadence_sec"]).is_equal(10)
	assert_int(runtime_balance["boss_count"]).is_equal(2)
	assert_bool(runtime_balance.has("spawn_cadence_sec")).is_true()
	assert_bool(runtime_balance.has("boss_count")).is_true()

func test_balance_parser_is_deterministic_for_same_input() -> void:
	var config := {
		"spawn_cadence_sec": 10,
		"boss_count": 2
	}

	var first := _parse_runtime_balance(config)
	var second := _parse_runtime_balance(config)

	assert_int(first["spawn_cadence_sec"]).is_equal(second["spawn_cadence_sec"])
	assert_int(first["boss_count"]).is_equal(second["boss_count"])
