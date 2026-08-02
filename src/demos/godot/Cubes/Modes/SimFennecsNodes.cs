// SPDX-License-Identifier: MIT

using System;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace fennecs.demos.godot.Cubes;

/// <summary>
///     Mode c) fennecs + Nodes: cube state lives in Components (Position, index, and the cube's own
///     MeshInstance3D as a reference-type Component). A single-threaded Stream.For runs the motion
///     math and writes each Entity's node Transform. Driven via Call() by <see cref="DemoCubes" />.
/// </summary>
public partial class SimFennecsNodes : Node3D
{
    [Export] public Mesh CubeMesh = null!;

    private readonly World _world = new();
    private Stream<Vector3, int, MeshInstance3D> _stream;
    private int _prevVisible;


    public override void _Ready()
    {
        _stream = _world.Query<Vector3, int, MeshInstance3D>().Stream();
    }


    public void Activate() => Visible = true;


    public void Deactivate()
    {
        _stream.For((ref Vector3 _, ref int _, ref MeshInstance3D node) => node.QueueFree());
        _stream.Query.Despawn();
        _prevVisible = 0;
        Visible = false;
    }


    public void SetSimulatedCount(int count)
    {
        // Spawn missing cubes; each Entity carries its own node. Indices stay contiguous 0..n-1.
        for (var i = _stream.Count; i < count; i++)
        {
            var node = new MeshInstance3D { Mesh = CubeMesh, Visible = false };
            AddChild(node);
            _world.Spawn().Add<Vector3>().Add(i).Add(node);
        }

        // Remove excess cubes; Despawns are deferred until the runner's scope ends.
        if (_stream.Count > count)
        {
            _stream.For((in EntityRef entity, ref Vector3 _, ref int index, ref MeshInstance3D node) =>
            {
                if (index < count) return;
                node.QueueFree();
                entity.Despawn();
            });
        }

        _prevVisible = Math.Min(_prevVisible, count);
    }


    public void UpdateSim(float time, Godot.Vector3 amplitude, float cubeCount, float dt)
    {
        var visibleCount = (int) cubeCount;

        // Godot nodes are not thread-safe, so this mode runs a single-threaded For (never a Job).
        _stream.For(
            uniform: (time,
                new Vector3(amplitude.X, amplitude.Y, amplitude.Z),
                cubeCount,
                Basis.FromScale(Godot.Vector3.One * CubeMotion.CubeScale(cubeCount)),
                visibleCount,
                _prevVisible,
                dt),
            action: UpdateCube);

        _prevVisible = visibleCount;
    }


    private static void UpdateCube(
        (float Time, Vector3 Amplitude, float CubeCount, Basis Basis, int Visible, int PrevVisible, float Dt) uniform,
        ref Vector3 position,
        ref int index,
        ref MeshInstance3D node)
    {
        CubeMotion.Simulate(index, uniform.Time, uniform.CubeCount, uniform.Dt, ref position);

        // Every node gets simulated and positioned; visibility only toggles across the threshold.
        var show = index < uniform.Visible;
        if (show != index < uniform.PrevVisible) node.Visible = show;

        var translated = position * uniform.Amplitude;
        node.Transform = new Transform3D(uniform.Basis, new Godot.Vector3(translated.X, translated.Y, translated.Z));
    }
}
