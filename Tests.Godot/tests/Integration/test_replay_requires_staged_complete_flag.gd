extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class ReplayStartGate:
	func can_start_replay(staged_complete: bool, slice_passes: Array[bool]) -> bool:
		if not staged_complete:
			return false
		if slice_passes.size() != 5:
			return false
		for passed in slice_passes:
			if not passed:
				return false
		return true


func test_replay_refuses_when_staged_complete_flag_is_false() -> void:
	var gate := ReplayStartGate.new()
	var can_start := gate.can_start_replay(false, [true, true, true, true, true])
	assert_bool(can_start).is_false()


# acceptance: ACC:T10.16
func test_replay_refuses_when_any_slice_is_incomplete_even_if_staged_complete_true() -> void:
	var gate := ReplayStartGate.new()
	var can_start := gate.can_start_replay(true, [true, true, false, true, true])
	assert_bool(can_start).is_false()


func test_replay_allows_start_only_when_flag_true_and_all_five_slices_pass() -> void:
	var gate := ReplayStartGate.new()
	var can_start := gate.can_start_replay(true, [true, true, true, true, true])
	assert_bool(can_start).is_true()
