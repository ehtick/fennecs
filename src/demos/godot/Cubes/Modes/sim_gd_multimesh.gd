# SPDX-License-Identifier: MIT
extends Node

## Mode d) GDScript + MultiMesh: positions in a PackedVector3Array, transforms written into a
## PackedFloat32Array and submitted to the MultiMesh in one call.
## Method names are PascalCase to share a Call() convention with the C# sims.

@export var mesh_instance: MultiMeshInstance3D

var _positions := PackedVector3Array()
var _buffer := PackedFloat32Array()


func Activate() -> void:
	mesh_instance.visible = true


func Deactivate() -> void:
	_positions.resize(0)
	_buffer.resize(0)
	mesh_instance.visible = false


func SetSimulatedCount(count: int) -> void:
	_positions.resize(count)
	_buffer.resize(count * 12)


func UpdateSim(time: float, amplitude: Vector3, cube_count: float, dt: float) -> void:
	var visible_count := int(cube_count)
	var s := CubeMotion.cube_scale(cube_count)

	# Simulate all cubes and write a 3x4 transform (12 floats) each; only the visible
	# prefix of the buffer is submitted below.
	for i in _positions.size():
		var pos := CubeMotion.simulate(i, time, cube_count, dt, _positions[i])
		_positions[i] = pos

		pos *= amplitude
		var o := i * 12
		_buffer[o + 0] = s
		_buffer[o + 1] = 0.0
		_buffer[o + 2] = 0.0
		_buffer[o + 3] = pos.x
		_buffer[o + 4] = 0.0
		_buffer[o + 5] = s
		_buffer[o + 6] = 0.0
		_buffer[o + 7] = pos.y
		_buffer[o + 8] = 0.0
		_buffer[o + 9] = 0.0
		_buffer[o + 10] = s
		_buffer[o + 11] = pos.z

	mesh_instance.multimesh.instance_count = visible_count

	# Submitting an empty buffer is illegal in the Godot API.
	if visible_count > 0:
		RenderingServer.multimesh_set_buffer(mesh_instance.multimesh.get_rid(), _buffer.slice(0, visible_count * 12))
