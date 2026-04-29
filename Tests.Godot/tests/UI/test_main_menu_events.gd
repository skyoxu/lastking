extends "res://addons/gdUnit4/src/GdUnitTestSuite.gd"

var _bus: Node
var _event_types: Array[String] = []
var _events: Array[Dictionary] = []
const BOOT_READY_OVERRIDE_ENV := "LASTKING_BOOT_READY_OVERRIDE"

func _continue_state_path() -> String:
    return ProjectSettings.globalize_path("user://continue_state.json")

func _latest_chapter7_closure_summary_path() -> String:
    var logs_ci_root := ProjectSettings.globalize_path("res://../logs/ci")
    var ci_dir := DirAccess.open(logs_ci_root)
    if ci_dir == null:
        return ""

    var latest_date := ""
    ci_dir.list_dir_begin()
    while true:
        var dir_name := ci_dir.get_next()
        if dir_name == "":
            break
        if dir_name.begins_with("."):
            continue
        if not ci_dir.current_is_dir():
            continue
        if dir_name.length() != 10:
            continue
        var candidate := logs_ci_root.path_join(dir_name).path_join("chapter7-ui-wiring").path_join("closure-summary.json")
        if not FileAccess.file_exists(candidate):
            continue
        if dir_name > latest_date:
            latest_date = dir_name
    ci_dir.list_dir_end()

    if latest_date == "":
        return ""
    return logs_ci_root.path_join(latest_date).path_join("chapter7-ui-wiring").path_join("closure-summary.json")

func _remove_continue_snapshot_if_exists() -> void:
    var path := _continue_state_path()
    if FileAccess.file_exists(path):
        DirAccess.remove_absolute(path)

func before() -> void:
    # Install a temporary EventBus under /root to mimic Autoload
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")
    _bus = preload("res://Game.Godot/Adapters/EventBusAdapter.cs").new()
    _bus.name = "EventBus"
    get_tree().get_root().add_child(auto_free(_bus))
    _bus.connect("DomainEventEmitted", Callable(self, "_on_evt"))
    _remove_continue_snapshot_if_exists()

func after() -> void:
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")
    _remove_continue_snapshot_if_exists()

func _on_evt(type, _source, _data_json, _id, _spec, _ct, _ts) -> void:
    _event_types.append(str(type))
    var payload := {}
    if str(_data_json) != "":
        var parsed = JSON.parse_string(str(_data_json))
        if typeof(parsed) == TYPE_DICTIONARY:
            payload = parsed
    _events.append({
        "type": str(type),
        "payload": payload,
    })

func _find_event_payload(type_name: String) -> Dictionary:
    for evt in _events:
        if str(evt.get("type", "")) == type_name:
            return evt.get("payload", {})
    return {}

# ACC:T41.1
func test_main_menu_emits_start() -> void:
    _event_types.clear()
    _events.clear()
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var btn = menu.get_node("VBox/BtnPlay")
    btn.emit_signal("pressed")
    await get_tree().process_frame
    assert_bool(_event_types.has("ui.menu.start")).is_true()

# ACC:T41.2
# ACC:T41.3
func test_main_menu_blocks_start_when_bootstrap_not_ready() -> void:
    _event_types.clear()
    _events.clear()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "not-ready")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var btn = menu.get_node("VBox/BtnPlay")
    btn.emit_signal("pressed")
    await get_tree().process_frame
    var gate = menu.get_node("ContinueGateDialog")
    var boot_label: Label = menu.get_node("BootStatusPanel/VBox/BootStatusLabel")
    assert_str(boot_label.text).contains("Boot Status: Not Ready")
    assert_bool(bool(gate.visible)).is_false()
    assert_bool(_event_types.has("ui.menu.start")).is_true()
    assert_bool(_event_types.has("ui.menu.start_degraded")).is_true()
    assert_bool(_event_types.has("ui.menu.start_blocked")).is_false()
    var start_payload := _find_event_payload("ui.menu.start_degraded")
    assert_str(str(start_payload.get("reason", ""))).is_equal("bootstrap_override_not_ready")
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")

