extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

const _CLASS_CANDIDATES := ["LocalizationManager"]
const _AUTOLOAD_NODE_CANDIDATES := ["LocalizationManager", "I18n", "I18nManager"]
const _SCRIPT_CANDIDATES := [
    "res://Game.Godot/Localization/LocalizationManager.gd",
    "res://Game.Godot/Scripts/Localization/LocalizationManager.gd",
    "res://Game.Godot/I18n/LocalizationManager.gd"
]

func _new_localization_manager() -> Variant:
    for class_id in _CLASS_CANDIDATES:
        if ClassDB.class_exists(class_id) and ClassDB.can_instantiate(class_id):
            return ClassDB.instantiate(class_id)

    var loop := Engine.get_main_loop()
    if loop is SceneTree:
        var tree := loop as SceneTree
        if tree.root != null:
            for node_name in _AUTOLOAD_NODE_CANDIDATES:
                if tree.root.has_node(node_name):
                    return tree.root.get_node(node_name)

    for script_path in _SCRIPT_CANDIDATES:
        if ResourceLoader.exists(script_path):
            var script := load(script_path) as Script
            if script != null:
                return script.new()

    fail("LocalizationManager is not discoverable. Expose a constructible class, autoload, or script.")
    return null

func _write_locale_file(locale_code: String, json_payload: String) -> String:
    var dir_path := ProjectSettings.globalize_path("user://tmp_t28_locales")
    var mkdir_result := DirAccess.make_dir_recursive_absolute(dir_path)
    assert_that(mkdir_result).is_equal(OK)

    var file_path := "%s/%s.json" % [dir_path, locale_code]
    var file := FileAccess.open(file_path, FileAccess.WRITE)
    assert_that(file).is_not_null()
    file.store_string(json_payload)
    file.close()
    return file_path

func _call_required(target: Variant, method_candidates: Array[String], args: Array, capability_name: String) -> Variant:
    var object_target := target as Object
    if object_target == null:
        fail("LocalizationManager instance is null while calling capability: %s" % capability_name)
        return null

    for method_name in method_candidates:
        if object_target.has_method(method_name):
            return object_target.callv(method_name, args)

    var joined := ", ".join(method_candidates)
    fail("Missing LocalizationManager capability '%s'. Expected one of methods: %s" % [capability_name, joined])
    return null

func _configure_locale_resource(target: Variant, locale_code: String, file_path: String) -> void:
    _call_required(
        target,
        ["configure_locale_resource", "set_locale_resource", "register_locale_resource", "set_locale_file"],
        [locale_code, file_path],
        "configure locale resource path"
    )

func _switch_locale(target: Variant, locale_code: String) -> bool:
    var result: Variant = _call_required(
        target,
        ["switch_locale", "set_locale", "change_locale", "use_locale"],
        [locale_code],
        "switch locale with accept/reject result"
    )
    if typeof(result) != TYPE_BOOL:
        fail("Locale switch API must return bool to express accept/reject outcome.")
        return false
    return result

func _current_locale(target: Variant) -> String:
    var result: Variant = _call_required(
        target,
        ["current_locale", "get_current_locale", "get_locale", "locale", "get_language"],
        [],
        "read current locale"
    )
    if typeof(result) != TYPE_STRING:
        fail("Current locale API must return String.")
        return ""
    return result

func _translate_key(target: Variant, key: String) -> String:
    var result: Variant = _call_required(
        target,
        ["translate", "tr_key", "get_text", "get_translation", "tr"],
        [key],
        "query translated text by key"
    )
    if typeof(result) != TYPE_STRING:
        fail("Translation query API must return String.")
        return ""
    return result

# acceptance: ACC:T28.10
# RED: switch must be refused and locale must stay unchanged when target locale data cannot be parsed.
func test_rejects_locale_switch_when_target_resource_parse_fails_and_keeps_current_locale() -> void:
    var manager: Variant = _new_localization_manager()
    if manager == null:
        return

    var en_path := _write_locale_file("en-US", "{\"menu.play\":\"Play\"}")
    var zh_broken_path := _write_locale_file("zh-CN", "{\"menu.play\":\"Play_ZH\"")

    _configure_locale_resource(manager, "en-US", en_path)
    _configure_locale_resource(manager, "zh-CN", zh_broken_path)

    assert_that(_switch_locale(manager, "en-US")).is_true()
    var locale_before := _current_locale(manager)

    var switched := _switch_locale(manager, "zh-CN")
    assert_that(switched).is_false()
    assert_that(_current_locale(manager)).is_equal(locale_before)

# acceptance: ACC:T28.15
# RED: LocalizationManager must load both supported locales and expose deterministic key lookup.
func test_loads_supported_locale_resources_and_provides_key_lookup() -> void:
    var manager: Variant = _new_localization_manager()
    if manager == null:
        return

    var en_path := _write_locale_file("en-US", "{\"menu.play\":\"Play\",\"menu.exit\":\"Exit\"}")
    var zh_path := _write_locale_file("zh-CN", "{\"menu.play\":\"Play_ZH\",\"menu.exit\":\"Exit_ZH\"}")

    _configure_locale_resource(manager, "en-US", en_path)
    _configure_locale_resource(manager, "zh-CN", zh_path)

    assert_that(_switch_locale(manager, "en-US")).is_true()
    assert_that(_translate_key(manager, "menu.play")).is_equal("Play")

    assert_that(_switch_locale(manager, "zh-CN")).is_true()
    assert_that(_translate_key(manager, "menu.play")).is_equal("Play_ZH")

    var unsupported_result := _switch_locale(manager, "fr-FR")
    assert_that(unsupported_result).is_false()
    assert_that(_current_locale(manager)).is_equal("zh-CN")
