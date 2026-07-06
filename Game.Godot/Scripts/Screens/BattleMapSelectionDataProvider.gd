extends Node

const BUILDING_DEFINITIONS := {
	"barracks_alpha": {
		"selection_id": "barracks_alpha",
		"building_type": "barracks",
		"display_name_key": "hud.build_slot.barracks",
		"preview_kind": "barracks",
		"category": "unit",
		"placement_zone": "inner_castle",
		"allowed_regions": ["outer_field"],
		"gold_cost": 80,
		"iron_cost": 0,
		"population_cost": 0,
		"range_slots": [],
		"blocked_range_slots": [],
		"linked_unit_slots": [],
	},
	"tower_alpha": {
		"selection_id": "tower_alpha",
		"building_type": "mg tower",
		"display_name_key": "hud.build_slot.tower",
		"preview_kind": "tower",
		"category": "defense",
		"placement_zone": "inner_castle",
		"allowed_regions": ["inner_castle"],
		"gold_cost": 60,
		"iron_cost": 0,
		"population_cost": 0,
		"range_slots": [],
		"blocked_range_slots": [],
		"linked_unit_slots": [],
		"range_px": 300,
		"attack_damage": 25,
		"attack_interval_seconds": 2.0,
		"targeting_mode": "frontline_finisher_split",
	},
	"tower_beta": {
		"selection_id": "tower_beta",
		"building_type": "sniper tower",
		"display_name_key": "hud.build_slot.sniper_tower",
		"preview_kind": "tower_beta",
		"category": "defense",
		"placement_zone": "inner_castle",
		"allowed_regions": ["inner_castle"],
		"gold_cost": 90,
		"iron_cost": 10,
		"population_cost": 0,
		"range_slots": [],
		"blocked_range_slots": [],
		"linked_unit_slots": [],
		"range_px": 420,
		"attack_damage": 14,
		"attack_interval_seconds": 1.2,
		"targeting_mode": "frontline_pressure_split",
	},
	"farm_alpha": {
		"selection_id": "farm_alpha",
		"building_type": "residence",
		"display_name_key": "hud.build_slot.residence",
		"preview_kind": "residence",
		"category": "economy",
		"placement_zone": "inner_castle",
		"allowed_regions": ["inner_castle"],
		"gold_cost": 30,
		"iron_cost": 0,
		"population_cost": 0,
		"range_slots": [],
		"blocked_range_slots": [],
		"linked_unit_slots": [],
	},
}

const RUNTIME_BRIDGE_SLOT_TO_SELECTION_OWNER := {
	"Battlefield/Barracks": {
		"slot_id": "RightOuterFieldSlot_00_00",
		"selection_id": "barracks_alpha",
	},
	"Battlefield/MgTower": {
		"slot_id": "InnerCastleRegionSlot_03_00",
		"selection_id": "tower_alpha",
	},
	"Battlefield/SniperTower": {
		"slot_id": "InnerCastleRegionSlot_06_05",
		"selection_id": "tower_beta",
	},
	"Battlefield/Residence": {
		"slot_id": "InnerCastleRegionSlot_06_00",
		"selection_id": "farm_alpha",
	},
}

var _bridge_provider: Callable

func configure(refs: Dictionary) -> void:
	_bridge_provider = refs.get("bridge_provider", Callable())

func get_building_definition(selection_id: String) -> Dictionary:
	var definition: Variant = BUILDING_DEFINITIONS.get(selection_id, null)
	if definition is Dictionary:
		return (definition as Dictionary).duplicate(true)
	return {}

func get_formal_selection_snapshot(slot_id: String) -> Dictionary:
	var selection_owner: String = _resolve_selection_owner(slot_id)
	if selection_owner.is_empty():
		return {}
	var definition: Dictionary = get_building_definition(selection_owner)
	if definition.is_empty():
		return {}
	return _formal_selection_snapshot_for(selection_owner, slot_id, definition)

func _resolve_selection_owner(slot_id: String) -> String:
	var bridge: Node = _current_bridge()
	if bridge != null:
		if bridge.has_method("GetPlacedBuildingSlots"):
			var placed_variant: Variant = bridge.call("GetPlacedBuildingSlots")
			if placed_variant is Dictionary:
				var placed_slots: Dictionary = placed_variant as Dictionary
				if placed_slots.has(slot_id):
					return str(placed_slots[slot_id])
		var slot_is_runtime_managed: bool = false
		for path_variant in RUNTIME_BRIDGE_SLOT_TO_SELECTION_OWNER.keys():
			var node_path: String = str(path_variant)
			var entry: Dictionary = RUNTIME_BRIDGE_SLOT_TO_SELECTION_OWNER[path_variant] as Dictionary
			if entry == null:
				continue
			if str(entry.get("slot_id", "")) != slot_id:
				continue
			slot_is_runtime_managed = true
			if bridge.has_node(NodePath(node_path)):
				return str(entry.get("selection_id", ""))
		if slot_is_runtime_managed:
			return ""
	return ""

func _formal_selection_snapshot_for(selection_id: String, slot_id: String, definition: Dictionary) -> Dictionary:
	var snapshot: Dictionary = definition.duplicate(true)
	snapshot["building_slots"] = [slot_id]
	match selection_id:
		"tower_alpha":
			snapshot["range_slots"] = ["InnerCastleRegionSlot_04_00"]
			snapshot["blocked_range_slots"] = ["InnerCastleRegionSlot_05_00"]
		"tower_beta":
			snapshot["range_slots"] = ["InnerCastleRegionSlot_05_00"]
			snapshot["blocked_range_slots"] = ["InnerCastleRegionSlot_06_00"]
		"barracks_alpha":
			snapshot["range_slots"] = ["InnerCastleRegionSlot_01_00"]
			snapshot["blocked_range_slots"] = ["InnerCastleRegionSlot_02_00"]
			snapshot["linked_unit_slots"] = ["RightOuterFieldSlot_01_00"]
		"farm_alpha":
			snapshot["hidden_slots"] = ["InnerCastleRegionSlot_03_00"]
		_:
			pass
	return snapshot

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided: Variant = _bridge_provider.call()
		if provided is Node:
			return provided
	return null









