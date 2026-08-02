# SPDX-License-Identifier: MIT
class_name CubeMotion extends RefCounted

## The cube motion math for the GDScript demo modes.
## Identical port of ../CubeMotion.cs — keep both in sync!

const MAX_ENTITIES := 313370


## Advance one cube's smoothed position by one frame of chaotic Lissajous-like motion.
static func simulate(index: int, time: float, cube_count: float, dt: float, position: Vector3) -> Vector3:
	var motion_index := fmod(index + time * TAU * 69.0, cube_count) - cube_count / 2.0

	var entity_ratio := cube_count / MAX_ENTITIES

	var phase1 := motion_index / 3.0 * sin(motion_index / 1500.0 * TAU) * 7.0 * TAU / cube_count
	var phase2 := motion_index / 3.0 * sin(motion_index / 1700.0 * TAU) * (sin(time * 23.0) + 1.5) * 5.0 * TAU / cube_count
	var phase3 := motion_index / 3.0 * sin(motion_index / 1000.0 * TAU) * (sin(time * 13.0) + 1.5) * 11.0 * entity_ratio * TAU / cube_count

	var vector := Vector3(
		sin(phase1 + time * 500.0 + motion_index / 150.0),
		sin(phase2 + time * 500.0 + motion_index / 100.0),
		sin(phase3 + time * 500.0 + motion_index / 200.0))

	var cubic := sin(time * 300.0 * TAU) * 0.5 + 0.5
	var shell := clampf(vector.length(), 0.0, 1.0)
	vector = (1.0 - cubic) * shell * vector / vector.length() + cubic * vector

	# Smooth the position to illustrate accumulative operations using data from the past frame.
	return _fir(position, vector, 0.99, dt)


## Uniform cube scale, between 1 and 3: cubes shrink as more of them become visible.
static func cube_scale(cube_count: float) -> float:
	return 2.0 * (1.5 - sqrt(cube_count / MAX_ENTITIES))


static func _fir(from: Vector3, to: Vector3, k: float, dt: float) -> Vector3:
	var alpha := pow(k, dt * 120.0) # reference frame rate 120 fps, it's 2026, for fox's sake!
	return alpha * from + to * (1.0 - alpha)
