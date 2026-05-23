extends Node

var _screen: Control = null
var _bridge: Node = null
var _bridge_provider: Callable
var _status_label: Label = null
var _summary_label: Label = null
var _background: ColorRect = null
var _map_marker_layer: Control = null
var _battlefield_view: Node = null
var _enemy_spawn_a: ColorRect = null
var _enemy_spawn_b: ColorRect = null
var _local_feedback_layer: Control = null
var _hit_flash_overlay: ColorRect = null
var _wall_pressure_overlay: ColorRect = null
var _wall_hit_flash_overlay: Control = null
var _wall_crack_overlay: Control = null
var _wall_damage_layer: Control = null
var _local_prompt_panel: Control = null
var _local_prompt_label: Label = null
var _enemy_tokens: Dictionary = {}
var _wall_damage_numbers: Array = []
var _left_crack_tiles_root: Control = null
var _right_crack_tiles_root: Control = null
var _left_crack_tiles: Array[TextureRect] = []
var _right_crack_tiles: Array[TextureRect] = []
var _path_points: PackedVector2Array = PackedVector2Array()
var _spawn_pulse_time_left: float = 0.0
var _hit_flash_time_left: float = 0.0
var _wall_pressure_time_left: float = 0.0
var _wall_hit_flash_time_left: float = 0.0
var _prompt_time_left: float = 0.0
var _wall_feedback_initialized: bool = false
var _last_wall_hp: int = 100
var _current_wall_hp: int = 100
var _last_wall_hit_on_left: bool = true
var _last_status_text: String = ""
var _last_prompt_text: String = ""
var _translate: Callable
var _battle_hud: Node = null
var _building_visual_root: Control = null
var _building_visuals: Dictionary = {}
var _tower_debug_labels: Dictionary = {}
var _attack_effect_root: Control = null
var _attack_traces: Array = []
var _last_projectiles_created: int = 0
var _last_enemy_hp_by_name: Dictionary = {}
var _enemy_token_states: Dictionary = {}
var _building_texture_cache: Dictionary = {}
var _attack_effects_spawned_total: int = 0
var _enemy_sprite_sheet_textures: Dictionary = {}
var _projectile_texture: Texture2D = null
var _impact_texture: Texture2D = null
var _muzzle_flash_texture: Texture2D = null
var _feedback_ready: bool = false

const HIT_FLASH_DURATION_SEC := 0.65
const WALL_PRESSURE_DURATION_SEC := 2.4
const WALL_HIT_FLASH_DURATION_SEC := 0.42
const WALL_DAMAGE_NUMBER_DURATION_SEC := 0.9
const PROMPT_DURATION_SEC := 2.8
const LEFT_CRACK_TEXTURE_PATH := "res://Game.Godot/Assets/Textures/BattleMapFeedback/battlemap_wall_crack_left.png"
const RIGHT_CRACK_TEXTURE_PATH := "res://Game.Godot/Assets/Textures/BattleMapFeedback/battlemap_wall_crack_right.png"
const CRACK_TILE_COUNT := 13
const CRACK_TILE_SIZE := Vector2i(48, 48)
const CRACK_REVEAL_ORDER := [6, 5, 7, 4, 8, 3, 9, 2, 10, 1, 11, 0, 12]
const BUILDING_VISUAL_TEXTURES := {
	"MgTower": "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_tower.png",
	"SniperTower": "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_tower.png",
	"Barracks": "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_barracks.png",
	"Residence": "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_residence.png",
}
const DEFAULT_BUILDING_SLOT_IDS := {
	"MgTower": "InnerCastleRegionSlot_03_00",
	"SniperTower": "InnerCastleRegionSlot_06_05",
	"Barracks": "InnerCastleRegionSlot_00_00",
	"Residence": "InnerCastleRegionSlot_06_00",
}
const BUILDING_VISUAL_SIZE := Vector2(48.0, 48.0)
const ATTACK_TRACE_DURATION_SEC := 0.18
const HIT_SPARK_DURATION_SEC := 0.22
const ENEMY_HIT_FLASH_DURATION_SEC := 0.18
const ENEMY_DEATH_FADE_DURATION_SEC := 0.32
const ENEMY_TOKEN_SIZE := Vector2(16.0, 16.0)
const ENEMY_SPRITE_SHEET_TEXTURE_PATHS := {
	"grunt": "res://Game.Godot/Assets/Textures/BattleMapFeedback/battlemap_enemy_grunt_walk_sheet.png",
	"elite": "res://Game.Godot/Assets/Textures/BattleMapFeedback/battlemap_enemy_elite_walk_sheet.png",
	"boss": "res://Game.Godot/Assets/Textures/BattleMapFeedback/battlemap_enemy_elite_walk_sheet.png",
}
const ENEMY_SPRITE_FRAME_SIZE := Vector2(48.0, 48.0)
const ENEMY_SPRITE_FRAME_COUNT := 4
const ENEMY_WALK_FPS := 8.0
const PROJECTILE_TEXTURE_PATH := "res://Game.Godot/Assets/Textures/BattleMapFeedback/battlemap_tower_projectile.png"
const PROJECTILE_TRAVEL_DURATION_SEC := 0.22
const IMPACT_TEXTURE_PATH := "res://Game.Godot/Assets/Textures/BattleMapFeedback/battlemap_tower_impact.png"
const IMPACT_DURATION_SEC := 0.18
const MUZZLE_FLASH_TEXTURE_PATH := "res://Game.Godot/Assets/Textures/BattleMapFeedback/battlemap_tower_muzzle_flash.png"
const MUZZLE_FLASH_DURATION_SEC := 0.12

func configure(screen: Control, bridge: Node, refs: Dictionary) -> void:
	_screen = screen
	_bridge = bridge
	_bridge_provider = refs["bridge_provider"]
	_status_label = refs["status"]
	_summary_label = refs["summary"]
	_background = refs["background"]
	_map_marker_layer = refs.get("map_marker_layer", null)
	_battlefield_view = refs.get("battlefield_view", null)
	_enemy_spawn_a = refs["enemy_spawn_a"]
	_enemy_spawn_b = refs["enemy_spawn_b"]
	_local_feedback_layer = refs["local_feedback_layer"]
	_hit_flash_overlay = refs["hit_flash_overlay"]
	_wall_pressure_overlay = refs["wall_pressure_overlay"]
	_wall_hit_flash_overlay = refs.get("wall_hit_flash_overlay", null)
	_wall_crack_overlay = refs.get("wall_crack_overlay", null)
	_wall_damage_layer = refs.get("wall_damage_layer", null)
	_local_prompt_panel = refs["local_prompt_panel"]
	_local_prompt_label = refs["local_prompt_label"]
	_path_points = refs["path_points"]
	_translate = refs["translate"]
	_battle_hud = _screen.get_node_or_null("BattleHud")
	_feedback_ready = _has_required_refs()
	if not _feedback_ready:
		return
	_ensure_building_visual_root()
	_ensure_attack_effect_root()
	_prepare_wall_crack_tiles()
	_apply_local_feedback_visuals()

func is_configured() -> bool:
	return _feedback_ready

func process_frame(delta: float) -> void:
	if not _feedback_ready:
		return
	_ensure_building_visual_root()
	_ensure_attack_effect_root()
	_update_spawn_cues(delta)
	_sync_runtime_wall_feedback()
	_sync_combat_visual_feedback()
	_update_local_feedback(delta)
	_update_attack_effects(delta)
	_update_enemy_token_effects(delta)
	_render_building_visuals()
	_render_actor_tokens()
	_render_tower_debug_labels()

