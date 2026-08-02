# SPDX-License-Identifier: MIT

extends Label

var smoothed : float = 0.01
@onready var ECS : DemoCubes = %Demo
@onready var VisibleSlider : VSlider = %VisibleSlider

var last : int

func _process(_delta: float) -> void:
	# Must get raw time, as deltatime is capped by Godot to ~100 ms
	var delta := (Time.get_ticks_usec() - last) / 1_000_000.0
	last = Time.get_ticks_usec()

	var alpha := pow(0.95, delta * 120.0)
	smoothed = smoothed * alpha + delta * (1.0 - alpha)

	var fps := floori(1.0 / smoothed)
	var fps_text := "%d fps" % fps
	var entities_text := "%d entities" % ECS.QueryCount
	var visible_text := "%3.0f" % (VisibleSlider.value*100) + "% visible"
	self.text = fps_text + '\n' + entities_text + '\n' +  visible_text
