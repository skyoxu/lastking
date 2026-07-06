extends Control

const FORMAL_SCREEN_SIZE: Vector2 = Vector2(1600.0, 900.0)
const GAME_MANAGER_SCRIPT: Script = preload("res://Game.Godot/Scripts/Runtime/GameManager.cs")

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
@onready var _battle_settings_menu: Control = get_node_or_null("BattleSettingsMenu")
@onready var _combat_debug_geometry_controller: Node = $CombatDebugGeometryController
@onready var _battle_hud: Control = get_node_or_null("BattleHud")

var _path_points: PackedVector2Array = PackedVector2Array(
	[
		Vector2(96, 312),
		Vector2(1488, 312),
	]
)
var _resume_speed_scale_percent: int = 100
var _resume_was_paused: bool = false
var _battle_runtime_ready: bool = false
var _post_place_probe_frames: int = 0

func _ready() -> void:
	_initialize_battle_screen()

func _initialize_battle_screen() -> void:
	if _debug_component_enabled("runtime_singletons"):
		_ensure_runtime_singletons()
	if _debug_component_enabled("formal_screen_frame"):
		_apply_formal_screen_frame()
	if _debug_component_enabled("suspend_main_layers"):
		_suspend_main_runtime_layers()
	if _debug_component_enabled("configure_controllers"):
		_configure_controllers()
	_battle_runtime_ready = true
	if _debug_component_enabled("hide_legacy_hud"):
		_hide_legacy_runtime_hud_panels()
	process_mode = Node.PROCESS_MODE_ALWAYS
	if _debug_component_enabled("battle_settings_menu") and _battle_settings_menu != null:
		_configure_battle_settings_menu_runtime()
		_presentation_controller.call("sync_locale_and_texts")
		_battle_settings_menu.visible = false
		var return_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToGameBtn")
		var main_menu_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToMainMenuBtn")
		if return_btn != null and not return_btn.pressed.is_connected(_on_return_to_game_pressed):
			return_btn.pressed.connect(_on_return_to_game_pressed)
		if main_menu_btn != null and not main_menu_btn.pressed.is_connected(_on_return_to_main_menu_pressed):
			main_menu_btn.pressed.connect(_on_return_to_main_menu_pressed)
	if _debug_component_enabled("runtime"):
		_runtime_coordinator.call("initialize_runtime")
	_disable_battle_hud_process_for_isolation()

func _disable_battle_hud_process_for_isolation() -> void:
	if _battle_hud == null:
		return
	_battle_hud.set_process(false)
	_battle_hud.set_physics_process(false)
	_battle_hud.set_process_input(true)
	_battle_hud.set_process_unhandled_input(true)
	_battle_hud.set_process_unhandled_key_input(true)

func _process(delta: float) -> void:
	if _debug_component_enabled("runtime"):
		_runtime_coordinator.call("process_runtime_frame", delta)
	if _debug_component_enabled("debug_geometry"):
		_combat_debug_geometry_controller.call("process_frame", delta)
	_presentation_controller.call("sync_locale_and_texts")
	if _post_place_probe_frames > 0:
		_post_place_probe_frames -= 1

func _exit_tree() -> void:
	_disconnect_battlefield_selection_signals()
	if _operation_controller != null and _operation_controller.has_method("cleanup"):
		_operation_controller.call("cleanup")
	if _outcome_controller != null and _outcome_controller.has_method("cleanup"):
		_outcome_controller.call("cleanup")
	if _combat_debug_geometry_controller != null and _combat_debug_geometry_controller.has_method("cleanup"):
		_combat_debug_geometry_controller.call("cleanup")

func _input(event: InputEvent) -> void:
	_handle_pointer_input(event)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		return

func debug_handle_pointer_input(event: InputEvent) -> void:
	_handle_pointer_input(event)