func reset_runtime_state() -> void:
	_last_projectiles_created = 0
	_last_enemy_hp_by_name.clear()
	_attack_effects_spawned_total = 0
	_spawn_pulse_time_left = 0.0
	_hit_flash_time_left = 0.0
	_wall_pressure_time_left = 0.0
	_wall_hit_flash_time_left = 0.0
	_prompt_time_left = 0.0
	_last_prompt_text = ""
	_last_status_text = ""
	for actor_name_variant in _enemy_tokens.keys():
		var actor_name := str(actor_name_variant)
		var token: CanvasItem = _enemy_tokens.get(actor_name, null)
		if token != null and is_instance_valid(token):
			token.queue_free()
	_enemy_tokens.clear()
	_enemy_token_states.clear()
	for building_name_variant in _building_visuals.keys():
		var building_name := str(building_name_variant)
		var visual: CanvasItem = _building_visuals.get(building_name, null)
		if visual != null and is_instance_valid(visual):
			visual.queue_free()
	_building_visuals.clear()
	for building_name_variant in _tower_debug_labels.keys():
		var building_name := str(building_name_variant)
		var label: CanvasItem = _tower_debug_labels.get(building_name, null)
		if label != null and is_instance_valid(label):
			label.queue_free()
	_tower_debug_labels.clear()
	for entry_variant in _attack_traces:
		var entry := entry_variant as Dictionary
		if entry == null:
			continue
		var node: CanvasItem = entry.get("node", null)
		if node != null and is_instance_valid(node):
			node.queue_free()
	_attack_traces.clear()
	clear_local_prompt()

func mark_spawn_pulse(duration_sec: float) -> void:
	if not _feedback_ready:
		return
	_spawn_pulse_time_left = duration_sec
	_apply_spawn_cues()
	show_local_prompt(_t("battlemap.status.wave_spawned"), PROMPT_DURATION_SEC)

func clear_spawn_pulse() -> void:
	if not _feedback_ready:
		return
	_spawn_pulse_time_left = 0.0
	_apply_spawn_cues()

func render_summary(result: Dictionary, status_text: String) -> void:
	if not _feedback_ready:
		return
	_status_label.text = status_text
	_summary_label.text = _compose_formal_summary(result)
	_push_formal_battle_hud_messages(result, status_text)
	_last_status_text = status_text
	_sync_local_feedback_from_summary(result, status_text)

func render_loaded_summary(status_text: String) -> void:
	if not _feedback_ready:
		return
	render_summary(_try_bridge_summary(), status_text)

func show_local_prompt(text: String, duration_sec: float = PROMPT_DURATION_SEC) -> void:
	if not _feedback_ready:
		return
	if _local_prompt_panel == null or _local_prompt_label == null:
		return
	_last_prompt_text = text
	_local_prompt_label.text = text
	_prompt_time_left = maxf(duration_sec, 0.0)
	_apply_local_feedback_visuals()

func clear_local_prompt() -> void:
	if not _feedback_ready:
		return
	_prompt_time_left = 0.0
	_last_prompt_text = ""
	if _local_prompt_label != null:
		_local_prompt_label.text = ""
	_apply_local_feedback_visuals()

func render_status_only(status_text: String) -> void:
	if not _feedback_ready:
		return
	_status_label.text = status_text
	_last_status_text = status_text

func _render_actor_tokens() -> void:
	var bridge: Node = _current_bridge()
	if bridge == null or not bridge.has_method("GetActorSnapshots"):
		return
	var snapshots: Array = bridge.call("GetActorSnapshots")
	var alive_names: Dictionary = {}
	var visible_enemy_names: Dictionary = {}
	for item in snapshots:
		var d: Dictionary = item as Dictionary
		if d == null:
			continue
		if d.get("is_moving_enemy", false) != true:
			continue
		var actor_name: String = str(d.get("name", ""))
		if actor_name.is_empty():
			continue
		visible_enemy_names[actor_name] = true
		if d.get("active", false) != true:
			_mark_enemy_token_dying(actor_name)
			continue
		alive_names[actor_name] = true
		var token: ColorRect = _enemy_tokens.get(actor_name, null)
		if token == null:
			token = ColorRect.new()
			token.color = Color(0.95, 0.25, 0.25, 0.95)
			token.custom_minimum_size = ENEMY_TOKEN_SIZE
			token.size = ENEMY_TOKEN_SIZE
			token.mouse_filter = Control.MOUSE_FILTER_IGNORE
			token.z_index = 8
			var token_parent: Node = _map_marker_layer if _map_marker_layer != null else _background
			token_parent.add_child(token)
			var sprite := TextureRect.new()
			sprite.name = "Sprite"
			sprite.set_anchors_preset(Control.PRESET_FULL_RECT)
			sprite.mouse_filter = Control.MOUSE_FILTER_IGNORE
			sprite.stretch_mode = TextureRect.STRETCH_SCALE
			sprite.texture = AtlasTexture.new()
			token.add_child(sprite)
			_enemy_tokens[actor_name] = token
			_enemy_token_states[actor_name] = {
				"hit_flash": 0.0,
				"death_fade": -1.0,
				"dying": false,
				"walk_phase": 0.0,
				"frame_index": 0,
				"visual_tier": "grunt",
				"sprite_sheet_path": str(ENEMY_SPRITE_SHEET_TEXTURE_PATHS.get("grunt", "")),
			}
		var world_x: float = float(d.get("world_x", 0.0))
		var world_y: float = float(d.get("world_y", 0.0))
		token.position = Vector2(world_x - (token.size.x * 0.5), world_y - (token.size.y * 0.5))
		var state := _enemy_token_states.get(actor_name, {}) as Dictionary
		var visual_tier := _resolve_visual_tier(d)
		state["visual_tier"] = visual_tier
		state["sprite_sheet_path"] = _resolve_enemy_sprite_sheet_path(visual_tier)
		_enemy_token_states[actor_name] = state
		_apply_enemy_token_visual(actor_name, token)

	for key in _enemy_tokens.keys():
		if visible_enemy_names.has(key):
			continue
		_mark_enemy_token_dying(str(key))

func _render_building_visuals() -> void:
	if _building_visual_root == null:
		return
	var bridge: Node = _current_bridge()
	var active_names: Dictionary = {}
	if bridge != null and bridge.has_method("GetPlacedBuildingSlots"):
		var placed_slots: Variant = bridge.call("GetPlacedBuildingSlots")
		var placed_node_names: Dictionary = {}
		if bridge.has_method("GetPlacedBuildingNodeNames"):
			var placed_node_names_variant: Variant = bridge.call("GetPlacedBuildingNodeNames")
			if placed_node_names_variant is Dictionary:
				placed_node_names = placed_node_names_variant as Dictionary
		if placed_slots is Dictionary:
			for slot_id_variant in (placed_slots as Dictionary).keys():
				var slot_id := str(slot_id_variant)
				var selection_id := str((placed_slots as Dictionary)[slot_id_variant])
				var building_name := str(placed_node_names.get(slot_id, ""))
				if building_name.is_empty():
					building_name = _building_node_name_for_selection(selection_id, slot_id)
				if building_name.is_empty():
					continue
				active_names[building_name] = true
				var visual := _ensure_building_visual(building_name, selection_id)
				var center := _resolve_slot_center(slot_id)
				visual.position = center - (BUILDING_VISUAL_SIZE / 2.0)
	else:
		for building_name_variant in BUILDING_VISUAL_TEXTURES.keys():
			var building_name := str(building_name_variant)
			if bridge == null or not bridge.has_node("Battlefield/%s" % building_name):
				continue
			active_names[building_name] = true
			var visual := _ensure_building_visual(building_name, _selection_id_for_building_name(building_name))
			var center := _resolve_building_center(building_name)
			visual.position = center - (BUILDING_VISUAL_SIZE / 2.0)
	for building_name_variant in _building_visuals.keys():
		var stale_name := str(building_name_variant)
		if active_names.has(stale_name):
			continue
		var stale_visual: CanvasItem = _building_visuals[stale_name]
		if stale_visual != null and is_instance_valid(stale_visual):
			stale_visual.queue_free()
		_building_visuals.erase(stale_name)
		var stale_label: Label = _tower_debug_labels.get(stale_name, null)
		if stale_label != null and is_instance_valid(stale_label):
			stale_label.queue_free()
		_tower_debug_labels.erase(stale_name)

