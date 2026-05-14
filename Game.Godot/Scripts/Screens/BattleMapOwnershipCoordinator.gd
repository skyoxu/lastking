extends Node

var _background: Node = null
var _bridge: Node = null
var _wave_timer: Timer = null
var _margin: Node = null

func configure(refs: Dictionary) -> void:
	_background = refs["background"]
	_bridge = refs["bridge"]
	_wave_timer = refs["wave_timer"]
	_margin = refs["margin"]

func apply_ownership_markers() -> void:
	_background.set_meta("ownership_container", "battlefield_presentation")
	_bridge.set_meta("ownership_container", "runtime_bridge")
	_wave_timer.set_meta("ownership_container", "runtime_bridge")
	_margin.set_meta("ownership_container", "legacy_prototype")
	_margin.set_meta("migration_only", true)
