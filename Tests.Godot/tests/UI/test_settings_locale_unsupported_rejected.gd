extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _panel: Node = null
var _original_locale: String = ""

func before() -> void:
    _original_locale = TranslationServer.get_locale()
    TranslationServer.set_locale("en-US")

func after() -> void:
    if is_instance_valid(_panel):
        if _panel.get_parent() != null:
            _panel.get_parent().remove_child(_panel)
        _panel.queue_free()
    _panel = null

    if _original_locale != "":
        TranslationServer.set_locale(_original_locale)

func _new_settings_panel() -> Node:
    var packed := load("res://Game.Godot/Scenes/UI/SettingsPanel.tscn") as PackedScene
    assert_object(packed).is_not_null()

    var panel := packed.instantiate()
    _panel = panel
    add_child(panel)
    await get_tree().process_frame
    return panel

func _find_item_index(button: OptionButton, text: String) -> int:
    for i in range(button.get_item_count()):
        if button.get_item_text(i) == text:
            return i
    return -1

# ACC:T28.8
func test_settings_rejects_unsupported_locale_and_keeps_current_locale() -> void:
    var panel := await _new_settings_panel()
    var locale_option := panel.get_node("VBox/LangRow/LangOpt") as OptionButton

    var unsupported_index := _find_item_index(locale_option, "ja")
    assert_int(unsupported_index).is_equal(-1)

    var locale_before := TranslationServer.get_locale()

    var locale_after := TranslationServer.get_locale()
    assert_str(locale_after).is_equal(locale_before)
    assert_str(locale_after).is_not_equal("ja")
