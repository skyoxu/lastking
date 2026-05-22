extends Control

var center_point: Vector2 = Vector2.ZERO
var radius_px: float = 0.0

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_preset(Control.PRESET_FULL_RECT)

func _draw() -> void:
	if radius_px <= 0.0:
		return
	draw_arc(center_point, radius_px, 0.0, TAU, 96, Color(0.980392, 0.827451, 0.356863, 0.92), 3.0, true)
	draw_circle(center_point, 4.0, Color(1.0, 0.929412, 0.584314, 0.98))
