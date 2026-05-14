extends Node

const DEFAULT_OVERLAY_CONTROLLER_NAME := "UI_PlacementOverlayController"

var _screen: Control = null

func configure(screen: Control, _refs: Dictionary = {}) -> void:
	_screen = screen

func register_overlay_controller(path: NodePath, controller: Node) -> void:
	if _screen == null:
		return
	var node_name := String(path.get_concatenated_names()).replace("/", "_")
	var existing := _screen.get_node_or_null(NodePath(node_name))
	if existing != null and existing != controller:
		_screen.remove_child(existing)
		existing.queue_free()
	if controller.get_parent() != null and controller.get_parent() != _screen:
		controller.get_parent().remove_child(controller)
	controller.name = node_name
	_screen.add_child(controller)

func get_overlay_controller() -> Node:
	if _screen == null:
		return null
	return _screen.get_node_or_null(NodePath(DEFAULT_OVERLAY_CONTROLLER_NAME))

func apply_legality_overlay(legality_by_slot: Dictionary) -> void:
	var controller := get_overlay_controller()
	if controller != null and controller.has_method("apply_legality_overlay"):
		controller.call("apply_legality_overlay", legality_by_slot)

func read_slot_visual(slot_id: String) -> Dictionary:
	var controller := get_overlay_controller()
	if controller != null and controller.has_method("read_slot_visual"):
		var result = controller.call("read_slot_visual", slot_id)
		if result is Dictionary:
			return (result as Dictionary).duplicate(true)
	return {}

func set_placement_context_active(active: bool) -> void:
	var controller := get_overlay_controller()
	if controller != null and controller.has_method("set_placement_context_active"):
		controller.call("set_placement_context_active", active)
