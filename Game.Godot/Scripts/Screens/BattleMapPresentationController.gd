extends Node

var _i18n: Variant = null
var _current_locale: String = ""
var _title: Label = null
var _battle_settings_menu: Control = null

func configure(refs: Dictionary) -> void:
	_title = refs["title"]
	_battle_settings_menu = refs.get("battle_settings_menu", null)
	if _i18n == null:
		_i18n = load("res://Game.Godot/Scripts/Localization/LocalizationManager.gd").new()
		_i18n.configure_locale_resource("en-US", "res://Game.Godot/Localization/en-US.json")
		_i18n.configure_locale_resource("zh-CN", "res://Game.Godot/Localization/zh-CN.json")

func sync_locale_and_texts() -> void:
	if _i18n == null:
		return
	var locale: String = _normalize_locale(str(TranslationServer.get_locale()))
	if locale == _current_locale:
		return
	if not _i18n.switch_locale(locale):
		return
	_current_locale = locale
	_apply_static_texts()
	_apply_battle_settings_texts()

func translate(key: String) -> String:
	if _i18n == null:
		return key
	sync_locale_and_texts()
	var translated: String = str(_i18n.translate(key))
	return translated if translated != key else key

func _normalize_locale(locale: String) -> String:
	var v: String = locale.strip_edges().to_lower()
	if v == "zh":
		return "zh-CN"
	if v == "en":
		return "en-US"
	if v.begins_with("zh"):
		return "zh-CN"
	return "en-US"

func _apply_static_texts() -> void:
	_title.text = translate("battlemap.title")

func _apply_battle_settings_texts() -> void:
	if _battle_settings_menu == null:
		return
	var title: Label = _battle_settings_menu.get_node_or_null("VBox/Title")
	if title != null:
		title.text = translate("battlemap.settings.title")
	var summary_title: Label = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsContent/VBox/SummaryTitle")
	if summary_title != null:
		summary_title.text = translate("battlemap.settings.summary_title")
	var summary_body: Label = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsContent/VBox/SummaryBody")
	if summary_body != null:
		summary_body.text = translate("battlemap.settings.summary_body")
	var controls_title: Label = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsContent/VBox/ControlsTitle")
	if controls_title != null:
		controls_title.text = translate("battlemap.settings.controls_title")
	var controls_body: Label = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsContent/VBox/ControlsBody")
	if controls_body != null:
		controls_body.text = translate("battlemap.settings.controls_body")
	var status_title: Label = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsContent/VBox/StatusTitle")
	if status_title != null:
		status_title.text = translate("battlemap.settings.status_title")
	var status_body: Label = _battle_settings_menu.get_node_or_null("VBox/Panel/SettingsContent/VBox/StatusBody")
	if status_body != null:
		status_body.text = translate("battlemap.settings.status_body")
	var return_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToGameBtn")
	if return_btn != null:
		return_btn.text = translate("battlemap.settings.return_to_game")
	var main_menu_btn: Button = _battle_settings_menu.get_node_or_null("VBox/Buttons/ReturnToMainMenuBtn")
	if main_menu_btn != null:
		main_menu_btn.text = translate("battlemap.settings.return_to_main_menu")