func _sync_combat_visual_feedback() -> void:
	var bridge: Node = _current_bridge()
	if bridge == null or not bridge.has_method("GetSummary"):
		_last_projectiles_created = 0
		_last_enemy_hp_by_name.clear()
		return
	var summary: Dictionary = bridge.call("GetSummary")
	var projectiles_created: int = int(summary.get("projectiles_created", 0))
	var snapshots: Array = bridge.call("GetActorSnapshots") if bridge.has_method("GetActorSnapshots") else []
	if bridge.has_method("ConsumeTowerShotEvents"):
		var shot_events_variant: Variant = bridge.call("ConsumeTowerShotEvents")
		if shot_events_variant is Array:
			for shot_event_variant in shot_events_variant:
				var shot_event := shot_event_variant as Dictionary
				if shot_event == null:
					continue
				var origin := Vector2(float(shot_event.get("source_x", 0.0)), float(shot_event.get("source_y", 0.0)))
				var target := Vector2(float(shot_event.get("target_x", 0.0)), float(shot_event.get("target_y", 0.0)))
				_spawn_attack_trace(origin, target)
	elif projectiles_created > _last_projectiles_created:
		var closest_target := _find_closest_enemy_snapshot_to_tower(snapshots)
		if not closest_target.is_empty():
			_spawn_attack_trace(_resolve_building_center("MgTower"), Vector2(float(closest_target.get("world_x", 0.0)), float(closest_target.get("world_y", 0.0))))
	for item in snapshots:
		var snapshot := item as Dictionary
		if snapshot == null:
			continue
		var actor_name := str(snapshot.get("name", ""))
		if actor_name.is_empty() or snapshot.get("is_moving_enemy", false) != true:
			continue
		var current_hp := int(snapshot.get("hp", 0))
		var previous_hp := int(_last_enemy_hp_by_name.get(actor_name, current_hp))
		if current_hp < previous_hp:
			_mark_enemy_token_hit(actor_name)
			_spawn_hit_spark(Vector2(float(snapshot.get("world_x", 0.0)), float(snapshot.get("world_y", 0.0))), previous_hp - current_hp)
		_last_enemy_hp_by_name[actor_name] = current_hp
	var active_enemy_names: Dictionary = {}
	for item in snapshots:
		var snapshot := item as Dictionary
		if snapshot == null:
			continue
		var actor_name := str(snapshot.get("name", ""))
		if actor_name.is_empty() or snapshot.get("is_moving_enemy", false) != true:
			continue
		active_enemy_names[actor_name] = true
	for actor_name_variant in _last_enemy_hp_by_name.keys():
		var actor_name := str(actor_name_variant)
		if active_enemy_names.has(actor_name):
			continue
		_last_enemy_hp_by_name.erase(actor_name)
	_last_projectiles_created = projectiles_created

func _update_spawn_cues(delta: float) -> void:
	if _spawn_pulse_time_left > 0.0:
		_spawn_pulse_time_left = maxf(0.0, _spawn_pulse_time_left - delta)
	_apply_spawn_cues()

func _update_local_feedback(delta: float) -> void:
	var visuals_changed: bool = false
	if _hit_flash_time_left > 0.0:
		_hit_flash_time_left = maxf(0.0, _hit_flash_time_left - delta)
		visuals_changed = true
	if _wall_pressure_time_left > 0.0:
		_wall_pressure_time_left = maxf(0.0, _wall_pressure_time_left - delta)
		visuals_changed = true
	if _wall_hit_flash_time_left > 0.0:
		_wall_hit_flash_time_left = maxf(0.0, _wall_hit_flash_time_left - delta)
		visuals_changed = true
	if _prompt_time_left > 0.0:
		_prompt_time_left = maxf(0.0, _prompt_time_left - delta)
		if _prompt_time_left <= 0.0 and _local_prompt_label != null:
			_local_prompt_label.text = ""
			_last_prompt_text = ""
		visuals_changed = true
	if _update_wall_damage_numbers(delta):
		visuals_changed = true
	if visuals_changed:
		_apply_local_feedback_visuals()

func _update_attack_effects(delta: float) -> void:
	if _attack_traces.is_empty():
		return
	for index in range(_attack_traces.size() - 1, -1, -1):
		var entry: Dictionary = _attack_traces[index]
		var node: CanvasItem = entry.get("node", null)
		if node == null or not is_instance_valid(node):
			_attack_traces.remove_at(index)
			continue
		var next_life: float = float(entry.get("life", 0.0)) - delta
		if next_life <= 0.0:
			node.queue_free()
			_attack_traces.remove_at(index)
			continue
		entry["life"] = next_life
		var duration: float = float(entry.get("duration", ATTACK_TRACE_DURATION_SEC))
		var alpha: float = clampf(next_life / maxf(duration, 0.001), 0.0, 1.0)
		node.modulate = Color(1.0, 1.0, 1.0, alpha)
		if node is Label:
			node.position = Vector2(entry.get("origin", Vector2.ZERO)) + (Vector2(0.0, -20.0) * (1.0 - alpha))
		elif node is TextureRect:
			var progress: float = 1.0 - (next_life / maxf(duration, 0.001))
			var effect_kind: String = str(entry.get("kind", "projectile"))
			if effect_kind == "impact":
				var impact_origin: Vector2 = entry.get("origin", Vector2.ZERO)
				node.position = impact_origin - (node.size / 2.0)
				node.scale = Vector2.ONE * lerpf(0.72, 1.18, progress)
			elif effect_kind == "muzzle_flash":
				var flash_origin: Vector2 = entry.get("origin", Vector2.ZERO)
				node.position = flash_origin - (node.size / 2.0)
				node.scale = Vector2.ONE * lerpf(0.86, 1.22, progress)
			else:
				var origin: Vector2 = entry.get("origin", Vector2.ZERO)
				var target: Vector2 = entry.get("target", origin)
				node.position = origin.lerp(target, progress) - (node.size / 2.0)
				node.scale = Vector2.ONE * lerpf(0.88, 1.05, progress)
		_attack_traces[index] = entry

func _apply_spawn_cues() -> void:
	if not _feedback_ready or _enemy_spawn_a == null or _enemy_spawn_b == null:
		return
	var pulse_active: bool = _spawn_pulse_time_left > 0.0
	var weak_color: Color = Color(0.231373, 0.0862745, 0.0862745, 0.18)
	var pulse_color: Color = Color(0.913725, 0.345098, 0.345098, 0.92)
	var spawn_color: Color = pulse_color if pulse_active else weak_color
	_enemy_spawn_a.color = spawn_color
	_enemy_spawn_b.color = spawn_color

func _sync_local_feedback_from_summary(result: Dictionary, status_text: String) -> void:
	if not _feedback_ready or _status_label == null or _summary_label == null:
		return
	var castle_hp: int = int(result.get("castle_hp", 100))
	var wall_hp: int = int(result.get("wall_hp", 100))
	var exchanges: int = int(result.get("combat_exchanges", 0))
	var enemies: int = int(result.get("enemy_units_spawned", 0))
	var defeat_reason: String = str(result.get("defeat_reason", ""))
	var status_lower: String = status_text.to_lower()

	if exchanges > 0 or defeat_reason == "castle_destroyed":
		_hit_flash_time_left = HIT_FLASH_DURATION_SEC

	if wall_hp <= 75 or defeat_reason == "wall_breached":
		_wall_pressure_time_left = WALL_PRESSURE_DURATION_SEC
	_ingest_wall_hp(wall_hp, true)

	if defeat_reason == "wall_breached":
		show_local_prompt(_t("battlemap.prompt.wall_under_attack"), PROMPT_DURATION_SEC)
	elif defeat_reason == "castle_destroyed":
		show_local_prompt(_t("battlemap.prompt.castle_collapsing"), PROMPT_DURATION_SEC)
	elif status_lower.find("cleanup") >= 0:
		show_local_prompt(_t("battlemap.prompt.cleanup_required"), PROMPT_DURATION_SEC)
	elif status_lower.find("wave") >= 0 and enemies > 0:
		show_local_prompt(_t("battlemap.prompt.wave_entered"), PROMPT_DURATION_SEC)
	elif status_lower.find("finished") >= 0:
		show_local_prompt(_t("battlemap.prompt.battle_resolved"), PROMPT_DURATION_SEC)
	elif castle_hp <= 50 and wall_hp <= 75:
		show_local_prompt(_t("battlemap.prompt.reinforce_frontline"), PROMPT_DURATION_SEC)
	elif _prompt_time_left <= 0.0:
		clear_local_prompt()

	_apply_local_feedback_visuals()

