extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func _evaluate_windows_startup_validation(report: Dictionary) -> Dictionary:
    var checks: Array = report.get("check_results", [])
    for check in checks:
        if not check.get("passed", false):
            return {
                "accepted": false,
                "reason": "failing check blocks acceptance"
            }
    var steam_runtime_check_executed: bool = bool(report.get("steam_runtime_check_executed", false))
    var steam_runtime_launch_count: int = int(report.get("steam_runtime_launch_count", 0))
    if not steam_runtime_check_executed or steam_runtime_launch_count <= 0:
        return {
            "accepted": false,
            "reason": "steam runtime launch check was skipped"
        }
    var integration_errors: Array = report.get("integration_errors", [])
    for error_item in integration_errors:
        var error_text: String = String(error_item).to_lower()
        if error_text.find("steam") >= 0:
            return {
                "accepted": false,
                "reason": "steam-blocking integration errors detected"
            }

    return {
        "accepted": true,
        "reason": "all referenced checks passed"
    }

# acceptance: ACC:T21.17
func test_rejects_acceptance_when_steam_runtime_launch_check_was_skipped() -> void:
    var report := {
        "steam_runtime_launch_count": 0,
        "steam_runtime_check_executed": false,
        "check_results": [
            {"name": "windows_export_profile_locked", "passed": true},
            {"name": "startup_validation_pipeline_completed", "passed": true}
        ],
        "integration_errors": []
    }

    var result: Dictionary = _evaluate_windows_startup_validation(report)

    assert_that(result.get("accepted", true)).is_false()
    assert_that(str(result.get("reason", ""))).contains("steam runtime launch")

# acceptance: ACC:T21.2
func test_blocks_acceptance_when_any_referenced_check_fails() -> void:
    var report := {
        "steam_runtime_launch_count": 1,
        "steam_runtime_check_executed": true,
        "check_results": [
            {"name": "windows_export_profile_locked", "passed": true},
            {"name": "reused_t11_baseline_validation", "passed": false}
        ],
        "integration_errors": []
    }

    var result: Dictionary = _evaluate_windows_startup_validation(report)

    assert_that(result.get("accepted", true)).is_false()
    assert_that(str(result.get("reason", ""))).contains("failing check")

# acceptance: ACC:T21.6
func test_rejects_launch_with_steam_blocking_integration_errors() -> void:
    var report := {
        "steam_runtime_launch_count": 1,
        "steam_runtime_check_executed": true,
        "check_results": [
            {"name": "windows_export_profile_locked", "passed": true},
            {"name": "reused_t11_baseline_validation", "passed": true}
        ],
        "integration_errors": ["steam_api_init_failed"]
    }

    var result: Dictionary = _evaluate_windows_startup_validation(report)

    assert_that(result.get("accepted", true)).is_false()
    assert_that(str(result.get("reason", ""))).contains("steam-blocking")
