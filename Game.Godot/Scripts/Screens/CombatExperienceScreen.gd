extends Control

@onready var _summary: Label = $Margin/VBox/Summary
@onready var _bridge: Node = $CombatExperienceRuntimeBridge
@onready var _reset_btn: Button = $Margin/VBox/ButtonRow1/ResetBtn
@onready var _build_btn: Button = $Margin/VBox/ButtonRow1/BuildBtn
@onready var _train_btn: Button = $Margin/VBox/ButtonRow1/TrainBtn
@onready var _spawn_btn: Button = $Margin/VBox/ButtonRow1/SpawnBtn
@onready var _exchange_btn: Button = $Margin/VBox/ButtonRow2/ExchangeBtn
@onready var _cleanup_btn: Button = $Margin/VBox/ButtonRow2/CleanupBtn
@onready var _outcome_btn: Button = $Margin/VBox/ButtonRow2/OutcomeBtn
@onready var _run_all_btn: Button = $Margin/VBox/ButtonRow2/RunAllBtn

func _ready() -> void:
	_reset_btn.pressed.connect(_on_reset)
	_build_btn.pressed.connect(_on_build)
	_train_btn.pressed.connect(_on_train)
	_spawn_btn.pressed.connect(_on_spawn_enemy)
	_exchange_btn.pressed.connect(_on_exchange)
	_cleanup_btn.pressed.connect(_on_cleanup)
	_outcome_btn.pressed.connect(_on_outcome)
	_run_all_btn.pressed.connect(_on_run_all)
	_on_reset()

func _on_reset() -> void:
	_bridge.ResetForInteractiveRun()
	_render(_bridge.GetSummary())

func _on_build() -> void:
	_render(_bridge.BuildPhase())

func _on_train() -> void:
	_render(_bridge.TrainFriendlyUnitPhase())

func _on_spawn_enemy() -> void:
	_render(_bridge.SpawnEnemyWavePhase())

func _on_exchange() -> void:
	_render(_bridge.ResolveCombatExchangePhase())

func _on_cleanup() -> void:
	_render(_bridge.CleanupDeadUnitsPhase())

func _on_outcome() -> void:
	_render(_bridge.PublishOutcomePhase())

func _on_run_all() -> void:
	_render(_bridge.RunCompleteCombatExperienceForTest())

func _render(result: Dictionary) -> void:
	var built := result.get("mg_tower_built", false) == true and result.get("barracks_built", false) == true
	var friendly := int(result.get("friendly_units_deployed", 0))
	var enemies := int(result.get("enemy_units_spawned", 0))
	var projectiles := int(result.get("projectiles_created", 0))
	var exchanges := int(result.get("combat_exchanges", 0))
	var retired := int(result.get("dead_units_retired", 0))
	var active := int(result.get("active_combat_nodes_after_cleanup", 0))
	var dead_targetable := result.get("dead_unit_targetable_after_cleanup", false) == true

	_summary.text = "Combat Experience Live\n" \
		+ "Built: %s\n" % ("yes" if built else "no") \
		+ "Friendly Units: %d\n" % friendly \
		+ "Enemy Units: %d\n" % enemies \
		+ "Projectiles: %d\n" % projectiles \
		+ "Combat Exchanges: %d\n" % exchanges \
		+ "Dead Units Retired: %d\n" % retired \
		+ "Active Nodes After Cleanup: %d\n" % active \
		+ "Dead Unit Still Targetable: %s" % ("yes" if dead_targetable else "no")
