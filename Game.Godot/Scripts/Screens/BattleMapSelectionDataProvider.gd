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
		"range_slots": ["InnerCastleRegionSlot_01_00"],
		"blocked_range_slots": ["InnerCastleRegionSlot_02_00"],
		"linked_unit_slots": ["LeftOuterFieldSlot_00_00"],
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
		"range_slots": ["InnerCastleRegionSlot_04_00"],
		"blocked_range_slots": ["InnerCastleRegionSlot_05_00"],
		"linked_unit_slots": ["LeftOuterFieldSlot_01_00"],
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
		"slot_id": "InnerCastleRegionSlot_00_00",
		"selection_id": "barracks_alpha",
	},
	"Battlefield/MgTower": {
		"slot_id": "InnerCastleRegionSlot_03_00",
		"selection_id": "tower_alpha",
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

	var snapshot: Dictionary = definition.duplicate(true)
	snapshot["building_slots"] = [slot_id]
	return snapshot

func _resolve_selection_owner(slot_id: String) -> String:
	var bridge: Node = _current_bridge()
	if bridge != null:
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

func _current_bridge() -> Node:
	if _bridge_provider.is_valid():
		var provided: Variant = _bridge_provider.call()
		if provided is Node:
			return provided
	return null