# ACC:T41.4
func test_main_menu_blocks_continue_without_snapshot_and_shows_gate_dialog() -> void:
    _event_types.clear()
    _events.clear()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var continue_btn = menu.get_node("VBox/BtnContinue")
    continue_btn.emit_signal("pressed")
    await get_tree().process_frame
    var gate = menu.get_node("ContinueGateDialog")
    assert_bool(bool(gate.visible)).is_true()
    assert_bool(_event_types.has("ui.menu.continue")).is_false()
    assert_bool(_event_types.has("ui.menu.continue_blocked")).is_true()
    var blocked_payload := _find_event_payload("ui.menu.continue_blocked")
    assert_str(str(blocked_payload.get("reason", ""))).is_equal("missing_continue_state")
    assert_bool(bool(menu.visible)).is_true()

# ACC:T41.5
func test_main_menu_retry_bootstrap_closes_gate_and_emits_retry_event() -> void:
    _event_types.clear()
    _events.clear()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var continue_btn = menu.get_node("VBox/BtnContinue")
    continue_btn.emit_signal("pressed")
    await get_tree().process_frame
    var retry_btn = menu.get_node("ContinueGateDialog/VBox/BtnRetryBootstrap")
    retry_btn.emit_signal("pressed")
    await get_tree().process_frame
    var gate = menu.get_node("ContinueGateDialog")
    var boot_label: Label = menu.get_node("BootStatusPanel/VBox/BootStatusLabel")
    var export_label: Label = menu.get_node("BootStatusPanel/VBox/ExportStatusLabel")
    assert_bool(bool(gate.visible)).is_false()
    assert_bool(_event_types.has("ui.menu.bootstrap_retry")).is_true()
    assert_str(boot_label.text).is_equal("Boot Status: Ready")
    assert_str(export_label.text).is_equal("Export Status: Ready")

# ACC:T41.6
func test_main_menu_continue_succeeds_with_valid_snapshot() -> void:
    _event_types.clear()
    _events.clear()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")
    var snapshot_path := _continue_state_path()
    FileAccess.open(snapshot_path, FileAccess.WRITE).store_string("{\"ok\":true}")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var continue_btn = menu.get_node("VBox/BtnContinue")
    continue_btn.emit_signal("pressed")
    await get_tree().process_frame
    var gate = menu.get_node("ContinueGateDialog")
    assert_bool(_event_types.has("ui.menu.continue")).is_true()
    assert_bool(bool(gate.visible)).is_false()
    assert_bool(bool(menu.visible)).is_false()

# ACC:T41.7
func test_main_menu_continue_blocks_invalid_snapshot_payload() -> void:
    _event_types.clear()
    _events.clear()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")
    var snapshot_path := _continue_state_path()
    FileAccess.open(snapshot_path, FileAccess.WRITE).store_string("invalid-json")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var continue_btn = menu.get_node("VBox/BtnContinue")
    continue_btn.emit_signal("pressed")
    await get_tree().process_frame
    var gate = menu.get_node("ContinueGateDialog")
    assert_bool(_event_types.has("ui.menu.continue")).is_false()
    assert_bool(_event_types.has("ui.menu.continue_blocked")).is_true()
    var blocked_payload := _find_event_payload("ui.menu.continue_blocked")
    assert_str(str(blocked_payload.get("reason", ""))).is_equal("invalid_continue_state")
    assert_bool(bool(gate.visible)).is_true()
    assert_bool(bool(menu.visible)).is_true()

# ACC:T41.8
func test_main_menu_continue_blocks_when_bootstrap_not_ready() -> void:
    _event_types.clear()
    _events.clear()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "not-ready")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var continue_btn = menu.get_node("VBox/BtnContinue")
    continue_btn.emit_signal("pressed")
    await get_tree().process_frame
    var gate = menu.get_node("ContinueGateDialog")
    var boot_label: Label = menu.get_node("BootStatusPanel/VBox/BootStatusLabel")
    assert_str(boot_label.text).contains("Boot Status: Not Ready")
    assert_bool(_event_types.has("ui.menu.continue")).is_false()
    assert_bool(_event_types.has("ui.menu.continue_blocked")).is_true()
    var blocked_payload := _find_event_payload("ui.menu.continue_blocked")
    assert_str(str(blocked_payload.get("reason", ""))).is_equal("bootstrap_override_not_ready")
    assert_bool(bool(gate.visible)).is_true()
    assert_bool(bool(menu.visible)).is_true()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")

