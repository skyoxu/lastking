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
@onready var _selection_data_provider: Node = $SelectionDataProvider
@onready var _build_placement_controller: Node = $BuildPlacementController
@onready var _day_night_loop: Node = $DayNightRuntimeLoop
@onready var _battle_settings_menu: Control = $BattleSettingsMenu

var _path_points: PackedVector2Array = PackedVector2Array(
	[Vector2(120, 120), Vector2(260, 120), Vector2(420, 240), Vector2(640, 240), Vector2(840, 340), Vector2(1080, 340)]
)
var _resume_speed_scale_percent: int = 100
var _resume_was_paused: bool = false
var _last_locale: String = ""

func _ready() -> void:
	_apply_formal_screen_frame()
	_suspend_main_runtime_layers()
	_configure_controllers()
	_hide_legacy_runtime_hud_panels()
	process_mode = Node.PROCESS_MODE_ALWAYS
	if _battle_settings_menu != null:
		_configure_battle_settings_menu_runtime()
		_apply_battle_settings_texts()
		_battle_settings_menu.visible = false
		var return_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToGameBtn")
		var main_menu_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToMainMenuBtn")
		var close_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsPanel/VBox/Buttons/CloseBtn")
		if return_btn != null and not return_btn.pressed.is_connected(_on_return_to_game_pressed):
			return_btn.pressed.connect(_on_return_to_game_pressed)
		if main_menu_btn != null and not main_menu_btn.pressed.is_connected(_on_return_to_main_menu_pressed):
			main_menu_btn.pressed.connect(_on_return_to_main_menu_pressed)
		if close_btn != null:
			close_btn.visible = false
			close_btn.disabled = true
	_runtime_coordinator.call("initialize_runtime")

func _process(delta: float) -> void:
	_runtime_coordinator.call("process_runtime_frame", delta)
	_sync_scene_locale_texts()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		var motion_event := event as InputEventMouseMotion
		if _build_placement_controller != null and _build_placement_controller.has_method("update_drag_pointer_position"):
			_build_placement_controller.call("update_drag_pointer_position", motion_event.position)
	if event is InputEventMouseButton:
		var mouse_event := event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and not mouse_event.pressed:
			if _build_placement_controller != null and _build_placement_controller.has_method("handle_pointer_release_without_slot"):
				_build_placement_controller.call("handle_pointer_release_without_slot")

func _configure_controllers() -> void:
	_refs_provider.call("configure", {
		"screen": self,
	})
	_bridge_provider.call("configure", {
		"screen": self,
	})
	_selection_data_provider.call("configure", {
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
	})
	var refs: Dictionary = _refs_provider.call("build_refs")
	_ownership_coordinator.call("configure", {
		"background": refs["background"],
		"bridge": _bridge,
		"wave_timer": _wave_timer,
		"margin": get_node("LegacyPrototypeRoot"),
	})
	_ownership_coordinator.call("apply_ownership_markers")
	_presentation_controller.call("configure", {
		"title": refs["title"],
	})
	_navigation_controller.call("configure", {
		"screen": self,
	})
	_hud_coordinator.call("configure", {
		"screen": self,
		"operation_controller": _operation_controller,
		"navigation_controller": _navigation_controller,
		"hud": get_node_or_null("BattleHud"),
		"selection_controller": _selection_controller,
		"build_placement_controller": _build_placement_controller,
	})
	_runtime_coordinator.call("configure", {
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
		"presentation_controller": _presentation_controller,
		"outcome_controller": _outcome_controller,
		"feedback_controller": _feedback_controller,
		"operation_controller": _operation_controller,
		"runtime_coordinator": _runtime_coordinator,
		"screen": self,
		"hud": get_node_or_null("BattleHud"),
		"day_night_loop": _day_night_loop,
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
		"translate": Callable(_presentation_controller, "translate"),
		"back_callback": Callable(_navigation_controller, "navigate_back_to_main_menu"),
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
		"operation_controller": _operation_controller,
		"feedback_controller": _feedback_controller,
		"navigation_controller": _navigation_controller,
		"daily_settlement_modal": refs["daily_settlement_modal"],
		"daily_settlement_title": refs["daily_settlement_title"],
		"daily_settlement_summary": refs["daily_settlement_summary"],
		"daily_evidence_title": refs["daily_evidence_title"],
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
		"daily_hint": refs["daily_hint"],
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
		"battle_settings_title": refs["battle_settings_title"],
		"battle_settings_return_btn": refs["battle_settings_return_btn"],
		"battle_settings_main_menu_btn": refs["battle_settings_main_menu_btn"],
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
	})
	_operation_controller.call("connect_signals")
	_navigation_controller.call("connect_signals")
	_hud_coordinator.call("connect_signals")

	_selection_controller.call("configure", self, {
		"presentation_controller": _presentation_controller,
		"formal_selection_data_provider": _selection_data_provider,
	})
	_build_placement_controller.call("configure", {
		"screen": self,
		"selection_controller": _selection_controller,
		"feedback_controller": _feedback_controller,
		"selection_data_provider": _selection_data_provider,
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
		"battlefield_view": get_node_or_null("Background"),
		"translate": Callable(_presentation_controller, "translate"),
	})
	_connect_battlefield_selection_signals()

