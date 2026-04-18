extends RefCounted
class_name LocalizationManager

const SUPPORTED_LOCALES: Array[String] = ["en-US", "zh-CN"]

var _resource_paths: Dictionary = {}
var _tables: Dictionary = {}
var _current_locale: String = "en-US"

func configure_locale_resource(locale_code: String, file_path: String) -> bool:
	if not _is_supported_locale(locale_code):
		return false
	if file_path.strip_edges() == "":
		return false
	_resource_paths[locale_code] = file_path
	return true

func set_locale_resource(locale_code: String, file_path: String) -> bool:
	return configure_locale_resource(locale_code, file_path)

func register_locale_resource(locale_code: String, file_path: String) -> bool:
	return configure_locale_resource(locale_code, file_path)

func set_locale_file(locale_code: String, file_path: String) -> bool:
	return configure_locale_resource(locale_code, file_path)

func switch_locale(locale_code: String) -> bool:
	if not _is_supported_locale(locale_code):
		return false
	if not _tables.has(locale_code):
		if not _load_locale_table(locale_code):
			return false
	_current_locale = locale_code
	return true

func set_locale(locale_code: String) -> bool:
	return switch_locale(locale_code)

func change_locale(locale_code: String) -> bool:
	return switch_locale(locale_code)

func use_locale(locale_code: String) -> bool:
	return switch_locale(locale_code)

func current_locale() -> String:
	return _current_locale

func get_current_locale() -> String:
	return _current_locale

func get_locale() -> String:
	return _current_locale

func locale() -> String:
	return _current_locale

func get_language() -> String:
	return _current_locale

func translate(key: String) -> String:
	return _translate_internal(key)

func tr_key(key: String) -> String:
	return _translate_internal(key)

func get_text(key: String) -> String:
	return _translate_internal(key)

func get_translation(key: String) -> String:
	return _translate_internal(key)

func _load_locale_table(locale_code: String) -> bool:
	if not _resource_paths.has(locale_code):
		return false
	var file_path_value: Variant = _resource_paths[locale_code]
	var file_path: String = str(file_path_value)
	if file_path.strip_edges() == "":
		return false

	var file: FileAccess = FileAccess.open(file_path, FileAccess.READ)
	if file == null:
		return false
	var payload: String = file.get_as_text()
	file.close()

	var parsed: Variant = JSON.parse_string(payload)
	if typeof(parsed) != TYPE_DICTIONARY:
		return false

	var parsed_dict: Dictionary = parsed
	var table: Dictionary = {}
	for key_variant in parsed_dict.keys():
		var key_text: String = str(key_variant)
		var value_text: String = str(parsed_dict[key_variant])
		table[key_text] = value_text

	_tables[locale_code] = table
	return true

func _translate_internal(key: String) -> String:
	if key.strip_edges() == "":
		return key
	if not _tables.has(_current_locale):
		return key

	var table_value: Variant = _tables[_current_locale]
	if typeof(table_value) != TYPE_DICTIONARY:
		return key

	var table: Dictionary = table_value
	if not table.has(key):
		return key
	return str(table[key])

func _is_supported_locale(locale_code: String) -> bool:
	for candidate in SUPPORTED_LOCALES:
		if candidate == locale_code:
			return true
	return false