# ACC:T41.9
func test_main_menu_retry_bootstrap_keeps_gate_visible_when_still_not_ready() -> void:
    _event_types.clear()
    _events.clear()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "not-ready")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var continue_btn = menu.get_node("VBox/BtnContinue")
    continue_btn.emit_signal("pressed")
    await get_tree().process_frame
    var retry_btn = menu.get_node("ContinueGateDialog/VBox/BtnRetryBootstrap")
    retry_btn.emit_signal("pressed")
    await get_tree().process_frame
    var gate = menu.get_node("ContinueGateDialog")
    var boot_label: Label = menu.get_node("BootStatusPanel/VBox/BootStatusLabel")
    assert_str(boot_label.text).contains("Boot Status: Not Ready")
    assert_bool(bool(gate.visible)).is_true()
    assert_bool(_event_types.has("ui.menu.bootstrap_retry")).is_true()
    assert_bool(_event_types.has("ui.menu.start")).is_false()
    assert_bool(_event_types.has("ui.menu.continue")).is_false()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")

# ACC:T41.10
func test_main_menu_ready_status_has_no_missing_sdk_warning() -> void:
    _event_types.clear()
    _events.clear()
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var boot_label: Label = menu.get_node("BootStatusPanel/VBox/BootStatusLabel")
    assert_str(boot_label.text).contains("Boot Status: Ready")
    assert_bool(boot_label.text.find("missing_dotnet_sdk") == -1).is_true()
    assert_bool(boot_label.text.find("unsupported_engine_version") == -1).is_true()
    var gate = menu.get_node("ContinueGateDialog")
    assert_bool(bool(gate.visible)).is_false()

# ACC:T41.11
func test_main_menu_boot_surfaces_exist_as_owned_nodes() -> void:
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    assert_object(menu.get_node_or_null("BootStatusPanel")).is_not_null()
    assert_object(menu.get_node_or_null("ContinueGateDialog")).is_not_null()

# ACC:T41.13
func test_main_menu_boot_panel_is_runtime_visible_owned_surface() -> void:
    OS.set_environment(BOOT_READY_OVERRIDE_ENV, "ready")
    var menu = preload("res://Game.Godot/Scenes/UI/MainMenu.tscn").instantiate()
    add_child(auto_free(menu))
    await get_tree().process_frame
    var panel = menu.get_node("BootStatusPanel")
    var boot_label: Label = menu.get_node("BootStatusPanel/VBox/BootStatusLabel")
    var export_label: Label = menu.get_node("BootStatusPanel/VBox/ExportStatusLabel")
    assert_bool(bool(panel.visible)).is_true()
    assert_str(boot_label.text).contains("Boot Status:")
    assert_str(export_label.text).contains("Export Status:")

# ACC:T41.12
func test_main_menu_chapter7_closure_for_task41_reports_runtime_no_pending_surfaces() -> void:
    var summary_path := _latest_chapter7_closure_summary_path()
    if not FileAccess.file_exists(summary_path):
        push_error("chapter7 closure summary not found: " + summary_path)
        assert_bool(false).is_true()
        return
    var content := FileAccess.get_file_as_string(summary_path)
    var parsed = JSON.parse_string(content)
    assert_bool(typeof(parsed) == TYPE_DICTIONARY).is_true()
    var slices = parsed.get("slices", [])
    var target_slice: Dictionary = {}
    for slice in slices:
        if typeof(slice) != TYPE_DICTIONARY:
            continue
        if str(slice.get("ui_entry", "")) == "MainMenu / Boot Flow":
            target_slice = slice
            break
    assert_bool(target_slice.size() > 0).is_true()
    assert_str(str(target_slice.get("evidence_status", ""))).is_equal("runtime")
    var surface_status = target_slice.get("surface_status", {})
    var pending_surfaces = surface_status.get("pending_surfaces", [])
    var gaps = target_slice.get("gap_to_close", [])
    assert_int(int(pending_surfaces.size())).is_equal(0)
    assert_int(int(gaps.size())).is_equal(0)