func _apply_local_feedback_visuals() -> void:
	if not _feedback_ready:
		return
	if _local_feedback_layer != null:
		_local_feedback_layer.visible = true
	if _hit_flash_overlay != null:
		_hit_flash_overlay.visible = _hit_flash_time_left > 0.0
		if _hit_flash_overlay.visible:
			var flash_alpha: float = clampf(_hit_flash_time_left / HIT_FLASH_DURATION_SEC, 0.15, 1.0) * 0.22
			_hit_flash_overlay.color = Color(1.0, 0.560784, 0.403922, flash_alpha)
	if _wall_pressure_overlay != null:
		_wall_pressure_overlay.visible = _wall_pressure_time_left > 0.0
		if _wall_pressure_overlay.visible:
			var pressure_alpha: float = clampf(_wall_pressure_time_left / WALL_PRESSURE_DURATION_SEC, 0.2, 1.0) * 0.16
			_wall_pressure_overlay.color = Color(0.901961, 0.309804, 0.25098, pressure_alpha)
	if _wall_hit_flash_overlay != null:
		_wall_hit_flash_overlay.visible = _wall_hit_flash_time_left > 0.0
		var wall_flash_alpha: float = clampf(_wall_hit_flash_time_left / WALL_HIT_FLASH_DURATION_SEC, 0.0, 1.0) * 0.82
		for child in _wall_hit_flash_overlay.get_children():
			if child is CanvasItem:
				var child_name: String = str(child.name)
				var is_active_side: bool = (child_name == "LeftFlash" and _last_wall_hit_on_left) or (child_name == "RightFlash" and not _last_wall_hit_on_left)
				(child as CanvasItem).visible = is_active_side and wall_flash_alpha > 0.01
				(child as CanvasItem).modulate = Color(1.0, 1.0, 1.0, wall_flash_alpha if is_active_side else 0.0)
	if _wall_crack_overlay != null:
		var crack_ratio: float = clampf(float(100 - _current_wall_hp) / 100.0, 0.0, 1.0)
		var crack_alpha: float = 0.0
		if crack_ratio > 0.0:
			crack_alpha = lerpf(0.36, 1.0, crack_ratio)
		if _wall_hit_flash_time_left > 0.0:
			crack_alpha = maxf(crack_alpha, 0.72)
		_wall_crack_overlay.visible = crack_alpha > 0.01
		_wall_crack_overlay.modulate = Color(1.0, 1.0, 1.0, crack_alpha)
		_apply_wall_crack_tiles(crack_ratio)
	if _local_prompt_panel != null:
		_local_prompt_panel.visible = _prompt_time_left > 0.0 and _local_prompt_label != null and not _local_prompt_label.text.is_empty()

func _sync_runtime_wall_feedback() -> void:
	var summary: Dictionary = _try_bridge_summary()
	if summary.is_empty():
		return
	_ingest_wall_hp(int(summary.get("wall_hp", 100)), true)

func _ingest_wall_hp(wall_hp: int, spawn_hit_feedback: bool) -> void:
	var clamped_wall_hp: int = clampi(wall_hp, 0, 100)
	_current_wall_hp = clamped_wall_hp
	if not _wall_feedback_initialized:
		_last_wall_hp = clamped_wall_hp
		_wall_feedback_initialized = true
		_apply_local_feedback_visuals()
		return
	if clamped_wall_hp > _last_wall_hp:
		_last_wall_hp = clamped_wall_hp
	elif spawn_hit_feedback and clamped_wall_hp < _last_wall_hp:
		var damage_amount: int = maxi(1, _last_wall_hp - clamped_wall_hp)
		_trigger_wall_damage_feedback(damage_amount)
		_last_wall_hp = clamped_wall_hp
	else:
		_last_wall_hp = clamped_wall_hp
	_apply_local_feedback_visuals()

func _trigger_wall_damage_feedback(damage_amount: int) -> void:
	_last_wall_hit_on_left = _should_render_crack_on_left()
	_wall_hit_flash_time_left = WALL_HIT_FLASH_DURATION_SEC
	_wall_pressure_time_left = maxf(_wall_pressure_time_left, WALL_PRESSURE_DURATION_SEC * 0.45)
	_spawn_wall_damage_number(damage_amount)

func _spawn_wall_damage_number(damage_amount: int) -> void:
	if _wall_damage_layer == null:
		return
	var label := Label.new()
	label.text = "-%d" % damage_amount
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", 28)
	label.modulate = Color(1.0, 0.901961, 0.713725, 1.0)
	label.z_index = 5
	var origin: Vector2 = _resolve_wall_damage_anchor()
	label.position = origin
	_wall_damage_layer.add_child(label)
	_wall_damage_numbers.append({
		"node": label,
		"life": WALL_DAMAGE_NUMBER_DURATION_SEC,
		"origin": origin,
		"drift": Vector2(randf_range(-14.0, 14.0), -68.0),
	})

func _resolve_wall_damage_anchor() -> Vector2:
	var left_anchor := Vector2(600.0, 252.0)
	var right_anchor := Vector2(984.0, 348.0)
	var bridge: Node = _current_bridge()
	if bridge == null or not bridge.has_method("GetActorSnapshots"):
		return left_anchor
	var snapshots: Array = bridge.call("GetActorSnapshots")
	for item in snapshots:
		var snapshot: Dictionary = item as Dictionary
		if snapshot == null:
			continue
		if str(snapshot.get("state", "")) != "attacking_wall":
			continue
		var hit_position: Vector2 = _sample_path(float(snapshot.get("path_progress", 0.0)))
		return left_anchor if hit_position.x < 792.0 else right_anchor
	return left_anchor if _current_wall_hp <= 50 else right_anchor

func _should_render_crack_on_left() -> bool:
	var bridge: Node = _current_bridge()
	if bridge == null or not bridge.has_method("GetActorSnapshots"):
		return _current_wall_hp <= 50
	var snapshots: Array = bridge.call("GetActorSnapshots")
	for item in snapshots:
		var snapshot: Dictionary = item as Dictionary
		if snapshot == null:
			continue
		if str(snapshot.get("state", "")) != "attacking_wall":
			continue
		var hit_position: Vector2 = _sample_path(float(snapshot.get("path_progress", 0.0)))
		return hit_position.x < 792.0
	return _current_wall_hp <= 50

func _update_wall_damage_numbers(delta: float) -> bool:
	if _wall_damage_numbers.is_empty():
		return false
	var changed: bool = false
	for index in range(_wall_damage_numbers.size() - 1, -1, -1):
		var entry: Dictionary = _wall_damage_numbers[index]
		var label: Label = entry.get("node", null)
		if label == null or not is_instance_valid(label):
			_wall_damage_numbers.remove_at(index)
			changed = true
			continue
		var next_life: float = float(entry.get("life", 0.0)) - delta
		if next_life <= 0.0:
			label.queue_free()
			_wall_damage_numbers.remove_at(index)
			changed = true
			continue
		entry["life"] = next_life
		var origin: Vector2 = entry.get("origin", Vector2.ZERO)
		var drift: Vector2 = entry.get("drift", Vector2(0.0, -60.0))
		var progress: float = 1.0 - (next_life / WALL_DAMAGE_NUMBER_DURATION_SEC)
		label.position = origin + (drift * progress)
		label.scale = Vector2.ONE * lerpf(0.92, 1.08, progress)
		label.modulate = Color(1.0, 0.901961, 0.713725, clampf(1.0 - progress, 0.0, 1.0))
		_wall_damage_numbers[index] = entry
		changed = true
	return changed

func _sample_path(progress: float) -> Vector2:
	var p: float = clampf(progress, 0.0, 1.0)
	var rev: PackedVector2Array = PackedVector2Array()
	for i in range(_path_points.size() - 1, -1, -1):
		rev.append(_path_points[i])
	var seg_count: int = rev.size() - 1
	if seg_count <= 0:
		return Vector2.ZERO
	var scaled: float = p * float(seg_count)
	var idx: int = mini(int(floor(scaled)), seg_count - 1)
	var local_t: float = scaled - float(idx)
	return rev[idx].lerp(rev[idx + 1], local_t)