func _handle_pointer_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		var motion_event: InputEventMouseMotion = event as InputEventMouseMotion
		if _build_placement_controller != null and _build_placement_controller.has_method("sync_drag_pointer"):
			_build_placement_controller.call("sync_drag_pointer", motion_event.position, _slot_id_under_pointer(motion_event.position))
		elif _build_placement_controller != null and _build_placement_controller.has_method("update_drag_pointer_position"):
			_build_placement_controller.call("update_drag_pointer_position", motion_event.position)
	if event is InputEventMouseButton:
		var mouse_event: InputEventMouseButton = event as InputEventMouseButton
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and mouse_event.pressed:
			var pressed_slot_id: String = _slot_id_under_pointer(mouse_event.position)
			if not pressed_slot_id.is_empty():
				var has_active_placement: bool = false
				if _build_placement_controller != null and _build_placement_controller.has_method("has_active_placement"):
					has_active_placement = _build_placement_controller.call("has_active_placement") == true
				if not has_active_placement and _selection_controller != null and _selection_controller.has_method("select_formal_building_slot"):
					_selection_controller.call("select_formal_building_slot", pressed_slot_id)
					get_viewport().set_input_as_handled()
					return
		if mouse_event.button_index == MOUSE_BUTTON_RIGHT and mouse_event.pressed:
			if _build_placement_controller != null and _build_placement_controller.has_method("has_active_placement") and _build_placement_controller.call("has_active_placement") == true:
				_build_placement_controller.call("cancel_active_placement")
				get_viewport().set_input_as_handled()
				return
		if mouse_event.button_index == MOUSE_BUTTON_LEFT and not mouse_event.pressed:
			var hovered_slot_id: String = _slot_id_under_pointer(mouse_event.position)
			if not hovered_slot_id.is_empty():
				if _build_placement_controller != null and _build_placement_controller.has_method("handle_battlefield_slot_released"):
					_build_placement_controller.call("handle_battlefield_slot_released", hovered_slot_id)
			elif _build_placement_controller != null and _build_placement_controller.has_method("handle_pointer_release_without_slot"):
				_build_placement_controller.call("handle_pointer_release_without_slot")

