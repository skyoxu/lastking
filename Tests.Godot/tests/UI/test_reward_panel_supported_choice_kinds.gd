extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"
const RewardPanelRuntime = preload("res://Game.Godot/Scripts/Reward/RewardPanelRuntime.gd")

# acceptance: ACC:T18.7
func test_reward_panel_presents_resource_unit_and_bonus_choices() -> void:
    var generated_choices := [
        {"kind": "resource", "id": "wood_bundle"},
        {"kind": "unit", "id": "spearman"},
        {"kind": "bonus", "id": "extra_reroll"}
    ]

    var panel := RewardPanelRuntime.new()
    panel.present_nightly_choices(generated_choices)

    var presented_kinds: Array = []
    for choice in panel.presented_choices:
        presented_kinds.append(choice.get("kind", ""))

    assert(panel.presented_choices.size() == 3)
    assert(presented_kinds.has("resource"))
    assert(presented_kinds.has("unit"))
    assert(presented_kinds.has("bonus"))

func test_reward_panel_ignores_unsupported_choice_kind() -> void:
    var generated_choices := [
        {"kind": "resource", "id": "wood_bundle"},
        {"kind": "unknown", "id": "mystery_payload"},
        {"kind": "unit", "id": "spearman"}
    ]

    var panel := RewardPanelRuntime.new()
    panel.present_nightly_choices(generated_choices)

    var presented_kinds: Array = []
    for choice in panel.presented_choices:
        presented_kinds.append(choice.get("kind", ""))

    assert(presented_kinds.has("resource"))
    assert(presented_kinds.has("unit"))
    assert(not presented_kinds.has("unknown"))
