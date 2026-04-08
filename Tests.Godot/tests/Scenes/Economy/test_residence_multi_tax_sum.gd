extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class ResidenceIncomeTickCalculator:
	func calculate_tick_delta(residences: Array[Dictionary]) -> int:
		var total = 0
		for residence in residences:
			if residence.get("is_built", false) and residence.get("is_settleable", false):
				total += int(residence.get("tax_amount", 0))
		return total

var _calculator = ResidenceIncomeTickCalculator.new()

# ACC:T14.11
# Every 15-second income tick should equal the sum of all eligible residence taxes.
func test_income_tick_equals_sum_of_all_eligible_residence_taxes() -> void:
	var residences: Array[Dictionary] = [
		{"is_built": true, "is_settleable": true, "tax_amount": 10},
		{"is_built": true, "is_settleable": true, "tax_amount": 15},
		{"is_built": true, "is_settleable": true, "tax_amount": 7}
	]

	var delta = _calculator.calculate_tick_delta(residences)

	assert_int(delta).is_equal(32)

func test_income_tick_ignores_unbuilt_or_unsettleable_residences() -> void:
	var residences: Array[Dictionary] = [
		{"is_built": false, "is_settleable": true, "tax_amount": 10},
		{"is_built": true, "is_settleable": false, "tax_amount": 15},
		{"is_built": true, "is_settleable": true, "tax_amount": 4}
	]

	var delta = _calculator.calculate_tick_delta(residences)

	assert_int(delta).is_equal(4)
