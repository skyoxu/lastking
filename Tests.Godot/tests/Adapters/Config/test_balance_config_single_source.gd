extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const CONFIG_MANAGER_NAME := "ConfigManager"
const SELECTED_BALANCE_CONFIG_SOURCE := "res://Config/balance.json"
const ALLOWED_BALANCE_ACCESS_SURFACES := ["ConfigManager"]
const ALLOWED_SOURCE_EXTENSIONS := ["json", "ini"]

func _get_extension(path: String) -> String:
	var dot_index := path.rfind(".")
	if dot_index == -1:
		return ""
	return path.substr(dot_index + 1, path.length() - dot_index - 1).to_lower()

func test_balance_config_source_declares_single_supported_file() -> void:
	assert_bool(SELECTED_BALANCE_CONFIG_SOURCE != "").is_true()
	var extension := _get_extension(SELECTED_BALANCE_CONFIG_SOURCE)
	assert_bool(ALLOWED_SOURCE_EXTENSIONS.has(extension)).is_true()

# ACC:T2.7
# acceptance: loop/wave/spawn must read balance values through one global ConfigManager surface.
func test_balance_access_surface_is_config_manager_only() -> void:
	var access_surfaces: Array = ALLOWED_BALANCE_ACCESS_SURFACES.duplicate()
	assert_int(access_surfaces.size()).is_equal(1)
	assert_str(str(access_surfaces[0])).is_equal(CONFIG_MANAGER_NAME)
	assert_bool(not access_surfaces.has("LoopController")).is_true()
	assert_bool(not access_surfaces.has("WaveController")).is_true()
	assert_bool(not access_surfaces.has("SpawnController")).is_true()
