extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const REQUIRED_GODOT_VERSION := "4.5.1"
const REQUIRED_DOTNET_MAJOR := 8

func _validate_windows_startup_prerequisite(report: Dictionary) -> Dictionary:
	var result := {
		"startup_ok": true,
		"reasons": []
	}

	if report.get("godot_version", "") != REQUIRED_GODOT_VERSION:
		result["startup_ok"] = false
		result["reasons"].append("godot_version_mismatch")

	if report.get("csharp_init_error", false):
		result["startup_ok"] = false
		result["reasons"].append("csharp_init_error")

	if not report.get("windows_export_profile_locked", false):
		result["startup_ok"] = false
		result["reasons"].append("windows_export_profile_unlocked")

	return result

func _validate_dotnet_toolchain(report: Dictionary) -> Dictionary:
	var result := {
		"compatible": true,
		"reasons": []
	}

	if report.get("godot_version", "") != REQUIRED_GODOT_VERSION:
		result["compatible"] = false
		result["reasons"].append("godot_version_mismatch")

	var sdk_major := int(report.get("dotnet_sdk_major", 0))
	var runtime_major := int(report.get("dotnet_runtime_major", 0))

	if sdk_major != runtime_major:
		result["compatible"] = false
		result["reasons"].append("sdk_runtime_major_mismatch")

	if sdk_major < REQUIRED_DOTNET_MAJOR:
		result["compatible"] = false
		result["reasons"].append("dotnet_sdk_too_old")

	return result

# ACC:T21.15
func test_startup_validation_fails_on_csharp_initialization_error() -> void:
	var report := {
		"godot_version": "4.5.1",
		"csharp_init_error": true,
		"windows_export_profile_locked": true
	}

	var result := _validate_windows_startup_prerequisite(report)

	assert_bool(result["startup_ok"]).is_false()
	assert_bool(result["reasons"].has("csharp_init_error")).is_true()

# ACC:T21.8
func test_dotnet_toolchain_compatible_path_passes_for_godot_451_windows_startup() -> void:
	var report := {
		"godot_version": "4.5.1",
		"dotnet_sdk_major": 8,
		"dotnet_runtime_major": 8
	}

	var result := _validate_dotnet_toolchain(report)

	assert_bool(result["compatible"]).is_true()

func test_dotnet_incompatibility_fails_validation() -> void:
	var report := {
		"godot_version": "4.5.1",
		"dotnet_sdk_major": 7,
		"dotnet_runtime_major": 8
	}

	var result := _validate_dotnet_toolchain(report)

	assert_bool(result["compatible"]).is_false()
	assert_bool(result["reasons"].has("dotnet_sdk_too_old")).is_true()
