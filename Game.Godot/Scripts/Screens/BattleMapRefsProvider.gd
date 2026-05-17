extends Node

var _screen: Control = null

func configure(refs: Dictionary) -> void:
	_screen = refs["screen"]

func build_refs() -> Dictionary:
	return {
		"title": _screen.get_node("LegacyPrototypeRoot/VBox/Title"),
		"status": _screen.get_node("LegacyPrototypeRoot/VBox/Status"),
		"summary": _screen.get_node("LegacyPrototypeRoot/VBox/Summary"),
		"background": _screen.get_node("Background"),
		"enemy_spawn_a": _screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnA"),
		"enemy_spawn_b": _screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/EnemySpawnB"),
		"local_feedback_layer": _screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer"),
		"hit_flash_overlay": _screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/HitFlashOverlay"),
		"wall_pressure_overlay": _screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/WallPressureOverlay"),
		"local_prompt_panel": _screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel"),
		"local_prompt_label": _screen.get_node("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/LocalPromptPanel/PromptLabel"),
		"daily_settlement_modal": _screen.get_node("DailySettlementModal"),
		"daily_settlement_title": _screen.get_node("DailySettlementModal/VBox/Title"),
		"daily_settlement_summary": _screen.get_node("DailySettlementModal/VBox/Summary"),
		"daily_evidence_title": _screen.get_node("DailySettlementModal/VBox/EvidencePanel/EvidenceTitle"),
		"daily_evidence_hp": _screen.get_node("DailySettlementModal/VBox/EvidencePanel/HpEvidence"),
		"daily_evidence_kills": _screen.get_node("DailySettlementModal/VBox/EvidencePanel/KillEvidence"),
		"daily_evidence_reward_summary": _screen.get_node("DailySettlementModal/VBox/EvidencePanel/RewardSummaryEvidence"),
		"daily_evidence_resources": _screen.get_node("DailySettlementModal/VBox/EvidencePanel/ResourceEvidence"),
		"daily_evidence_defeat_reason": _screen.get_node("DailySettlementModal/VBox/EvidencePanel/DefeatReasonEvidence"),
		"daily_expand_context_btn": _screen.get_node("DailySettlementModal/VBox/EvidencePanel/ExpandContextBtn"),
		"daily_runtime_context_payload": _screen.get_node("DailySettlementModal/VBox/EvidencePanel/RuntimeContextPayload"),
		"daily_reward_a": _screen.get_node("DailySettlementModal/VBox/Rewards/RewardA"),
		"daily_reward_b": _screen.get_node("DailySettlementModal/VBox/Rewards/RewardB"),
		"daily_reward_c": _screen.get_node("DailySettlementModal/VBox/Rewards/RewardC"),
		"daily_hint": _screen.get_node("DailySettlementModal/VBox/Hint"),
		"victory_outcome_modal": _screen.get_node("VictoryOutcomeModal"),
		"victory_outcome_title": _screen.get_node("VictoryOutcomeModal/VBox/Title"),
		"victory_outcome_summary": _screen.get_node("VictoryOutcomeModal/VBox/Summary"),
		"victory_outcome_hint": _screen.get_node("VictoryOutcomeModal/VBox/Hint"),
		"victory_return_btn": _screen.get_node("VictoryOutcomeModal/VBox/Actions/ReturnToMainMenuBtn"),
		"victory_restart_btn": _screen.get_node("VictoryOutcomeModal/VBox/Actions/RestartBtn"),
		"defeat_outcome_modal": _screen.get_node("DefeatOutcomeModal"),
		"defeat_outcome_title": _screen.get_node("DefeatOutcomeModal/VBox/Title"),
		"defeat_outcome_summary": _screen.get_node("DefeatOutcomeModal/VBox/Summary"),
		"defeat_outcome_hint": _screen.get_node("DefeatOutcomeModal/VBox/Hint"),
		"defeat_return_btn": _screen.get_node("DefeatOutcomeModal/VBox/Actions/ReturnToMainMenuBtn"),
		"defeat_restart_btn": _screen.get_node("DefeatOutcomeModal/VBox/Actions/RestartBtn"),
		"battle_settings_title": _screen.get_node("BattleSettingsMenu/VBox/Title"),
		"battle_settings_return_btn": _screen.get_node("BattleSettingsMenu/VBox/Buttons/ReturnToGameBtn"),
		"battle_settings_main_menu_btn": _screen.get_node("BattleSettingsMenu/VBox/Buttons/ReturnToMainMenuBtn"),
	}
