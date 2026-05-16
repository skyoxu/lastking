extends Control

@onready var _label: Label = $RuntimeUi/VBox/Output
var _score: int = 0
var _hp: int = 100
var _i18n: Variant = null

func _enter_tree() -> void:
	_ensure_event_bus()

func _ready() -> void:
	_i18n = load("res://Game.Godot/Scripts/Localization/LocalizationManager.gd").new()
	_i18n.configure_locale_resource("en-US", "res://Game.Godot/Localization/en-US.json")
	_i18n.configure_locale_resource("zh-CN", "res://Game.Godot/Localization/zh-CN.json")
	_i18n.switch_locale(_normalize_locale(str(TranslationServer.get_locale())))
	var hud = get_node_or_null("RuntimeUi/HUD")
	if hud != null:
		hud.visible = false
		var bottom_bar = hud.get_node_or_null("CombatHud/BottomBar")
		if bottom_bar != null:
			bottom_bar.visible = false
		var feedback_layer = hud.get_node_or_null("FeedbackLayer")
		if feedback_layer != null:
			feedback_layer.visible = false
		if hud.has_method("SetBattleHudActive"):
			hud.call("SetBattleHudActive", false)
	print("[TEMPLATE_SMOKE_READY] Main scene initialized")
	var db = get_node_or_null("/root/SqlDb")
	if db != null:
		var ok = db.TryOpen("user://data/game.db")
		if not ok:
			print("[DB] open failed: ", str(db.LastError))
		else:
			print("[DB] opened at user://data/game.db")
	$RuntimeUi/VBox/PublishBtn.pressed.connect(_on_publish)
	$RuntimeUi/VBox/SaveLoadBtn.pressed.connect(_on_save_load)
	$RuntimeUi/VBox/LogBtn.pressed.connect(_on_log)
	if has_node("RuntimeUi/VBox/AddScoreBtn"):
		$RuntimeUi/VBox/AddScoreBtn.pressed.connect(_on_add_score)
	if has_node("RuntimeUi/VBox/LoseHpBtn"):
		$RuntimeUi/VBox/LoseHpBtn.pressed.connect(_on_lose_hp)
	var bus = get_node_or_null("/root/EventBus")
	if bus != null:
		bus.connect("DomainEventEmitted", Callable(self, "_on_domain_event"))

func _process(_delta: float) -> void:
	var locale := _normalize_locale(str(TranslationServer.get_locale()))
	if locale != _i18n.current_locale():
		_i18n.switch_locale(locale)

func _exit_tree() -> void:
	var bus = get_node_or_null("/root/EventBus")
	if bus == null:
		return
	var callable := Callable(self, "_on_domain_event")
	if bus.is_connected("DomainEventEmitted", callable):
		bus.disconnect("DomainEventEmitted", callable)

func _on_publish() -> void:
	var bus = get_node_or_null("/root/EventBus")
	if bus == null:
		_label.text = _t("main.eventbus_missing")
		return
	bus.PublishSimple("demo.event", "ui", "{\"msg\":\"hello\"}")
	_label.text = _t("main.published_demo")

func _on_save_load() -> void:
	var ds = get_node_or_null("/root/DataStore")
	if ds == null:
		_label.text = _t("main.datastore_missing")
		return
	var key = "demo_save"
	var json = "{\"ts\":" + str(Time.get_unix_time_from_system()) + "}"
	ds.SaveSync(key, json)
	var loaded = ds.LoadSync(key)
	_label.text = _t("main.loaded_prefix") + str(loaded)

func _on_log() -> void:
	var logger = get_node_or_null("/root/Logger")
	if logger == null:
		_label.text = _t("main.logger_missing")
		return
	logger.Info("Hello from Main.gd")
	_label.text = _t("main.logged_console")

func _bus():
	return get_node_or_null("/root/EventBus")

func _ensure_event_bus() -> void:
	if get_node_or_null("/root/EventBus") != null:
		return
	var root := get_tree().get_root()
	if root == null:
		return
	var event_bus_script := load("res://Game.Godot/Adapters/EventBusAdapter.cs")
	if event_bus_script == null:
		return
	var event_bus = event_bus_script.new()
	if event_bus == null:
		return
	event_bus.name = "EventBus"
	root.add_child(event_bus)

func _on_add_score() -> void:
	_score += 10
	var demo = get_node_or_null("/root/Main/EngineDemo")
	if demo != null and demo.has_method("AddScore"):
		demo.AddScore(10)
	else:
		var bus = _bus()
		if bus != null:
			bus.PublishSimple("core.score.updated", "ui", "{\"value\":%d}" % _score)
	_label.text = _t("main.score_prefix") + str(_score)

func _on_lose_hp() -> void:
	_hp = max(0, _hp - 5)
	var demo = get_node_or_null("/root/Main/EngineDemo")
	if demo != null and demo.has_method("ApplyDamage"):
		demo.ApplyDamage(5)
	else:
		var bus = _bus()
		if bus != null:
			bus.PublishSimple("core.health.updated", "ui", "{\"value\":%d}" % _hp)
	_label.text = _t("main.hp_prefix") + str(_hp)

func _on_domain_event(type: String, _source: String, _data_json: String, _id: String, _spec: String, _ct: String, _ts: String) -> void:
	if type == "ui.menu.start":
		var demo = get_node_or_null("/root/Main/EngineDemo")
		if demo != null and demo.has_method("StartGame"):
			demo.StartGame()
		var nav = get_node_or_null("/root/Main/ScreenNavigator")
		if nav != null and nav.has_method("SwitchTo"):
			if ResourceLoader.exists("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn"):
				var ok: Variant = nav.SwitchTo("res://Game.Godot/Scenes/Screens/BattleMapScreen.tscn")
				if bool(ok):
					var menu = get_node_or_null("/root/Main/RuntimeUi/MainMenu")
					if menu != null and menu.has_method("HideMenu"):
						menu.HideMenu()
				else:
					push_warning("SwitchTo BattleMapScreen failed, keep MainMenu visible.")
	elif type == "ui.menu.settings":
		var sp = get_node_or_null("/root/Main/RuntimeUi/SettingsPanel")
		if sp != null and sp.has_method("ShowPanel"):
			sp.ShowPanel()
	elif type == "ui.menu.quit":
		get_tree().quit()

func _normalize_locale(locale: String) -> String:
	var v := locale.strip_edges().to_lower()
	if v == "zh" or v.begins_with("zh"):
		return "zh-CN"
	return "en-US"

func _t(key: String) -> String:
	var text: String = str(_i18n.translate(key))
	return text if text != key else key
