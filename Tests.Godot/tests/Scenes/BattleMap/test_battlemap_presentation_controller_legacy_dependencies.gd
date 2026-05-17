extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func test_refs_provider_should_not_expose_legacy_legend_or_metrics_keys() -> void:
	var screen := preload("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn").instantiate()
	add_child(auto_free(screen))
	await get_tree().process_frame
	await get_tree().process_frame

	var refs_provider: Node = screen.get_node("RefsProvider")
	var refs: Dictionary = refs_provider.call("build_refs")

	assert_bool(refs.has("legend")).is_false()
	assert_bool(refs.has("metrics_help")).is_false()

func test_presentation_controller_should_only_require_title_for_locale_sync() -> void:
	var controller := preload("res://Game.Godot/Scripts/Screens/BattleMapPresentationController.gd").new()
	add_child(auto_free(controller))

	var title := Label.new()

	controller.call("configure", {
		"title": title,
	})

	controller.call("sync_locale_and_texts")

	assert_bool(String(title.text).length() > 0).is_true()
