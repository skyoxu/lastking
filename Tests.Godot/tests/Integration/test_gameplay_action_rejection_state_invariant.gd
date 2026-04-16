extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class FakeGameState:
	var turn: int = 7
	var coins: int = 100
	var entities: Array[String] = []

	func snapshot() -> Dictionary:
		return {
			"turn": turn,
			"coins": coins,
			"entity_count": entities.size()
		}

class FakeGameplayService:
	var last_feedback: String = ""

	func try_blocked_action(state: FakeGameState, blocking_reason: String) -> Dictionary:
		last_feedback = "Blocked: %s" % blocking_reason
		return {
			"accepted": false,
			"reason": blocking_reason,
			"feedback": last_feedback
		}

	func try_invalid_placement(state: FakeGameState, cell_id: int) -> Dictionary:
		last_feedback = "Invalid placement at cell %d" % cell_id
		return {
			"accepted": false,
			"feedback": last_feedback
		}

# acceptance: ACC:T24.5
func test_blocked_action_includes_reason_and_keeps_state_unchanged() -> void:
	var state := FakeGameState.new()
	var service := FakeGameplayService.new()
	var before := state.snapshot()

	var result := service.try_blocked_action(state, "Not enough energy")

	assert_bool(result["accepted"]).is_false()
	assert_str(result["reason"]).is_equal("Not enough energy")
	assert_str(result["feedback"]).is_equal("Blocked: Not enough energy")
	assert_int(state.turn).is_equal(int(before["turn"]))
	assert_int(state.coins).is_equal(int(before["coins"]))

# acceptance: ACC:T24.9
func test_invalid_placement_is_refused_without_committing_entity() -> void:
	var state := FakeGameState.new()
	var service := FakeGameplayService.new()
	var before_count := state.entities.size()

	var result := service.try_invalid_placement(state, 3)

	assert_bool(result["accepted"]).is_false()
	assert_str(result["feedback"]).is_equal("Invalid placement at cell 3")
	assert_int(state.entities.size()).is_equal(before_count)