func _prepare_wall_crack_tiles() -> void:
	if not _feedback_ready or _wall_crack_overlay == null:
		return
	_left_crack_tiles_root = _wall_crack_overlay.get_node_or_null("LeftCrackTiles")
	_right_crack_tiles_root = _wall_crack_overlay.get_node_or_null("RightCrackTiles")
	_rebuild_wall_crack_tiles(_left_crack_tiles_root, LEFT_CRACK_TEXTURE_PATH, _left_crack_tiles)
	_rebuild_wall_crack_tiles(_right_crack_tiles_root, RIGHT_CRACK_TEXTURE_PATH, _right_crack_tiles)

func _rebuild_wall_crack_tiles(root: Control, texture_path: String, target_tiles: Array[TextureRect]) -> void:
	target_tiles.clear()
	if root == null:
		return
	for child in root.get_children():
		child.queue_free()
	var textures: Array[Texture2D] = _load_wall_crack_tile_textures(texture_path)
	for row in range(CRACK_TILE_COUNT):
		var tile := TextureRect.new()
		tile.name = "CrackTile_%02d" % row
		tile.position = Vector2(0.0, row * float(CRACK_TILE_SIZE.y))
		tile.size = Vector2(float(CRACK_TILE_SIZE.x), float(CRACK_TILE_SIZE.y))
		tile.mouse_filter = Control.MOUSE_FILTER_IGNORE
		tile.stretch_mode = TextureRect.STRETCH_SCALE
		tile.texture = textures[row] if row < textures.size() else null
		tile.visible = false
		root.add_child(tile)
		target_tiles.append(tile)

func _apply_wall_crack_tiles(crack_ratio: float) -> void:
	var attack_on_left: bool = _should_render_crack_on_left()
	_last_wall_hit_on_left = attack_on_left
	var active_tiles: Array[TextureRect] = _left_crack_tiles if attack_on_left else _right_crack_tiles
	var inactive_tiles: Array[TextureRect] = _right_crack_tiles if attack_on_left else _left_crack_tiles
	var visible_count: int = clampi(int(ceil(crack_ratio * float(CRACK_TILE_COUNT))), 0, CRACK_TILE_COUNT)
	if crack_ratio > 0.0:
		visible_count = maxi(visible_count, 3)
	if _wall_hit_flash_time_left > 0.0 and crack_ratio > 0.0:
		visible_count = maxi(visible_count, 5)
	for tile in inactive_tiles:
		tile.visible = false
	var reveal_lookup: Dictionary = {}
	for reveal_index in range(mini(visible_count, CRACK_REVEAL_ORDER.size())):
		reveal_lookup[CRACK_REVEAL_ORDER[reveal_index]] = reveal_index
	for index in range(active_tiles.size()):
		var tile := active_tiles[index]
		var alpha := 0.0
		if reveal_lookup.has(index):
			var rank: int = int(reveal_lookup[index])
			var rank_fade: float = float(visible_count - rank) / float(maxi(visible_count, 1))
			alpha = 0.68 + minf(0.28, crack_ratio * 0.45) + (rank_fade * 0.12)
		elif _wall_hit_flash_time_left > 0.0 and crack_ratio > 0.0:
			var edge_distance: int = mini(abs(index - 6), 6)
			if edge_distance <= 3:
				alpha = 0.18
		tile.visible = alpha > 0.01
		tile.modulate = Color(1.0, 1.0, 1.0, clampf(alpha, 0.0, 1.0))

func _load_texture_from_file(texture_path: String) -> Texture2D:
	if texture_path.is_empty():
		return null
	if _building_texture_cache.has(texture_path):
		var cached: Variant = _building_texture_cache.get(texture_path, null)
		if cached is Texture2D:
			return cached
	var texture: Texture2D = null
	var image_path := ProjectSettings.globalize_path(texture_path)
	if FileAccess.file_exists(image_path):
		var image := Image.load_from_file(image_path)
		if image != null and not image.is_empty():
			texture = ImageTexture.create_from_image(image)
	if texture == null and ResourceLoader.exists(texture_path):
		texture = load(texture_path) as Texture2D
	if texture == null:
		texture = _create_fallback_building_texture(_preview_kind_for_texture_path(texture_path))
	if texture == null:
		return null
	_building_texture_cache[texture_path] = texture
	return texture

func _ensure_building_visual_root() -> void:
	if not _feedback_ready:
		return
	if _building_visual_root != null and is_instance_valid(_building_visual_root):
		return
	if _screen != null:
		_building_visual_root = _screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/MapMarkerLayer/BuildingVisualLayer")

func _ensure_attack_effect_root() -> void:
	if not _feedback_ready:
		return
	if _attack_effect_root != null and is_instance_valid(_attack_effect_root):
		return
	if _screen != null:
		_attack_effect_root = _screen.get_node_or_null("Background/BattlefieldViewport/BattlefieldRoot/LocalFeedbackLayer/AttackEffectLayer")

func _ensure_building_visual(building_name: String, selection_id: String = "") -> TextureRect:
	var visual := _building_visuals.get(building_name, null) as TextureRect
	if visual != null and is_instance_valid(visual):
		return visual
	visual = TextureRect.new()
	visual.name = "%sVisual" % building_name
	visual.size = BUILDING_VISUAL_SIZE
	visual.mouse_filter = Control.MOUSE_FILTER_IGNORE
	visual.stretch_mode = TextureRect.STRETCH_SCALE
	var texture_path := _building_texture_path_for(selection_id, building_name)
	visual.texture = _load_texture_from_file(texture_path)
	visual.modulate = Color(1.0, 1.0, 1.0, 0.96)
	_building_visual_root.add_child(visual)
	_building_visuals[building_name] = visual
	return visual

func _preview_kind_for_texture_path(texture_path: String) -> String:
	var normalized := texture_path.to_lower()
	if normalized.find("barracks") >= 0:
		return "barracks"
	if normalized.find("residence") >= 0:
		return "residence"
	return "tower"

func _create_fallback_building_texture(preview_kind: String) -> Texture2D:
	var image := Image.create(48, 48, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.094118, 0.12549, 0.164706, 0.92))
	for x in range(48):
		for y in range(48):
			if x <= 1 or x >= 46 or y <= 1 or y >= 46:
				image.set_pixel(x, y, Color(0.905882, 0.807843, 0.603922, 0.92))
	match preview_kind:
		"barracks":
			_fill_rect(image, Rect2i(10, 31, 28, 7), Color(0.384314, 0.282353, 0.219608, 1.0))
			_fill_rect(image, Rect2i(12, 19, 24, 12), Color(0.517647, 0.639216, 0.737255, 1.0))
			_fill_triangle(image, Vector2i(8, 20), Vector2i(24, 9), Vector2i(40, 20), Color(0.721569, 0.32549, 0.286275, 1.0))
			_fill_rect(image, Rect2i(21, 22, 6, 9), Color(0.219608, 0.254902, 0.313726, 1.0))
		"residence":
			_fill_rect(image, Rect2i(11, 31, 26, 7), Color(0.517647, 0.372549, 0.243137, 1.0))
			_fill_rect(image, Rect2i(13, 20, 22, 12), Color(0.839216, 0.760784, 0.603922, 1.0))
			_fill_triangle(image, Vector2i(10, 21), Vector2i(24, 9), Vector2i(38, 21), Color(0.745098, 0.313726, 0.227451, 1.0))
			_fill_rect(image, Rect2i(21, 24, 6, 8), Color(0.4, 0.27451, 0.192157, 1.0))
		_:
			_fill_rect(image, Rect2i(16, 13, 16, 22), Color(0.490196, 0.552941, 0.65098, 1.0))
			_fill_rect(image, Rect2i(13, 34, 22, 6), Color(0.694118, 0.517647, 0.286275, 1.0))
			_fill_triangle(image, Vector2i(13, 16), Vector2i(24, 7), Vector2i(35, 16), Color(0.819608, 0.658824, 0.301961, 1.0))
			_fill_rect(image, Rect2i(22, 22, 4, 8), Color(0.219608, 0.254902, 0.313726, 1.0))
	return ImageTexture.create_from_image(image)

