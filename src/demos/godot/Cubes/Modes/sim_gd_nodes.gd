# SPDX-License-Identifier: MIT
extends Node3D

## Mode a) GDScript + Nodes: every cube is its own MeshInstance3D, moved one by one from GDScript.
## Method names are PascalCase to share a Call() convention with the C# sims.

@export var cube_mesh: Mesh

var _nodes: Array[MeshInstance3D] = []
var _positions := PackedVector3Array()
var _prev_visible := 0


func Activate() -> void:
	visible = true


func Deactivate() -> void:
	for node in _nodes:
		node.queue_free()
	_nodes.clear()
	_positions.resize(0)
	_prev_visible = 0
	visible = false


func SetSimulatedCount(count: int) -> void:
	while _nodes.size() < count:
		var node := MeshInstance3D.new()
		node.mesh = cube_mesh
		node.visible = false
		add_child(node)
		_nodes.append(node)
	while _nodes.size() > count:
		_nodes.pop_back().queue_free()
	_positions.resize(count)
	_prev_visible = mini(_prev_visible, count)


func UpdateSim(time: float, amplitude: Vector3, cube_count: float, dt: float) -> void:
	var visible_count := int(cube_count)
	var cube_basis := Basis.IDENTITY.scaled(Vector3.ONE * CubeMotion.cube_scale(cube_count))

	# Every node gets simulated and positioned; visibility only toggles across the threshold.
	for i in _nodes.size():
		var pos := CubeMotion.simulate(i, time, cube_count, dt, _positions[i])
		_positions[i] = pos

		var node := _nodes[i]
		var shown := i < visible_count
		if shown != (i < _prev_visible):
			node.visible = shown
		node.transform = Transform3D(cube_basis, pos * amplitude)

	_prev_visible = visible_count