func _debug_component_enabled(component_name: String) -> bool:
	if component_name == "debug_geometry":
		return OS.has_environment("LASTKING_BATTLEMAP_ENABLE_DEBUG_GEOMETRY") and not OS.get_environment("LASTKING_BATTLEMAP_ENABLE_DEBUG_GEOMETRY").strip_edges().is_empty()
	if not OS.has_environment("LASTKING_BATTLEMAP_DISABLE_COMPONENTS"):
		return true
	var raw: String = OS.get_environment("LASTKING_BATTLEMAP_DISABLE_COMPONENTS").strip_edges()
	if raw.is_empty():
		return true
	for token_variant in raw.split(",", false):
		var token: String = str(token_variant).strip_edges().to_lower()
		if token == component_name.to_lower():
			return false
	return true

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
		"battle_settings_menu": _battle_settings_menu,
	})
	_navigation_controller.call("configure", {
		"screen": self,
	})
	if _debug_component_enabled("hud_configure"):
		_hud_coordinator.call("configure", {
			"screen": self,
			"operation_controller": _operation_controller,
			"navigation_controller": _navigation_controller,
			"hud": get_node_or_null("BattleHud"),
			"selection_controller": _selection_controller,
			"build_placement_controller": _build_placement_controller,
		})
	if _debug_component_enabled("runtime_configure"):
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

	if _debug_component_enabled("feedback"):
		_feedback_controller.call("configure", self, _bridge, {
			"status": refs["status"],
			"summary": refs["summary"],
			"background": refs["background"],
			"battlefield_view": get_node_or_null("Background"),
			"enemy_spawn_b": refs["enemy_spawn_b"],
			"local_feedback_layer": refs["local_feedback_layer"],
			"hit_flash_overlay": refs["hit_flash_overlay"],
			"wall_pressure_overlay": refs["wall_pressure_overlay"],
			"wall_hit_flash_overlay": refs["wall_hit_flash_overlay"],
			"wall_crack_overlay": refs["wall_crack_overlay"],
			"wall_damage_layer": refs["wall_damage_layer"],
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
	if _debug_component_enabled("hud"):
		_hud_coordinator.call("connect_signals")
	else:
		_hud_coordinator.call("configure", {
			"screen": self,
			"operation_controller": _operation_controller,
			"navigation_controller": _navigation_controller,
			"hud": get_node_or_null("BattleHud"),
			"selection_controller": _selection_controller,
			"build_placement_controller": _build_placement_controller,
		})

	if _debug_component_enabled("selection"):
		_selection_controller.call("configure", self, {
			"presentation_controller": _presentation_controller,
			"formal_selection_data_provider": _selection_data_provider,
		})
	if _debug_component_enabled("build_placement"):
		_build_placement_controller.call("configure", {
			"screen": self,
			"selection_controller": _selection_controller,
			"feedback_controller": _feedback_controller,
			"selection_data_provider": _selection_data_provider,
			"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
			"battlefield_view": get_node_or_null("Background"),
			"translate": Callable(_presentation_controller, "translate"),
		})
	if _debug_component_enabled("debug_geometry"):
		_combat_debug_geometry_controller.call("configure", {
		"screen": self,
		"bridge_provider": Callable(_bridge_provider, "resolve_current_bridge"),
		"battlefield_view": get_node_or_null("Background"),
		"map_marker_layer": refs["map_marker_layer"],
		"path_line": get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/Path"),
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

	var hidden_paths: Array = [
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
	var disabled_button_paths: Array = [
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

func _ensure_runtime_singletons() -> void:
	var manager: Node = get_node_or_null("/root/GameManager")
	if manager == null and GAME_MANAGER_SCRIPT != null:
		manager = GAME_MANAGER_SCRIPT.new()
		manager.name = "GameManager"
		var root: Window = get_tree().root
		if root != null:
			root.add_child(manager)

func _resolve_main_root() -> Node:
	var current: Node = self
	while current != null:
		if str(current.name) == "Main":
			return current
		current = current.get_parent()
	return null

func _slot_id_under_pointer(screen_position: Vector2) -> String:
	var battlefield_view: Node = get_node_or_null("Background")
	if battlefield_view != null and battlefield_view.has_method("get_slot_id_at_screen_position"):
		return str(battlefield_view.call("get_slot_id_at_screen_position", screen_position))
	return ""

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

func _disconnect_battlefield_selection_signals() -> void:
	var battlefield_view: Node = get_node_or_null("Background")
	if battlefield_view == null:
		return
	var clicked_callable: Callable = Callable(self, "_on_battlefield_slot_clicked")
	if battlefield_view.has_signal("battlefield_slot_clicked") and battlefield_view.is_connected("battlefield_slot_clicked", clicked_callable):
		battlefield_view.disconnect("battlefield_slot_clicked", clicked_callable)
	var hovered_callable: Callable = Callable(self, "_on_battlefield_slot_hovered")
	if battlefield_view.has_signal("battlefield_slot_hovered") and battlefield_view.is_connected("battlefield_slot_hovered", hovered_callable):
		battlefield_view.disconnect("battlefield_slot_hovered", hovered_callable)
	var released_callable: Callable = Callable(self, "_on_battlefield_slot_released")
	if battlefield_view.has_signal("battlefield_slot_released") and battlefield_view.is_connected("battlefield_slot_released", released_callable):
		battlefield_view.disconnect("battlefield_slot_released", released_callable)

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
	_presentation_controller.call("sync_locale_and_texts")
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
	_battle_settings_menu.mouse_filter = Control.MOUSE_FILTER_STOP
	_battle_settings_menu.top_level = true
	_battle_settings_menu.z_index = 500
	var backdrop: Control = _battle_settings_menu.get_node_or_null("Backdrop")
	if backdrop != null:
		backdrop.process_mode = Node.PROCESS_MODE_ALWAYS
		backdrop.mouse_filter = Control.MOUSE_FILTER_STOP
	var vbox: Control = _battle_settings_menu.get_node_or_null("VBox")
	if vbox != null:
		vbox.process_mode = Node.PROCESS_MODE_ALWAYS
		vbox.mouse_filter = Control.MOUSE_FILTER_PASS
	var panel: Control = _battle_settings_menu.get_node_or_null("VBox/Panel")
	if panel != null:
		panel.process_mode = Node.PROCESS_MODE_ALWAYS
		panel.mouse_filter = Control.MOUSE_FILTER_PASS
		panel.clip_contents = true
	var return_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToGameBtn")
	if return_btn != null:
		return_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	var main_menu_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToMainMenuBtn")
	if main_menu_btn != null:
		main_menu_btn.process_mode = Node.PROCESS_MODE_ALWAYS
	var buttons_box: Control = _battle_settings_menu.get_node_or_null("VBox/Buttons")
	if buttons_box != null:
		buttons_box.process_mode = Node.PROCESS_MODE_ALWAYS
		buttons_box.mouse_filter = Control.MOUSE_FILTER_PASS
	var title: Control = _battle_settings_menu.get_node_or_null("VBox/Title")
	if title != null:
		title.process_mode = Node.PROCESS_MODE_ALWAYS
	var settings_content: Control = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsContent")
	if settings_content != null:
		settings_content.process_mode = Node.PROCESS_MODE_ALWAYS
		settings_content.mouse_filter = Control.MOUSE_FILTER_PASS

func is_battle_runtime_ready() -> bool:
	return _battle_runtime_ready

func debug_arm_post_place_probe() -> void:
	_post_place_probe_frames = 4

