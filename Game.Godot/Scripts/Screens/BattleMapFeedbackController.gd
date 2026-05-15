extends Node

var _screen: Control = null
var _bridge: Node = null
var _bridge_provider: Callable
var _status_label: Label = null
var _summary_label: Label = null
var _background: ColorRect = null
var _enemy_spawn_a: ColorRect = null
var _enemy_spawn_b: ColorRect = null
var _local_feedback_layer: Control = null
var _hit_flash_overlay: ColorRect = null
var _wall_pressure_overlay: ColorRect = null
var _local_prompt_panel: Control = null
var _local_prompt_label: Label = null
var _enemy_tokens := {}
var _path_points: PackedVector2Array = PackedVector2Array()
var _spawn_pulse_time_left := 0.0
var _hit_flash_time_left := 0.0
var _wall_pressure_time_left := 0.0
var _prompt_time_left := 0.0
var _last_status_text := ""
var _last_prompt_text := ""
var _translate: Callable

const HIT_FLASH_DURATION_SEC := 0.65
const WALL_PRESSURE_DURATION_SEC := 2.4
const PROMPT_DURATION_SEC := 2.8

func configure(screen: Control, bridge: Node, refs: Dictionary) -> void:
	_screen = screen
	_bridge = bridge
	_bridge_provider = refs["bridge_provider"]
	_status_label = refs["status"]
	_summary_label = refs["summary"]
	_background = refs["background"]
	_enemy_spawn_a = refs["enemy_spawn_a"]
	_enemy_spawn_b = refs["enemy_spawn_b"]
	_local_feedback_layer = refs["local_feedback_layer"]
	_hit_flash_overlay = refs["hit_flash_overlay"]
	_wall_pressure_overlay = refs["wall_pressure_overlay"]
	_local_prompt_panel = refs["local_prompt_panel"]
	_local_prompt_label = refs["local_prompt_label"]
	_path_points = refs["path_points"]
	_translate = refs["translate"]
	_apply_local_feedback_visuals()

func process_frame(delta: float) -> void:
	_update_spawn_cues(delta)
	_update_local_feedback(delta)
	_render_actor_tokens()

func mark_spawn_pulse(duration_sec: float) -> void:
	_spawn_pulse_time_left = duration_sec
	_apply_spawn_cues()
	show_local_prompt(_t("battlemap.status.wave_spawned"), PROMPT_DURATION_SEC)

func clear_spawn_pulse() -> void:
	_spawn_pulse_time_left = 0.0
	_apply_spawn_cues()

func render_summary(result: Dictionary, status_text: String) -> void:
	_status_label.text = status_text
	_summary_label.text = _t("battlemap.summary.title")
	_last_status_text = status_text
	_sync_local_feedback_from_summary(result, status_text)

func render_loaded_summary(status_text: String) -> void:
	render_summary(_try_bridge_summary(), status_text)

func show_local_prompt(text: String, duration_sec: float = PROMPT_DURATION_SEC) -> void:
	if _local_prompt_panel == null or _local_prompt_label == null:
		return
	_last_prompt_text = text
	_local_prompt_label.text = text
	_prompt_time_left = maxf(duration_sec, 0.0)
	_apply_local_feedback_visuals()

func clear_local_prompt() -> void:
	_prompt_time_left = 0.0
	_last_prompt_text = ""
	if _local_prompt_label != null:
		_local_prompt_label.text = ""
	_apply_local_feedback_visuals()

func _render_actor_tokens() -> void:
	var bridge := _current_bridge()
	if bridge == null or not bridge.has_method("GetActorSnapshots"):
		return
	var snapshots: Array = bridge.call("GetActorSnapshots")
	var alive_names := {}
	for item in snapshots:
		var d := item as Dictionary
		if d == null:
			continue
		if not bool(d.get("is_moving_enemy", false)):
			continue
		var actor_name := String(d.get("name", ""))
		if actor_name.is_empty():
			continue
		if not bool(d.get("active", false)):
			continue
		alive_names[actor_name] = true
		var token: ColorRect = _enemy_tokens.get(actor_name, null)
		if token == null:
			token = ColorRect.new()
			token.color = Color(0.95, 0.35, 0.35, 1.0)
			token.custom_minimum_size = Vector2(14, 14)
			token.size = Vector2(14, 14)
			_background.add_child(token)
			_enemy_tokens[actor_name] = token
		token.position = _sample_path(float(d.get("path_progress", 0.0)))

	for key in _enemy_tokens.keys():
		if alive_names.has(key):
			continue
		var stale: ColorRect = _enemy_tokens[key]
		if stale != null and is_instance_valid(stale):
			stale.queue_free()
		_enemy_tokens.erase(key)

func _update_spawn_cues(delta: float) -> void:
	if _spawn_pulse_time_left > 0.0:
		_spawn_pulse_time_left = maxf(0.0, _spawn_pulse_time_left - delta)
	_apply_spawn_cues()

