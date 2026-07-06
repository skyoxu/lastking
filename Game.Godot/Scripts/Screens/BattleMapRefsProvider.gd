extends Node

var _screen: Control = null

func configure(refs: Dictionary) -> void:
	_screen = refs["screen"]

func _required(path: String) -> Node:
	return _screen.get_node(path)

func _optional(path: String) -> Node:
	return _screen.get_node_or_null(path)

func build_refs() -> Dictionary:
	return {
		"title": _required("LegacyPrototypeRoot/VBox/Title"),
		"status": _required("LegacyPrototypeRoot/VBox/Status"),
		"summary": _required("BattleHud/CombatHud/BottomBar/Root/BattlePanel/VBox/ReservedLabel"),
		"background": _required("Background"),
		"map_marker_layer": _required("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer"),
		"enemy_spawn_b": _required("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB"),
		"local_feedback_layer": _required("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer"),
		"hit_flash_overlay": _required("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/HitFlashOverlay"),
		"wall_pressure_overlay": _required("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/WallPressureOverlay"),
		"wall_hit_flash_overlay": _required("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/WallHitFlashOverlay"),
		"wall_crack_overlay": _required("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/WallCrackOverlay"),
		"wall_damage_layer": _required("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/WallDamageLayer"),
		"local_prompt_panel": _required("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel"),
		"local_prompt_label": _required("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel"),
		"daily_settlement_modal": _optional("DailySettlementModal"),
		"daily_settlement_title": _optional("DailySettlementModal/VBox/Title"),
		"daily_settlement_summary": _optional("DailySettlementModal/VBox/Summary"),
		"daily_evidence_title": _optional("DailySettlementModal/VBox/EvidencePanel/EvidenceTitle"),
		"daily_evidence_hp": _optional("DailySettlementModal/VBox/EvidencePanel/HpEvidence"),
		"daily_evidence_kills": _optional("DailySettlementModal/VBox/EvidencePanel/KillEvidence"),
		"daily_evidence_reward_summary": _optional("DailySettlementModal/VBox/EvidencePanel/RewardSummaryEvidence"),
		"daily_evidence_resources": _optional("DailySettlementModal/VBox/EvidencePanel/ResourceEvidence"),
		"daily_evidence_defeat_reason": _optional("DailySettlementModal/VBox/EvidencePanel/DefeatReasonEvidence"),
		"daily_expand_context_btn": _optional("DailySettlementModal/VBox/EvidencePanel/ExpandContextBtn"),
		"daily_runtime_context_payload": _optional("DailySettlementModal/VBox/EvidencePanel/RuntimeContextPayload"),
		"daily_reward_a": _optional("DailySettlementModal/VBox/Rewards/RewardA"),
		"daily_reward_b": _optional("DailySettlementModal/VBox/Rewards/RewardB"),
		"daily_reward_c": _optional("DailySettlementModal/VBox/Rewards/RewardC"),
		"daily_hint": _optional("DailySettlementModal/VBox/Hint"),
		"victory_outcome_modal": _optional("VictoryOutcomeModal"),
		"victory_outcome_title": _optional("VictoryOutcomeModal/VBox/Title"),
		"victory_outcome_summary": _optional("VictoryOutcomeModal/VBox/Summary"),
		"victory_outcome_hint": _optional("VictoryOutcomeModal/VBox/Hint"),
		"victory_return_btn": _optional("VictoryOutcomeModal/VBox/Actions/ReturnToMainMenuBtn"),
		"victory_restart_btn": _optional("VictoryOutcomeModal/VBox/Actions/RestartBtn"),
		"defeat_outcome_modal": _optional("DefeatOutcomeModal"),
		"defeat_outcome_title": _optional("DefeatOutcomeModal/VBox/Title"),
		"defeat_outcome_summary": _optional("DefeatOutcomeModal/VBox/Summary"),
		"defeat_outcome_hint": _optional("DefeatOutcomeModal/VBox/Hint"),
		"defeat_return_btn": _optional("DefeatOutcomeModal/VBox/Actions/ReturnToMainMenuBtn"),
		"defeat_restart_btn": _optional("DefeatOutcomeModal/VBox/Actions/RestartBtn"),
		"battle_settings_title": _optional("BattleSettingsMenu/VBox/Title"),
		"battle_settings_return_btn": _optional("BattleSettingsMenu/VBox/Buttons/ReturnToGameBtn"),
		"battle_settings_main_menu_btn": _optional("BattleSettingsMenu/VBox/Buttons/ReturnToMainMenuBtn"),
	}
