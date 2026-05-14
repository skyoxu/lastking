extends Node

var _i18n: Variant = null
var _current_locale := ""
var _title: Label = null
var _build_btn: Button = null
var _wave_btn: Button = null
var _auto_wave_btn: Button = null
var _exchange_btn: Button = null
var _cleanup_btn: Button = null
var _finish_btn: Button = null
var _back_btn: Button = null
var _legend: Label = null
var _metrics_help: Label = null
var _operation_controller: Node = null

func configure(refs: Dictionary) -> void:
	_title = refs["title"]
	_build_btn = refs["build_btn"]
	_wave_btn = refs["wave_btn"]
	_auto_wave_btn = refs["auto_wave_btn"]
	_exchange_btn = refs["exchange_btn"]
	_cleanup_btn = refs["cleanup_btn"]
	_finish_btn = refs["finish_btn"]
	_back_btn = refs["back_btn"]
	_legend = refs["legend"]
	_metrics_help = refs["metrics_help"]
	_operation_controller = refs["operation_controller"]
	if _i18n == null:
		_i18n = load("res://Game.Godot/Scripts/Localization/LocalizationManager.gd").new()
		_i18n.configure_locale_resource("en-US", "res://Game.Godot/Localization/en-US.json")
		_i18n.configure_locale_resource("zh-CN", "res://Game.Godot/Localization/zh-CN.json")

func sync_locale_and_texts() -> void:
	if _i18n == null:
		return
	var locale := _normalize_locale(str(TranslationServer.get_locale()))
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
	var v := locale.strip_edges().to_lower()
	if v == "zh":
		return "zh-CN"
	if v == "en":
		return "en-US"
	if v.begins_with("zh"):
		return "zh-CN"
	return "en-US"

func _apply_static_texts() -> void:
	_title.text = translate("battlemap.title")
	_build_btn.text = translate("battlemap.btn.build")
	_wave_btn.text = translate("battlemap.btn.wave")
	if _operation_controller == null or not bool(_operation_controller.call("is_auto_wave")):
		_auto_wave_btn.text = translate("battlemap.btn.auto_toggle")
	_exchange_btn.text = translate("battlemap.btn.exchange")
	_cleanup_btn.text = translate("battlemap.btn.cleanup")
	_finish_btn.text = translate("battlemap.btn.finish")
	_back_btn.text = translate("battlemap.btn.back")
	_legend.text = translate("battlemap.legend")
	_metrics_help.text = translate("battlemap.metrics_help")