func _hide_legacy_runtime_hud_panels() -> void:
	var huds: Array = []
	var local_hud: Node = get_node_or_null("BattleHud")
	if local_hud != null:
		huds.append(local_hud)
	var global_hud: Node = _resolve_runtime_ui_node("HUD")
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
	var main: Node = _resolve_main_root()
	if main != null:
		return main.get_node_or_null("RuntimeUi/%s" % relative_path)
	return null

func _suspend_main_runtime_layers() -> void:
	var main_menu: Control = _resolve_runtime_ui_node("MainMenu")
	if main_menu != null:
		main_menu.visible = false
		main_menu.mouse_filter = Control.MOUSE_FILTER_IGNORE

func _apply_formal_screen_frame() -> void:
	custom_minimum_size = FORMAL_SCREEN_SIZE
	var main: Node = _resolve_main_root()
	if main == null:
		anchor_right = 0.0
		anchor_bottom = 0.0
		position = Vector2.ZERO
		return
	anchor_right = 1.0
	anchor_bottom = 1.0
	position = Vector2.ZERO

func _resolve_main_root() -> Node:
	var current: Node = self
	while current != null:
		if str(current.name) == "Main":
			return current
		current = current.get_parent()
	return null

func _connect_battlefield_selection_signals() -> void:
	if not (_selection_controller != null and _selection_controller.has_method("select_formal_building_slot")):
		return
	var battlefield_view: Node = get_node_or_null("Background")
	if battlefield_view == null:
		return
	var clicked_callable: Callable = Callable(self, "_on_battlefield_slot_clicked")
	if battlefield_view.has_signal("battlefield_slot_clicked") and not battlefield_view.is_connected("battlefield_slot_clicked", clicked_callable):
		battlefield_view.connect("battlefield_slot_clicked", clicked_callable)
	var hovered_callable: Callable = Callable(self, "_on_battlefield_slot_hovered")
	if battlefield_view.has_signal("battlefield_slot_hovered") and not battlefield_view.is_connected("battlefield_slot_hovered", hovered_callable):
		battlefield_view.connect("battlefield_slot_hovered", hovered_callable)
	var released_callable: Callable = Callable(self, "_on_battlefield_slot_released")
	if battlefield_view.has_signal("battlefield_slot_released") and not battlefield_view.is_connected("battlefield_slot_released", released_callable):
		battlefield_view.connect("battlefield_slot_released", released_callable)

func _on_battlefield_slot_clicked(slot_id: String) -> void:
	if _build_placement_controller != null and _build_placement_controller.has_method("handle_battlefield_slot_clicked"):
		var handled: Variant = _build_placement_controller.call("handle_battlefield_slot_clicked", slot_id)
		if handled == true:
			return
	if _selection_controller != null and _selection_controller.has_method("select_formal_building_slot"):
		_selection_controller.call("select_formal_building_slot", slot_id)