func _update_local_feedback(delta: float) -> void:
	var visuals_changed := false
	if _hit_flash_time_left > 0.0:
		_hit_flash_time_left = maxf(0.0, _hit_flash_time_left - delta)
		visuals_changed = true
	if _wall_pressure_time_left > 0.0:
		_wall_pressure_time_left = maxf(0.0, _wall_pressure_time_left - delta)
		visuals_changed = true
	if _prompt_time_left > 0.0:
		_prompt_time_left = maxf(0.0, _prompt_time_left - delta)
		if _prompt_time_left <= 0.0 and _local_prompt_label != null:
			_local_prompt_label.text = ""
			_last_prompt_text = ""
		visuals_changed = true
	if visuals_changed:
		_apply_local_feedback_visuals()

func _apply_spawn_cues() -> void:
	var pulse_active := _spawn_pulse_time_left > 0.0
	var weak_color := Color(0.847059, 0.286275, 0.286275, 0.55)
	var pulse_color := Color(0.996078, 0.505882, 0.505882, 0.95)
	var spawn_color := pulse_color if pulse_active else weak_color
	_enemy_spawn_a.color = spawn_color
	_enemy_spawn_b.color = spawn_color

func _sync_local_feedback_from_summary(result: Dictionary, status_text: String) -> void:
	var castle_hp := int(result.get("castle_hp", 100))
	var wall_hp := int(result.get("wall_hp", 20))
	var exchanges := int(result.get("combat_exchanges", 0))
	var enemies := int(result.get("enemy_units_spawned", 0))
	var defeat_reason := String(result.get("defeat_reason", ""))
	var status_lower := status_text.to_lower()

	if exchanges > 0 or defeat_reason == "castle_destroyed":
		_hit_flash_time_left = HIT_FLASH_DURATION_SEC

	if wall_hp <= 15 or defeat_reason == "wall_breached":
		_wall_pressure_time_left = WALL_PRESSURE_DURATION_SEC

	if defeat_reason == "wall_breached":
		show_local_prompt("Wall under attack. Reinforce immediately.", PROMPT_DURATION_SEC)
	elif defeat_reason == "castle_destroyed":
		show_local_prompt("Castle collapsing. Combat lost.", PROMPT_DURATION_SEC)
	elif status_lower.find("cleanup") >= 0:
		show_local_prompt("Cleanup required before battle can finish.", PROMPT_DURATION_SEC)
	elif status_lower.find("wave") >= 0 and enemies > 0:
		show_local_prompt("Wave entered battlefield. Hold the line.", PROMPT_DURATION_SEC)
	elif status_lower.find("finished") >= 0:
		show_local_prompt("Battle resolved. Review outcome.", PROMPT_DURATION_SEC)
	elif castle_hp <= 50 and wall_hp <= 15:
		show_local_prompt("Reinforce frontline before the wall breaks.", PROMPT_DURATION_SEC)
	elif _prompt_time_left <= 0.0:
		clear_local_prompt()

	_apply_local_feedback_visuals()

func _apply_local_feedback_visuals() -> void:
	if _local_feedback_layer != null:
		_local_feedback_layer.visible = true
	if _hit_flash_overlay != null:
		_hit_flash_overlay.visible = _hit_flash_time_left > 0.0
		if _hit_flash_overlay.visible:
			var flash_alpha := clampf(_hit_flash_time_left / HIT_FLASH_DURATION_SEC, 0.15, 1.0) * 0.22
			_hit_flash_overlay.color = Color(1.0, 0.560784, 0.403922, flash_alpha)
	if _wall_pressure_overlay != null:
		_wall_pressure_overlay.visible = _wall_pressure_time_left > 0.0
		if _wall_pressure_overlay.visible:
			var pressure_alpha := clampf(_wall_pressure_time_left / WALL_PRESSURE_DURATION_SEC, 0.2, 1.0) * 0.16
			_wall_pressure_overlay.color = Color(0.901961, 0.309804, 0.25098, pressure_alpha)
	if _local_prompt_panel != null:
		_local_prompt_panel.visible = _prompt_time_left > 0.0 and _local_prompt_label != null and not _local_prompt_label.text.is_empty()

func _sample_path(progress: float) -> Vector2:
	var p := clampf(progress, 0.0, 1.0)
	var rev: PackedVector2Array = PackedVector2Array()
	for i in range(_path_points.size() - 1, -1, -1):
		rev.append(_path_points[i])
	var seg_count := rev.size() - 1
	if seg_count <= 0:
		return Vector2.ZERO
	var scaled := p * float(seg_count)
	var idx := mini(int(floor(scaled)), seg_count - 1)
	var local_t := scaled - float(idx)
	return rev[idx].lerp(rev[idx + 1], local_t)

func _t(key: String) -> String:
	if _translate.is_valid():
		return String(_translate.call(key))
	return key

func _try_bridge_summary() -> Dictionary:
	var bridge := _current_bridge()
	if bridge == null:
		return {}
	if bridge.has_method("GetSummary"):
		var result = bridge.call("GetSummary")
		if result is Dictionary:
			return result
	if bridge.has_method("RunCompleteCombatExperienceForTest"):
		var full = bridge.call("RunCompleteCombatExperienceForTest")
		if full is Dictionary:
			return full
	return {}

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided = _bridge_provider.call()
		if provided is Node:
			return provided
	return _bridge
