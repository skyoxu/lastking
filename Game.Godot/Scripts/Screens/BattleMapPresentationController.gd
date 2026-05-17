extends Node

var _i18n: Variant = null
var _current_locale: String = ""
var _title: Label = null

func configure(refs: Dictionary) -> void:
	_title = refs["title"]
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

func translate(key: String) -> String:
	if _i18n == null:
		return key
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



