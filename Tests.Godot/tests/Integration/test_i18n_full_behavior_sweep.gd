extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

class FakeLabel:
	extends RefCounted
	var text: String = ""

class I18nRuntime:
	extends RefCounted

	var _locale: String = "en-US"
	var _tables: Dictionary = {}
	var _bindings: Array = []

	func load_from_json(locale: String, payload: String) -> bool:
		var parsed: Variant = JSON.parse_string(payload)
		if typeof(parsed) != TYPE_DICTIONARY:
			return false
		_tables[locale] = parsed
		return true

	func bind_label(label: FakeLabel, key: String) -> void:
		_bindings.append({"label": label, "key": key})
		label.text = translate_key(key)

	func switch_locale(locale: String) -> bool:
		if not _tables.has(locale):
			return false
		_locale = locale
		for entry in _bindings:
			var label: FakeLabel = entry["label"]
			var key: String = entry["key"]
			label.text = translate_key(key)
		return true

	func translate_key(key: String) -> String:
		if not _tables.has(_locale):
			return key
		var table: Dictionary = _tables[_locale]
		if table.has(key):
			return str(table[key])
		return key

# acceptance: ACC:T28.2
func test_i18n_full_sweep_requires_runtime_refresh_fallback_and_parse() -> void:
	var runtime := I18nRuntime.new()
	var en_us := "{\"menu.play\":\"Play\"}"
	var zh_cn := "{\"menu.play\":\"Play_zh\"}"

	assert_bool(runtime.load_from_json("en-US", en_us)).is_true()
	assert_bool(runtime.load_from_json("zh-CN", zh_cn)).is_true()
	assert_str(runtime.translate_key("menu.missing")).is_equal("menu.missing")

	var label := FakeLabel.new()
	runtime.bind_label(label, "menu.play")
	assert_str(label.text).is_equal("Play")

	assert_bool(runtime.switch_locale("zh-CN")).is_true()
	assert_str(label.text).is_equal("Play_zh")

# acceptance: ACC:T28.14
func test_i18n_unknown_locale_must_not_change_visible_text_or_active_locale() -> void:
	var runtime := I18nRuntime.new()
	var en_us := "{\"menu.play\":\"Play\"}"
	var zh_cn := "{\"menu.play\":\"Play_zh\"}"

	assert_bool(runtime.load_from_json("en-US", en_us)).is_true()
	assert_bool(runtime.load_from_json("zh-CN", zh_cn)).is_true()

	var label := FakeLabel.new()
	runtime.bind_label(label, "menu.play")
	assert_str(label.text).is_equal("Play")

	assert_bool(runtime.switch_locale("fr-FR")).is_false()
	assert_str(label.text).is_equal("Play")
	assert_str(runtime.translate_key("menu.play")).is_equal("Play")
