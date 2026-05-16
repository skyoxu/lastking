extends Control

const FORMAL_SCREEN_SIZE := Vector2(1600.0, 900.0)

@onready var _bridge: Node = $CombatExperienceRuntimeBridge
@onready var _wave_timer: Timer = $WaveTimer
@onready var _operation_controller: Node = $OperationController
@onready var _feedback_controller: Node = $FeedbackController
@onready var _outcome_controller: Node = $OutcomeController
@onready var _selection_controller: Node = $SelectionController
@onready var _presentation_controller: Node = $PresentationController
@onready var _navigation_controller: Node = $NavigationController
@onready var _runtime_coordinator: Node = $RuntimeCoordinator
@onready var _refs_provider: Node = $RefsProvider
@onready var _ownership_coordinator: Node = $OwnershipCoordinator
@onready var _bridge_provider: Node = $BridgeProvider
@onready var _hud_coordinator: Node = $HudCoordinator

var _path_points: PackedVector2Array = PackedVector2Array(
	[Vector2(120, 120), Vector2(260, 120), Vector2(420, 240), Vector2(640, 240), Vector2(840, 340), Vector2(1080, 340)]
)

func _ready() -> void:
	_apply_formal_screen_frame()
	_configure_controllers()
	_hide_legacy_runtime_hud_panels()
	_runtime_coordinator.call("initialize_runtime")

func _process(delta: float) -> void:
	_runtime_coordinator.call("process_runtime_frame", delta)

func _configure_controllers() -> void:
	_refs_provider.call("configure", {
		"screen": self,
	})
	_bridge_provider.call("configure", {
		"screen": self,
	})
	var refs: Dictionary = _refs_provider.call("build_refs")
	_ownership_coordinator.call("configure", {
		"background": refs["background"],
		"bridge": _bridge,
		"wave_timer": _wave_timer,
		"margin": get_node("Margin"),
	})
	_ownership_coordinator.call("apply_ownership_markers")
	_presentation_controller.call("configure", {
		"title": refs["title"],
		"build_btn": refs["build_btn"],
		"wave_btn": refs["wave_btn"],
		"auto_wave_btn": refs["auto_wave_btn"],
		"exchange_btn": refs["exchange_btn"],
		"cleanup_btn": refs["cleanup_btn"],
		"finish_btn": refs["finish_btn"],
		"back_btn": refs["back_btn"],
		"legend": refs["legend"],
		"metrics_help": refs["metrics_help"],
		"operation_controller": _operation_controller,
	})
	_navigation_controller.call("configure", {
		"screen": self,
		"back_btn": refs["back_btn"],
	})
	_hud_coordinator.call("configure", {
		"screen": self,
		"operation_controller": _operation_controller,
		"navigation_controller": _navigation_controller,
		"hud": _resolve_runtime_ui_node("HUD"),
	})
	_runtime_coordinator.call("configure", {
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
		"presentation_controller": _presentation_controller,
		"outcome_controller": _outcome_controller,
		"feedback_controller": _feedback_controller,
		"operation_controller": _operation_controller,
		"runtime_coordinator": _runtime_coordinator,
	})

	_feedback_controller.call("configure", self, _bridge, {
		"status": refs["status"],
		"summary": refs["summary"],
		"background": refs["background"],
		"enemy_spawn_a": refs["enemy_spawn_a"],
		"enemy_spawn_b": refs["enemy_spawn_b"],
		"local_feedback_layer": refs["local_feedback_layer"],
		"hit_flash_overlay": refs["hit_flash_overlay"],
		"wall_pressure_overlay": refs["wall_pressure_overlay"],
		"local_prompt_panel": refs["local_prompt_panel"],
		"local_prompt_label": refs["local_prompt_label"],
		"path_points": _path_points,
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
		"translate": Callable(_presentation_controller, "translate"),
	})

	_outcome_controller.call("configure", self, {
		"status": refs["status"],
		"wave_timer": _wave_timer,
		"auto_wave_btn": refs["auto_wave_btn"],
		"translate": Callable(_presentation_controller, "translate"),
		"back_callback": Callable(_navigation_controller, "navigate_back_to_main_menu"),
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
		"operation_controller": _operation_controller,
		"feedback_controller": _feedback_controller,
		"navigation_controller": _navigation_controller,
		"daily_settlement_modal": refs["daily_settlement_modal"],
		"daily_settlement_summary": refs["daily_settlement_summary"],
		"daily_evidence_hp": refs["daily_evidence_hp"],
		"daily_evidence_kills": refs["daily_evidence_kills"],
		"daily_evidence_reward_summary": refs["daily_evidence_reward_summary"],
		"daily_evidence_resources": refs["daily_evidence_resources"],
		"daily_evidence_defeat_reason": refs["daily_evidence_defeat_reason"],
		"daily_expand_context_btn": refs["daily_expand_context_btn"],
		"daily_runtime_context_payload": refs["daily_runtime_context_payload"],
		"daily_reward_a": refs["daily_reward_a"],
		"daily_reward_b": refs["daily_reward_b"],
		"daily_reward_c": refs["daily_reward_c"],
		"victory_outcome_modal": refs["victory_outcome_modal"],
		"victory_outcome_title": refs["victory_outcome_title"],
		"victory_outcome_summary": refs["victory_outcome_summary"],
		"victory_outcome_hint": refs["victory_outcome_hint"],
		"victory_return_btn": refs["victory_return_btn"],
		"victory_restart_btn": refs["victory_restart_btn"],
		"defeat_outcome_modal": refs["defeat_outcome_modal"],
		"defeat_outcome_title": refs["defeat_outcome_title"],
		"defeat_outcome_summary": refs["defeat_outcome_summary"],
		"defeat_outcome_hint": refs["defeat_outcome_hint"],
		"defeat_return_btn": refs["defeat_return_btn"],
		"defeat_restart_btn": refs["defeat_restart_btn"],
	})
	_outcome_controller.call("set_process_modes")
	_outcome_controller.call("connect_signals")

	_operation_controller.call("configure", {
		"bridge": _bridge,
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
		"wave_timer": _wave_timer,
		"status": refs["status"],
		"translate": Callable(_presentation_controller, "translate"),
		"feedback_controller": _feedback_controller,
		"outcome_controller": _outcome_controller,
		"build_btn": refs["build_btn"],
		"wave_btn": refs["wave_btn"],
		"auto_wave_btn": refs["auto_wave_btn"],
		"exchange_btn": refs["exchange_btn"],
		"cleanup_btn": refs["cleanup_btn"],
		"finish_btn": refs["finish_btn"],
	})
	_operation_controller.call("connect_signals")
	_navigation_controller.call("connect_signals")
	_hud_coordinator.call("connect_signals")

	_selection_controller.call("configure", self, {
		"presentation_controller": _presentation_controller,
	})

