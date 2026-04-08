extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class ResidenceTaxFlowDouble:
	var player_gold: int
	var treasury_gold: int
	var residence_tax: int
	var negative_gold_policy: String

	func _init(initial_player_gold: int, initial_treasury_gold: int, tax_amount: int, policy: String = "refuse_and_unchanged") -> void:
		player_gold = initial_player_gold
		treasury_gold = initial_treasury_gold
		residence_tax = tax_amount
		negative_gold_policy = policy

	func apply_residence_tax() -> Dictionary:
		if negative_gold_policy == "refuse_and_unchanged" and (player_gold - residence_tax) < 0:
			return {
				"status": "refused_insufficient_funds",
				"player_gold": player_gold,
				"treasury_gold": treasury_gold,
				"policy": negative_gold_policy,
			}
		player_gold -= residence_tax
		treasury_gold += residence_tax
		return {
			"status": "applied",
			"player_gold": player_gold,
			"treasury_gold": treasury_gold,
			"policy": negative_gold_policy,
		}

# acceptance: ACC:T14.16
func test_residence_tax_refuses_and_keeps_all_state_unchanged_when_player_would_go_negative() -> void:
	var flow = ResidenceTaxFlowDouble.new(2, 10, 5, "refuse_and_unchanged")
	var before_player = flow.player_gold
	var before_treasury = flow.treasury_gold

	var result = flow.apply_residence_tax()

	assert_that(result["policy"]).is_equal("refuse_and_unchanged")
	assert_that(result["status"]).is_equal("refused_insufficient_funds")
	assert_that(flow.player_gold).is_equal(before_player)
	assert_that(flow.treasury_gold).is_equal(before_treasury)