func _fill_rect(image: Image, rect: Rect2i, color: Color) -> void:
	for x in range(rect.position.x, rect.position.x + rect.size.x):
		for y in range(rect.position.y, rect.position.y + rect.size.y):
			if x >= 0 and x < image.get_width() and y >= 0 and y < image.get_height():
				image.set_pixel(x, y, color)

func _fill_triangle(image: Image, a: Vector2i, b: Vector2i, c: Vector2i, color: Color) -> void:
	var min_x := mini(a.x, mini(b.x, c.x))
	var max_x := maxi(a.x, maxi(b.x, c.x))
	var min_y := mini(a.y, mini(b.y, c.y))
	var max_y := maxi(a.y, maxi(b.y, c.y))
	for x in range(min_x, max_x + 1):
		for y in range(min_y, max_y + 1):
			if _point_in_triangle(Vector2(x + 0.5, y + 0.5), Vector2(a), Vector2(b), Vector2(c)):
				if x >= 0 and x < image.get_width() and y >= 0 and y < image.get_height():
					image.set_pixel(x, y, color)

func _point_in_triangle(point: Vector2, a: Vector2, b: Vector2, c: Vector2) -> bool:
	var denominator := ((b.y - c.y) * (a.x - c.x)) + ((c.x - b.x) * (a.y - c.y))
	if is_zero_approx(denominator):
		return false
	var w1 := (((b.y - c.y) * (point.x - c.x)) + ((c.x - b.x) * (point.y - c.y))) / denominator
	var w2 := (((c.y - a.y) * (point.x - c.x)) + ((a.x - c.x) * (point.y - c.y))) / denominator
	var w3 := 1.0 - w1 - w2
	return w1 >= 0.0 and w2 >= 0.0 and w3 >= 0.0

func _resolve_building_center(building_name: String) -> Vector2:
	var slot_id := str(DEFAULT_BUILDING_SLOT_IDS.get(building_name, ""))
	if not slot_id.is_empty() and _battlefield_view != null and _battlefield_view.has_method("get_slot_position"):
		var pos: Variant = _battlefield_view.call("get_slot_position", slot_id)
		if pos is Vector2:
			return (pos as Vector2) + (BUILDING_VISUAL_SIZE / 2.0)
	var bridge: Node = _current_bridge()
	if bridge != null:
		var node := bridge.get_node_or_null("Battlefield/%s" % building_name) as Node2D
		if node != null:
			return node.position
	return Vector2.ZERO

func _resolve_slot_center(slot_id: String) -> Vector2:
	if slot_id.is_empty():
		return Vector2.ZERO
	if _battlefield_view != null and _battlefield_view.has_method("get_slot_position"):
		var pos: Variant = _battlefield_view.call("get_slot_position", slot_id)
		if pos is Vector2:
			return (pos as Vector2) + (BUILDING_VISUAL_SIZE / 2.0)
	return Vector2.ZERO

func _building_node_name_for_selection(selection_id: String, slot_id: String) -> String:
	match selection_id:
		"tower_alpha":
			return "MgTower" if slot_id == "InnerCastleRegionSlot_03_00" else "MgTower_%s" % slot_id
		"tower_beta":
			return "SniperTower" if slot_id == "InnerCastleRegionSlot_06_05" else "SniperTower_%s" % slot_id
		"barracks_alpha":
			return "Barracks"
		"farm_alpha":
			return "Residence"
		_:
			return ""

func _selection_id_for_building_name(building_name: String) -> String:
	if building_name.begins_with("MgTower"):
		return "tower_alpha"
	if building_name.begins_with("SniperTower"):
		return "tower_beta"
	if building_name == "Barracks":
		return "barracks_alpha"
	if building_name == "Residence":
		return "farm_alpha"
	return ""

func _building_texture_path_for(selection_id: String, building_name: String) -> String:
	if selection_id == "tower_alpha" or building_name.begins_with("MgTower"):
		return "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_tower.png"
	if selection_id == "tower_beta" or building_name.begins_with("SniperTower"):
		return "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_tower.png"
	if selection_id == "barracks_alpha" or building_name == "Barracks":
		return "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_barracks.png"
	if selection_id == "farm_alpha" or building_name == "Residence":
		return "res://Game.Godot/Assets/Textures/BattleMapBuildPreviews/battlemap_build_preview_residence.png"
	return str(BUILDING_VISUAL_TEXTURES.get(building_name, ""))

func _find_closest_enemy_snapshot_to_tower(snapshots: Array) -> Dictionary:
	var tower_center := _resolve_building_center("MgTower")
	var best_snapshot: Dictionary = {}
	var best_distance_sq := INF
	for item in snapshots:
		var snapshot := item as Dictionary
		if snapshot == null:
			continue
		if snapshot.get("is_moving_enemy", false) != true:
			continue
		var world := Vector2(float(snapshot.get("world_x", 0.0)), float(snapshot.get("world_y", 0.0)))
		var distance_sq := tower_center.distance_squared_to(world)
		if distance_sq >= best_distance_sq:
			continue
		best_distance_sq = distance_sq
		best_snapshot = snapshot
	return best_snapshot

func _spawn_attack_trace(origin: Vector2, target: Vector2) -> void:
	if _attack_effect_root == null:
		return
	_spawn_muzzle_flash(origin, target)
	var projectile := TextureRect.new()
	projectile.name = "TowerProjectile"
	projectile.size = Vector2(8.0, 8.0)
	projectile.mouse_filter = Control.MOUSE_FILTER_IGNORE
	projectile.stretch_mode = TextureRect.STRETCH_SCALE
	projectile.texture = _load_projectile_texture()
	projectile.position = origin - (projectile.size / 2.0)
	projectile.pivot_offset = projectile.size / 2.0
	projectile.rotation = (target - origin).angle()
	projectile.z_index = 12
	_attack_effect_root.add_child(projectile)
	_attack_effects_spawned_total += 1
	_attack_traces.append({
		"node": projectile,
		"kind": "projectile",
		"life": PROJECTILE_TRAVEL_DURATION_SEC,
		"duration": PROJECTILE_TRAVEL_DURATION_SEC,
		"origin": origin,
		"target": target,
	})

func _spawn_muzzle_flash(origin: Vector2, target: Vector2) -> void:
	if _attack_effect_root == null:
		return
	var flash := TextureRect.new()
	flash.name = "TowerMuzzleFlash"
	flash.size = Vector2(12.0, 12.0)
	flash.mouse_filter = Control.MOUSE_FILTER_IGNORE
	flash.stretch_mode = TextureRect.STRETCH_SCALE
	flash.texture = _load_muzzle_flash_texture()
	flash.position = origin - (flash.size / 2.0)
	flash.pivot_offset = flash.size / 2.0
	flash.rotation = (target - origin).angle()
	flash.z_index = 13
	_attack_effect_root.add_child(flash)
	_attack_effects_spawned_total += 1
	_attack_traces.append({
		"node": flash,
		"kind": "muzzle_flash",
		"life": MUZZLE_FLASH_DURATION_SEC,
		"duration": MUZZLE_FLASH_DURATION_SEC,
		"origin": origin,
	})

func _spawn_hit_spark(position: Vector2, damage_amount: int) -> void:
	if _attack_effect_root == null:
		return
	_spawn_impact_sprite(position)
	var label := Label.new()
	label.name = "EnemyHitSpark"
	label.text = "-%d" % damage_amount
	label.add_theme_font_size_override("font_size", 16)
	label.add_theme_color_override("font_color", Color(1.0, 0.917647, 0.607843, 1.0))
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.position = position + Vector2(10.0, -18.0)
	_attack_effect_root.add_child(label)
	_attack_effects_spawned_total += 1
	_attack_traces.append({
		"node": label,
		"kind": "label",
		"life": HIT_SPARK_DURATION_SEC,
		"duration": HIT_SPARK_DURATION_SEC,
		"origin": label.position,
	})

