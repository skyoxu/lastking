extends Node

var _screen: Control = null
var _hud: Node = null
var _operation_controller: Node = null
var _navigation_controller: Node = null
var _selection_controller: Node = null

func configure(refs: Dictionary) -> void:
	_screen = refs.get("screen", null)
	_operation_controller = refs.get("operation_controller", null)
	_navigation_controller = refs.get("navigation_controller", null)
	_hud = refs.get("hud", null)
	_selection_controller = refs.get("selection_controller", null)

func connect_signals() -> void:
	_hud = _resolve_hud()
	if _hud == null:
		return
	_set_hud_active(_hud, true)
	var global_hud: Node = _resolve_runtime_ui_node("HUD")
	if global_hud != null and global_hud != _hud:
		_set_hud_active(global_hud, false)

func route_action(action_code: String) -> void:
	match action_code:
		"select_tower":
			if _selection_controller != null and _selection_controller.has_method("apply_building_selection"):
				_selection_controller.call("apply_building_selection", {
					"selection_id": "tower_alpha",
					"category": "defense",
					"building_slots": ["InnerCastleRegionSlot_03_00"],
					"range_slots": ["InnerCastleRegionSlot_04_00"],
					"blocked_range_slots": ["InnerCastleRegionSlot_05_00"],
					"linked_unit_slots": ["LeftOuterFieldSlot_01_00"],
				})
		"select_residence":
			if _selection_controller != null and _selection_controller.has_method("apply_building_selection"):
				_selection_controller.call("apply_building_selection", {
					"selection_id": "farm_alpha",
					"category": "economy",
					"building_slots": ["InnerCastleRegionSlot_06_00"],
					"range_slots": [],
					"blocked_range_slots": [],
					"linked_unit_slots": [],
				})
		"build":
			if _operation_controller != null:
				_operation_controller.call("on_build")
		"wave":
			if _operation_controller != null:
				_operation_controller.call("on_wave")
		"exchange":
			if _operation_controller != null:
				_operation_controller.call("on_exchange")
		"cleanup":
			if _operation_controller != null:
				_operation_controller.call("on_cleanup")
		"finish":
			if _operation_controller != null:
				_operation_controller.call("on_finish")
		"back":
			if _navigation_controller != null:
				_navigation_controller.call("navigate_back_to_main_menu")
		"open_settings":
			if _screen != null and _screen.has_method("open_battle_settings_menu"):
				_screen.call("open_battle_settings_menu")

func _resolve_hud() -> Node:
	if _screen == null:
		return null
	var local_hud: Node = _screen.get_node_or_null("BattleHud")
	if local_hud != null:
		return local_hud
	return _resolve_runtime_ui_node("HUD")

func _resolve_runtime_ui_node(relative_path: String) -> Node:
	if _screen == null:
		return null
	var current: Node = _screen
	while current != null:
		if str(current.name) == "Main":
			return current.get_node_or_null("RuntimeUi/%s" % relative_path)
		current = current.get_parent()
	return null

func _set_hud_active(hud: Node, active: bool) -> void:
	if hud == null or not (hud is CanvasItem):
		return
	if hud.has_method("SetBattleHudActive"):
		hud.call("SetBattleHudActive", active)
	(hud as CanvasItem).visible = active
	if hud.has_node("TopBar"):
		var top_bar: Node = hud.get_node("TopBar")
		if top_bar is CanvasItem:
			(top_bar as CanvasItem).visible = active
	if hud.has_node("CombatHud/BottomBar"):
		var bottom_bar: Node = hud.get_node("CombatHud/BottomBar")
		if bottom_bar is CanvasItem:
			(bottom_bar as CanvasItem).visible = active
	if hud.has_node("FeedbackLayer"):
		var feedback_layer: Node = hud.get_node("FeedbackLayer")
		if feedback_layer is CanvasItem:
			(feedback_layer as CanvasItem).visible = active
	if hud is Control:
		(hud as Control).mouse_filter = Control.MOUSE_FILTER_PASS if active else Control.MOUSE_FILTER_IGNORE


