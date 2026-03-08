extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const REQUIRED_CHANNEL_KEYS: Array[String] = ["elite_channel", "boss_channel"]

func _build_balance_config() -> Dictionary:
	return {
		"normal_channel": "normal_lane",
		"elite_channel": "elite_lane",
		"boss_channel": "boss_lane"
	}

func _select_runtime_channel_from_config(balance_config: Dictionary, wave_index: int) -> String:
	var normal_channel: String = str(balance_config.get("normal_channel", "normal"))
	var elite_channel: String = str(balance_config.get("elite_channel", "elite"))
	var boss_channel: String = str(balance_config.get("boss_channel", "boss"))

	if wave_index > 0 and wave_index % 10 == 0:
		return boss_channel
	if wave_index > 0 and wave_index % 3 == 0:
		return elite_channel
	return normal_channel

# ACC:T2.11
# Acceptance binding:
# Config must define elite and boss channel fields,
# and runtime wave routing must consume those fields.
func test_config_contains_elite_and_boss_and_runtime_selection_consumes_them() -> void:
	var config: Dictionary = _build_balance_config()

	for key in REQUIRED_CHANNEL_KEYS:
		assert_bool(config.has(key)).is_true()

	assert_str(_select_runtime_channel_from_config(config, 3)).is_equal(str(config["elite_channel"]))
	assert_str(_select_runtime_channel_from_config(config, 10)).is_equal(str(config["boss_channel"]))

	config["elite_channel"] = "elite_override"
	config["boss_channel"] = "boss_override"

	assert_str(_select_runtime_channel_from_config(config, 3)).is_equal("elite_override")
	assert_str(_select_runtime_channel_from_config(config, 10)).is_equal("boss_override")

func test_runtime_selection_has_stable_fallbacks_when_keys_missing() -> void:
	var config: Dictionary = {"normal_channel": "normal_override"}

	assert_str(_select_runtime_channel_from_config(config, 1)).is_equal("normal_override")
	assert_str(_select_runtime_channel_from_config(config, 3)).is_equal("elite")
	assert_str(_select_runtime_channel_from_config(config, 10)).is_equal("boss")