func _spawn_impact_sprite(position: Vector2) -> void:
	if _attack_effect_root == null:
		return
	var impact := TextureRect.new()
	impact.name = "TowerImpact"
	impact.size = Vector2(12.0, 12.0)
	impact.mouse_filter = Control.MOUSE_FILTER_IGNORE
	impact.stretch_mode = TextureRect.STRETCH_SCALE
	impact.texture = _load_impact_texture()
	impact.position = position - (impact.size / 2.0)
	impact.z_index = 13
	_attack_effect_root.add_child(impact)
	_attack_effects_spawned_total += 1
	_attack_traces.append({
		"node": impact,
		"kind": "impact",
		"life": IMPACT_DURATION_SEC,
		"duration": IMPACT_DURATION_SEC,
		"origin": position,
	})

func _mark_enemy_token_hit(actor_name: String) -> void:
	var state := _enemy_token_states.get(actor_name, {
		"hit_flash": 0.0,
		"death_fade": -1.0,
		"dying": false,
	}) as Dictionary
	state["hit_flash"] = ENEMY_HIT_FLASH_DURATION_SEC
	_enemy_token_states[actor_name] = state

func _mark_enemy_token_dying(actor_name: String) -> void:
	if actor_name.is_empty():
		return
	var token: ColorRect = _enemy_tokens.get(actor_name, null)
	if token == null or not is_instance_valid(token):
		_enemy_tokens.erase(actor_name)
		_enemy_token_states.erase(actor_name)
		return
	var state := _enemy_token_states.get(actor_name, {
		"hit_flash": 0.0,
		"death_fade": -1.0,
		"dying": false,
	}) as Dictionary
	if state.get("dying", false) == true:
		return
	state["dying"] = true
	state["death_fade"] = ENEMY_DEATH_FADE_DURATION_SEC
	state["hit_flash"] = 0.0
	_enemy_token_states[actor_name] = state

func _update_enemy_token_effects(delta: float) -> void:
	for actor_name_variant in _enemy_tokens.keys():
		var actor_name := str(actor_name_variant)
		var token: ColorRect = _enemy_tokens.get(actor_name, null)
		if token == null or not is_instance_valid(token):
			_enemy_tokens.erase(actor_name)
			_enemy_token_states.erase(actor_name)
			continue
		var state := _enemy_token_states.get(actor_name, null) as Dictionary
		if state == null:
			state = {
				"hit_flash": 0.0,
				"death_fade": -1.0,
				"dying": false,
				"walk_phase": 0.0,
				"frame_index": 0,
				"visual_tier": "grunt",
				"sprite_sheet_path": str(ENEMY_SPRITE_SHEET_TEXTURE_PATHS.get("grunt", "")),
			}
		var hit_flash := maxf(0.0, float(state.get("hit_flash", 0.0)) - delta)
		state["hit_flash"] = hit_flash
		if state.get("dying", false) != true:
			var walk_phase := float(state.get("walk_phase", 0.0)) + (delta * ENEMY_WALK_FPS)
			state["walk_phase"] = walk_phase
			state["frame_index"] = int(floor(walk_phase)) % ENEMY_SPRITE_FRAME_COUNT
		if state.get("dying", false) == true:
			var death_fade := float(state.get("death_fade", -1.0))
			death_fade = maxf(0.0, death_fade - delta)
			state["death_fade"] = death_fade
			if death_fade <= 0.0:
				token.queue_free()
				_enemy_tokens.erase(actor_name)
				_enemy_token_states.erase(actor_name)
				continue
		_enemy_token_states[actor_name] = state
		_apply_enemy_token_visual(actor_name, token)

func _apply_enemy_token_visual(actor_name: String, token: ColorRect) -> void:
	var state := _enemy_token_states.get(actor_name, null) as Dictionary
	var sprite := token.get_node_or_null("Sprite") as TextureRect
	if state == null:
		token.color = Color(1.0, 1.0, 1.0, 0.0)
		if sprite != null:
			_apply_enemy_sprite_frame(sprite, 0)
			sprite.modulate = Color(1.0, 1.0, 1.0, 1.0)
		token.scale = Vector2.ONE
		return
	if sprite != null:
		_apply_enemy_sprite_frame(sprite, int(state.get("frame_index", 0)))
	var hit_flash := float(state.get("hit_flash", 0.0))
	var dying: bool = state.get("dying", false) == true
	var death_fade := float(state.get("death_fade", -1.0))
	var base_color := Color(1.0, 1.0, 1.0, 1.0)
	if hit_flash > 0.0:
		var flash_ratio := clampf(hit_flash / ENEMY_HIT_FLASH_DURATION_SEC, 0.0, 1.0)
		base_color = Color(1.0, 0.96, 0.88, 1.0).lerp(base_color, 1.0 - flash_ratio)
		token.scale = Vector2.ONE * lerpf(1.12, 1.0, 1.0 - flash_ratio)
	else:
		token.scale = Vector2.ONE
	if dying and death_fade >= 0.0:
		var fade_ratio := clampf(death_fade / ENEMY_DEATH_FADE_DURATION_SEC, 0.0, 1.0)
		base_color.a = fade_ratio
		token.scale = Vector2.ONE * lerpf(0.72, 1.0, fade_ratio)
	token.color = Color(1.0, 1.0, 1.0, 0.0)
	if sprite != null:
		sprite.modulate = base_color

func _resolve_visual_tier(snapshot: Dictionary) -> String:
	if snapshot.get("is_boss", false) == true:
		return "boss"
	if snapshot.get("is_elite", false) == true:
		return "elite"
	var tier := str(snapshot.get("visual_tier", ""))
	if tier.is_empty():
		return "grunt"
	return tier

func _resolve_enemy_sprite_sheet_path(visual_tier: String) -> String:
	var normalized := visual_tier.to_lower()
	if ENEMY_SPRITE_SHEET_TEXTURE_PATHS.has(normalized):
		return str(ENEMY_SPRITE_SHEET_TEXTURE_PATHS[normalized])
	return str(ENEMY_SPRITE_SHEET_TEXTURE_PATHS["grunt"])

func _load_enemy_sprite_sheet_texture(visual_tier: String) -> Texture2D:
	var texture_path := _resolve_enemy_sprite_sheet_path(visual_tier)
	if _enemy_sprite_sheet_textures.has(texture_path):
		var cached: Variant = _enemy_sprite_sheet_textures.get(texture_path, null)
		if cached is Texture2D:
			return cached
	var texture: Texture2D = null
	var image_path := ProjectSettings.globalize_path(texture_path)
	if FileAccess.file_exists(image_path):
		var image := Image.load_from_file(image_path)
		if image != null and not image.is_empty():
			texture = ImageTexture.create_from_image(image)
	if texture == null and ResourceLoader.exists(texture_path):
		texture = load(texture_path) as Texture2D
	if texture != null:
		_enemy_sprite_sheet_textures[texture_path] = texture
		return texture
	var fallback := _create_fallback_enemy_sheet_texture(visual_tier)
	_enemy_sprite_sheet_textures[texture_path] = fallback
	return fallback

func _create_fallback_enemy_sheet_texture(visual_tier: String) -> Texture2D:
	var image := Image.create(192, 48, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.0, 0.0, 0.0, 0.0))
	var base_color := Color(0.95, 0.24, 0.24, 1.0)
	if visual_tier == "elite":
		base_color = Color(0.95, 0.48, 0.18, 1.0)
	elif visual_tier == "boss":
		base_color = Color(0.78, 0.16, 0.16, 1.0)
	for frame in range(4):
		var offset_x := frame * 48
		for x in range(16):
			for y in range(16):
				image.set_pixel(offset_x + 16 + x, 16 + y, base_color.lightened(frame * 0.04))
	return ImageTexture.create_from_image(image)

func _load_projectile_texture() -> Texture2D:
	if _projectile_texture != null:
		return _projectile_texture
	var image_path := ProjectSettings.globalize_path(PROJECTILE_TEXTURE_PATH)
	if FileAccess.file_exists(image_path):
		var image := Image.load_from_file(image_path)
		if image != null and not image.is_empty():
			_projectile_texture = ImageTexture.create_from_image(image)
	if _projectile_texture == null and ResourceLoader.exists(PROJECTILE_TEXTURE_PATH):
		_projectile_texture = load(PROJECTILE_TEXTURE_PATH) as Texture2D
	if _projectile_texture == null:
		_projectile_texture = _create_fallback_projectile_texture()
	return _projectile_texture