func _hide_legacy_runtime_hud_panels() -> void:
	var huds := []
	var local_hud := get_node_or_null("BattleHud")
	if local_hud != null:
		huds.append(local_hud)
	var global_hud := _resolve_runtime_ui_node("HUD")
	if global_hud != null:
		huds.append(global_hud)

	var hidden_paths := [
		"FeedbackLayer/PressurePanel",
		"FeedbackLayer/CameraControlOverlay",
		"FeedbackLayer/ConfigAuditPanel",
		"FeedbackLayer/MigrationStatusDialog",
		"FeedbackLayer/ReportMetadataPanel",
		"FeedbackLayer/OutcomePanel",
		"FeedbackLayer/RuntimePromptPanel",
		"FeedbackLayer/ResourcePanel",
		"FeedbackLayer/BuildPanel",
		"FeedbackLayer/ProgressionPanel",
	]
	var disabled_button_paths := [
		"FeedbackLayer/ConfigAuditPanel/VBox/RefreshButton",
		"FeedbackLayer/MigrationStatusDialog/VBox/RetryButton",
	]

	for hud in huds:
		for path in hidden_paths:
			var node = hud.get_node_or_null(path)
			if node is CanvasItem:
				(node as CanvasItem).visible = false
		for path in disabled_button_paths:
			var node = hud.get_node_or_null(path)
			if node is BaseButton:
				(node as BaseButton).disabled = true

func _resolve_runtime_ui_node(relative_path: String) -> Node:
	var main := _resolve_main_root()
	if main != null:
		return main.get_node_or_null("RuntimeUi/%s" % relative_path)
	return null

func _apply_formal_screen_frame() -> void:
	custom_minimum_size = FORMAL_SCREEN_SIZE
	var main := _resolve_main_root()
	if main == null:
		anchor_right = 0.0
		anchor_bottom = 0.0
		position = Vector2.ZERO
		size = FORMAL_SCREEN_SIZE
	return

func _resolve_main_root() -> Node:
	var current: Node = self
	while current != null:
		if String(current.name) == "Main":
			return current
		current = current.get_parent()
	return null
