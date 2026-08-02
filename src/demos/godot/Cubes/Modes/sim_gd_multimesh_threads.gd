# SPDX-License-Identifier: MIT
extends Node

## Mode e) GDScript + MultiMesh + Threads: like the GDScript MultiMesh mode, but the motion math
## is chunked and spread across all CPU cores via WorkerThreadPool group tasks.
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
	var count := _positions.size()

	if count > 0:
		# Each group task simulates one chunk; the tasks write to disjoint element ranges of
		# _positions and _buffer (no resizing!), so no locking is needed.
		var chunks := mini(count, OS.get_processor_count() * 4)
		var chunk_size := ceili(count / float(chunks))
		var task := _simulate_chunk.bind(chunk_size, count, time, amplitude, cube_count, dt, CubeMotion.cube_scale(cube_count))

		if OS.has_feature("web"):
			# Threads are unavailable on the web platform; run the chunks serially instead.
			for chunk in chunks:
				task.call(chunk)
		else:
			var group := WorkerThreadPool.add_group_task(task, chunks, -1, true, "cube motion")
			WorkerThreadPool.wait_for_group_task_completion(group)

	mesh_instance.multimesh.instance_count = visible_count

	# Submitting an empty buffer is illegal in the Godot API. (main thread only!)
	if visible_count > 0:
		RenderingServer.multimesh_set_buffer(mesh_instance.multimesh.get_rid(), _buffer.slice(0, visible_count * 12))


func _simulate_chunk(chunk_index: int, chunk_size: int, count: int, time: float, amplitude: Vector3, cube_count: float, dt: float, s: float) -> void:
	var first := chunk_index * chunk_size
	var last := mini(first + chunk_size, count)

	for i in range(first, last):
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
