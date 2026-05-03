extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

func test_localization_manager_loads_en_and_zh_tables_for_key_ui_labels() -> void:
	var lm_script := load("res://Game.Godot/Scripts/Localization/LocalizationManager.gd")
	assert_object(lm_script).is_not_null()
	var lm = lm_script.new()

	assert_bool(lm.configure_locale_resource("en-US", "res://Game.Godot/Localization/en-US.json")).is_true()
	assert_bool(lm.configure_locale_resource("zh-CN", "res://Game.Godot/Localization/zh-CN.json")).is_true()

	assert_bool(lm.switch_locale("en-US")).is_true()
	var en_play := String(lm.translate("menu.play"))
	var en_settings := String(lm.translate("menu.settings"))
	var en_pause := String(lm.translate("hud.pause"))

	assert_bool(lm.switch_locale("zh-CN")).is_true()
	var zh_play := String(lm.translate("menu.play"))
	var zh_settings := String(lm.translate("menu.settings"))
	var zh_pause := String(lm.translate("hud.pause"))

	assert_bool(en_play != "menu.play").is_true()
	assert_bool(en_settings != "menu.settings").is_true()
	assert_bool(en_pause != "hud.pause").is_true()

	assert_bool(zh_play != "menu.play").is_true()
	assert_bool(zh_settings != "menu.settings").is_true()
	assert_bool(zh_pause != "hud.pause").is_true()

	assert_bool(zh_play != en_play).is_true()
	assert_bool(zh_settings != en_settings).is_true()
	assert_bool(zh_pause != en_pause).is_true()
