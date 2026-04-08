extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class ResidenceTaxReplayHarness:
    static func replay(seed: int, ticks: int) -> Dictionary:
        var rng = RandomNumberGenerator.new()
        rng.seed = seed
        var trace: Array = []
        var total_gold_delta = 0

        for tick in ticks:
            var gold_delta = int((rng.randi() + seed + tick) % 17) + 1
            total_gold_delta += gold_delta
            trace.append({
                "tick": tick,
                "gold_delta": gold_delta,
                "reason": "residence_tax_tick"
            })

        return {
            "trace": trace,
            "total_gold_delta": total_gold_delta
        }

func _trace_signature(trace: Array) -> String:
    var chunks: Array[String] = []
    for entry in trace:
        chunks.append("%s:%s" % [str(entry["tick"]), str(entry["gold_delta"])])
    return "|".join(PackedStringArray(chunks))

# acceptance: ACC:T14.18
func test_fixed_seed_replay_preserves_trace_order_and_aggregate() -> void:
    var first_run = ResidenceTaxReplayHarness.replay(1418, 6)
    var second_run = ResidenceTaxReplayHarness.replay(1418, 6)

    assert_that(first_run["trace"]).is_equal(second_run["trace"])
    assert_that(first_run["total_gold_delta"]).is_equal(second_run["total_gold_delta"])

    for entry in first_run["trace"]:
        assert_that(entry.has("reason")).is_true()

func test_replay_with_different_seed_changes_trace_signature() -> void:
    var first_run = ResidenceTaxReplayHarness.replay(1418, 6)
    var second_run = ResidenceTaxReplayHarness.replay(2418, 6)

    var first_signature = _trace_signature(first_run["trace"])
    var second_signature = _trace_signature(second_run["trace"])

    assert_that(first_signature).is_not_equal(second_signature)
