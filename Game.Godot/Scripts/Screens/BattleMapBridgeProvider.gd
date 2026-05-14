extends Node

var _screen: Control = null

func configure(refs: Dictionary) -> void:
	_screen = refs["screen"]

func resolve_current_bridge() -> Node:
	if _screen == null:
		return null
	var current = _screen.get("_bridge")
	if current is Node:
		return current
	return null