func _load_impact_texture() -> Texture2D:
	if _impact_texture != null:
		return _impact_texture
	if ResourceLoader.exists(IMPACT_TEXTURE_PATH):
		_impact_texture = load(IMPACT_TEXTURE_PATH) as Texture2D
	return _impact_texture

func _load_muzzle_flash_texture() -> Texture2D:
	if _muzzle_flash_texture != null:
		return _muzzle_flash_texture
	if ResourceLoader.exists(MUZZLE_FLASH_TEXTURE_PATH):
		_muzzle_flash_texture = load(MUZZLE_FLASH_TEXTURE_PATH) as Texture2D
	return _muzzle_flash_texture

func _create_fallback_projectile_texture() -> Texture2D:
	var image := Image.create(4, 4, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.0, 0.0, 0.0, 0.0))
	for x in range(4):
		for y in range(4):
			var color := Color(0.960784, 0.87451, 0.447059, 1.0)
			if x == 0 or x == 3 or y == 0 or y == 3:
				color = Color(1.0, 0.980392, 0.823529, 1.0)
			image.set_pixel(x, y, color)
	return ImageTexture.create_from_image(image)

func _apply_enemy_sprite_frame(sprite: TextureRect, frame_index: int) -> void:
	if sprite == null:
		return
	var actor_name := ""
	if sprite.get_parent() != null:
		actor_name = str(sprite.get_parent().name)
	var state := _enemy_token_states.get(actor_name, {}) as Dictionary
	var visual_tier := str(state.get("visual_tier", "grunt"))
	var sheet := _load_enemy_sprite_sheet_texture(visual_tier)
	if sheet == null:
		return
	var atlas := sprite.texture as AtlasTexture
	if atlas == null:
		atlas = AtlasTexture.new()
		sprite.texture = atlas
	atlas.atlas = sheet
	atlas.region = Rect2(float(frame_index % ENEMY_SPRITE_FRAME_COUNT) * ENEMY_SPRITE_FRAME_SIZE.x, 0.0, ENEMY_SPRITE_FRAME_SIZE.x, ENEMY_SPRITE_FRAME_SIZE.y)

func get_runtime_visual_debug_state() -> Dictionary:
	var building_names: Array[String] = []
	for building_name_variant in _building_visuals.keys():
		building_names.append(str(building_name_variant))
	return {
		"building_visuals": building_names,
		"active_attack_effects": _attack_traces.size(),
		"spawned_attack_effects_total": _attack_effects_spawned_total,
		"enemy_tokens": _enemy_tokens.size(),
	}

func get_enemy_token_debug_state(actor_name: String) -> Dictionary:
	var token: ColorRect = _enemy_tokens.get(actor_name, null)
	var state := _enemy_token_states.get(actor_name, {}) as Dictionary
	return {
		"exists": token != null and is_instance_valid(token),
		"color": token.color if token != null and is_instance_valid(token) else Color(0, 0, 0, 0),
		"scale": token.scale if token != null and is_instance_valid(token) else Vector2.ZERO,
		"size": token.size if token != null and is_instance_valid(token) else Vector2.ZERO,
		"hit_flash": float(state.get("hit_flash", 0.0)),
		"dying": state.get("dying", false) == true,
		"death_fade": float(state.get("death_fade", -1.0)),
		"frame_index": int(state.get("frame_index", 0)),
		"visual_tier": str(state.get("visual_tier", "grunt")),
		"sprite_sheet_path": str(state.get("sprite_sheet_path", "")),
	}

func get_all_enemy_token_debug_states() -> Dictionary:
	var result := {}
	for actor_name_variant in _enemy_token_states.keys():
		var actor_name := str(actor_name_variant)
		result[actor_name] = get_enemy_token_debug_state(actor_name)
	return result

func _render_tower_debug_labels() -> void:
	if _building_visual_root == null:
		return
	var bridge := _current_bridge()
	if bridge == null or not bridge.has_method("GetTowerCombatDebugSnapshot"):
		return
	var snapshot: Variant = bridge.call("GetTowerCombatDebugSnapshot")
	if not (snapshot is Dictionary):
		return
	var tower_targets_by_name: Dictionary = {}
	var tower_targets: Variant = (snapshot as Dictionary).get("tower_targets", [])
	if tower_targets is Array:
		for entry_variant in tower_targets:
			var entry := entry_variant as Dictionary
			if entry == null:
				continue
			tower_targets_by_name[str(entry.get("tower_name", ""))] = entry
	for building_name_variant in _building_visuals.keys():
		var building_name := str(building_name_variant)
		if not (building_name.begins_with("MgTower") or building_name.begins_with("SniperTower")):
			continue
		var visual := _building_visuals.get(building_name, null) as TextureRect
		if visual == null or not is_instance_valid(visual):
			continue
		var label := _tower_debug_labels.get(building_name, null) as Label
		if label == null or not is_instance_valid(label):
			label = Label.new()
			label.name = "%sDebugLabel" % building_name
			label.mouse_filter = Control.MOUSE_FILTER_IGNORE
			label.add_theme_font_size_override("font_size", 12)
			label.add_theme_color_override("font_color", Color(1.0, 0.98, 0.92, 1.0))
			_building_visual_root.add_child(label)
			_tower_debug_labels[building_name] = label
		var target_entry := tower_targets_by_name.get(building_name, {}) as Dictionary
		var target_name := str(target_entry.get("target_name", "n/a"))
		var target_hp := int(target_entry.get("target_hp", (snapshot as Dictionary).get("target_hp", -1)))
		label.text = "T:%s\nHP:%s" % [target_name, str(target_hp) if target_hp >= 0 else "n/a"]
		label.position = visual.position + Vector2(-8.0, -30.0)

func _load_wall_crack_tile_textures(texture_path: String) -> Array[Texture2D]:
	var textures: Array[Texture2D] = []
	if not ResourceLoader.exists(texture_path):
		return textures
	var source := load(texture_path) as Texture2D
	if source == null:
		return textures
	for row in range(CRACK_TILE_COUNT):
		textures.append(source)
	return textures

func _t(key: String) -> String:
	if _translate.is_valid():
		return str(_translate.call(key))
	return key

func _try_bridge_summary() -> Dictionary:
	var bridge: Node = _current_bridge()
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

func _push_formal_battle_hud_messages(result: Dictionary, status_text: String) -> void:
	if _battle_hud == null or not is_instance_valid(_battle_hud):
		return
	if _battle_hud.has_method("SetBattleStatusMessage"):
		_battle_hud.call("SetBattleStatusMessage", status_text)
	if _battle_hud.has_method("SetBattleSummaryMessage"):
		_battle_hud.call("SetBattleSummaryMessage", _compose_formal_summary(result))

func _compose_formal_summary(result: Dictionary) -> String:
	var wall_hp: int = int(result.get("wall_hp", 100))
	var enemies: int = int(result.get("enemy_units_spawned", 0))
	var friendly: int = int(result.get("friendly_units_deployed", 0))
	var exchanges: int = int(result.get("combat_exchanges", 0))
	return "%s=%d/100 | %s=%d | %s=%d | %s=%d" % [
		_t("battlemap.summary.wall_hp"),
		wall_hp,
		_t("battlemap.summary.friendly_units"),
		friendly,
		_t("battlemap.summary.enemy_units_spawned"),
		enemies,
		_t("battlemap.summary.combat_exchanges"),
		exchanges,
	]

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided: Variant = _bridge_provider.call()
		if provided is Node:
			return provided
	return _bridge

func _has_required_refs() -> bool:
	return _status_label != null \
		and _summary_label != null \
		and _background != null \
		and _enemy_spawn_a != null \
		and _enemy_spawn_b != null \
		and _local_feedback_layer != null \
		and _local_prompt_panel != null \
		and _local_prompt_label != null
