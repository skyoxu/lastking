extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const ENEMY_AI_RUNTIME_PROBE_SCENE := "res://Game.Godot/Scenes/Combat/EnemyAiRuntimeProbe.tscn"
const ENEMY_AI_ATTACHMENT_VALIDATION_SCENE := "res://Game.Godot/Scenes/Combat/EnemyAiAttachmentValidationScene.tscn"

func _new_probe() -> Node:
	var packed_scene: PackedScene = load(ENEMY_AI_RUNTIME_PROBE_SCENE)
	var probe: Node = packed_scene.instantiate()
	add_child(probe)
	return probe

func _is_enemy_ai_attachment_valid(enemy: Node) -> bool:
	if enemy.get_script() == null:
		return false
	if not enemy.has_method("IsEnemyAiAttachedAndActive"):
		return false
	return bool(enemy.call("IsEnemyAiAttachedAndActive"))

# acceptance: ACC:T6.8
func test_enemy_ai_attachment_validation_passes_when_enemy_ai_probe_script_is_attached_and_active() -> void:
	var packed_scene: PackedScene = load(ENEMY_AI_ATTACHMENT_VALIDATION_SCENE)
	var combat_root: Node = packed_scene.instantiate()
	add_child(combat_root)

	var enemies: Array[Node] = []
	for child in combat_root.get_children():
		if child is Node and String(child.name).begins_with("Enemy"):
			enemies.append(child)
	assert_int(enemies.size()).is_greater(0)

	for enemy in enemies:
		assert_bool(enemy.get_script() != null).is_true()
		assert_str(String(enemy.get_script().resource_path)).contains("EnemyAi.cs")
		assert_bool(_is_enemy_ai_attachment_valid(enemy)).is_true()

# acceptance: ACC:T6.15
func test_enemy_ai_attachment_validation_fails_when_enemy_ai_is_disabled_or_missing_script() -> void:
	var packed_scene: PackedScene = load(ENEMY_AI_ATTACHMENT_VALIDATION_SCENE)
	var combat_root: Node = packed_scene.instantiate()
	add_child(combat_root)
	var enemy := combat_root.get_node("EnemyA")

	enemy.set("EnemyAiActive", false)
	assert_bool(_is_enemy_ai_attachment_valid(enemy)).is_false()

	enemy.set_script(null)
	assert_bool(enemy.get_script() == null).is_true()
	assert_bool(_is_enemy_ai_attachment_valid(enemy)).is_false()
