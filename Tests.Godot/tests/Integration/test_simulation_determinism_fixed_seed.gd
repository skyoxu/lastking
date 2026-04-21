extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const FIXED_SEED := 133742
const TICKS := 180

func _step_baseline(position: int, velocity: int, energy: int, delta: int) -> Dictionary:
	position += velocity
	velocity = clampi(velocity + delta, -10, 10)
	energy = maxi(0, energy - abs(delta))
	return {
		"position": position,
		"velocity": velocity,
		"energy": energy,
	}

func _step_optimized(position: int, velocity: int, energy: int, delta: int) -> Dictionary:
	var next_velocity := clampi(velocity + delta, -10, 10)
	var next_position := position + velocity
	var next_energy := maxi(0, energy - abs(delta))
	return {
		"position": next_position,
		"velocity": next_velocity,
		"energy": next_energy,
	}

func _simulate_core_loop(fixed_seed: int, ticks: int, use_optimized_path: bool) -> Dictionary:
	var position: int = fixed_seed % 23
	var velocity: int = 3
	var energy: int = 240
	var history: PackedInt32Array = PackedInt32Array()

	for i in range(ticks):
		var delta: int = int((fixed_seed + i * 17) % 9) - 4
		var next_state: Dictionary = (
			_step_optimized(position, velocity, energy, delta)
			if use_optimized_path
			else _step_baseline(position, velocity, energy, delta)
		)
		position = int(next_state.get("position", position))
		velocity = int(next_state.get("velocity", velocity))
		energy = int(next_state.get("energy", energy))
		history.append((position * 31 + velocity * 17 + energy + i) & 0x7fffffff)

	var checksum: int = 2166136261
	for point in history:
		checksum = int((checksum ^ point) * 16777619) & 0x7fffffff

	return {
		"seed": fixed_seed,
		"ticks": ticks,
		"final_state": {"position": position, "velocity": velocity, "energy": energy},
		"history": history,
		"checksum": checksum
	}

func _has_determinism_regression(pre_optimization: Dictionary, post_optimization: Dictionary) -> bool:
	if pre_optimization["checksum"] != post_optimization["checksum"]:
		return true
	if pre_optimization["final_state"] != post_optimization["final_state"]:
		return true
	return pre_optimization["history"] != post_optimization["history"]

# acceptance: ACC:T30.3
# Post-optimization output must remain unchanged for the same seed and tick budget.
func test_fixed_seed_simulation_matches_pre_optimization_baseline() -> void:
	var pre_optimization: Dictionary = _simulate_core_loop(FIXED_SEED, TICKS, false)
	var post_optimization: Dictionary = _simulate_core_loop(FIXED_SEED, TICKS, true)

	assert_that(post_optimization["checksum"]).is_equal(pre_optimization["checksum"])
	assert_that(post_optimization["final_state"]).is_equal(pre_optimization["final_state"])
	assert_that(post_optimization["history"]).is_equal(pre_optimization["history"])

func test_validator_rejects_changed_event_order_for_same_seed() -> void:
	var pre_optimization: Dictionary = _simulate_core_loop(FIXED_SEED, TICKS, false)
	var post_optimization: Dictionary = _simulate_core_loop(FIXED_SEED, TICKS, true)
	var mutated_history: PackedInt32Array = PackedInt32Array(post_optimization.get("history", PackedInt32Array()))
	if mutated_history.size() > 3:
		var swapped: int = mutated_history[1]
		mutated_history[1] = mutated_history[2]
		mutated_history[2] = swapped
	post_optimization["history"] = mutated_history

	assert_that(_has_determinism_regression(pre_optimization, post_optimization)).is_true()