func _on_battlefield_slot_hovered(slot_id: String) -> void:
	if _build_placement_controller != null and _build_placement_controller.has_method("handle_battlefield_slot_hovered"):
		_build_placement_controller.call("handle_battlefield_slot_hovered", slot_id)

func _on_battlefield_slot_released(slot_id: String) -> void:
	if _build_placement_controller != null and _build_placement_controller.has_method("handle_battlefield_slot_released"):
		_build_placement_controller.call("handle_battlefield_slot_released", slot_id)

func open_battle_settings_menu() -> void:
	_apply_battle_settings_texts()
	var manager: Node = get_node_or_null("/root/GameManager")
	if manager != null and manager.has_method("GetSpeedState"):
		var state: Variant = manager.call("GetSpeedState")
		if state is Dictionary:
			_resume_speed_scale_percent = int((state as Dictionary).get("scale_percent", 100))
			_resume_was_paused = (state as Dictionary).get("is_paused", false) == true
	if manager != null and manager.has_method("SetPause") and not _resume_was_paused:
		manager.call("SetPause")
	if _battle_settings_menu != null:
		_battle_settings_menu.visible = true

func close_battle_settings_menu() -> void:
	if _battle_settings_menu != null:
		_battle_settings_menu.visible = false

func _on_return_to_game_pressed() -> void:
	close_battle_settings_menu()
	var manager: Node = get_node_or_null("/root/GameManager")
	if manager == null:
		return
	if _resume_was_paused:
		if manager.has_method("SetPause"):
			manager.call("SetPause")
		return
	if _resume_speed_scale_percent >= 200:
		if manager.has_method("SetTwoX"):
			manager.call("SetTwoX")
	elif manager.has_method("SetOneX"):
		manager.call("SetOneX")

func _on_return_to_main_menu_pressed() -> void:
	close_battle_settings_menu()
	var manager: Node = get_node_or_null("/root/GameManager")
	if manager != null and manager.has_method("SetOneX"):
		manager.call("SetOneX")
	_navigation_controller.call("navigate_back_to_main_menu")

func _configure_battle_settings_menu_runtime() -> void:
	_battle_settings_menu.process_mode = Node.PROCESS_MODE_ALWAYS
	var backdrop: Control = _battle_settings_menu.get_node_or_null("Backdrop")
	if backdrop != null:
		backdrop.process_mode = Node.PROCESS_MODE_ALWAYS
	var vbox: Control = _battle_settings_menu.get_node_or_null("VBox")
	if vbox != null:
		vbox.process_mode = Node.PROCESS_MODE_ALWAYS
	var return_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToGameBtn")
	if return_btn != null:
		return_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	var main_menu_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToMainMenuBtn")
	if main_menu_btn != null:
		main_menu_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	var settings_panel: Node = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsPanel")
	if settings_panel != null:
		settings_panel.process_mode = Node.PROCESS_MODE_ALWAYS

func _sync_scene_locale_texts() -> void:
	var locale: String = _normalize_locale(str(TranslationServer.get_locale()))
	if locale == _last_locale:
		return
	_last_locale = locale
	_apply_battle_settings_texts()

func _apply_battle_settings_texts() -> void:
	if _battle_settings_menu == null:
		return
	var title: Label = _battle_settings_menu.get_node_or_null("VBox/Title")
	if title != null:
		title.text = _presentation_controller.call("translate", "battlemap.settings.title")
	var return_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToGameBtn")
	if return_btn != null:
		return_btn.text = _presentation_controller.call("translate", "battlemap.settings.return_to_game")
	var main_menu_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToMainMenuBtn")
	if main_menu_btn != null:
		main_menu_btn.text = _presentation_controller.call("translate", "battlemap.settings.return_to_main_menu")

func _normalize_locale(locale: String) -> String:
	var v: String = locale.strip_edges().to_lower()
	if v == "zh" or v.begins_with("zh"):
		return "zh-CN"
	return "en-US"

