// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace fennecs.demos.godot.Cubes;

/// <summary>
///     Mode b) C# + Nodes: every cube is its own MeshInstance3D, moved one by one from a
///     C# loop over plain arrays. Driven via Call() by <see cref="DemoCubes" />.
/// </summary>
public partial class SimCsNodes : Node3D
{
    [Export] public Mesh CubeMesh = null!;

    private readonly List<MeshInstance3D> _nodes = [];
    private Vector3[] _positions = [];
    private int _prevVisible;


    public void Activate() => Visible = true;


    public void Deactivate()
    {
        foreach (var node in _nodes) node.QueueFree();
        _nodes.Clear();
        _positions = [];
        _prevVisible = 0;
        Visible = false;
    }


    public void SetSimulatedCount(int count)
    {
        while (_nodes.Count < count)
        {
            var node = new MeshInstance3D { Mesh = CubeMesh, Visible = false };
            AddChild(node);
            _nodes.Add(node);
        }

        while (_nodes.Count > count)
        {
            _nodes[^1].QueueFree();
            _nodes.RemoveAt(_nodes.Count - 1);
        }

        Array.Resize(ref _positions, count);
        _prevVisible = Math.Min(_prevVisible, count);
    }


    public void UpdateSim(float time, Godot.Vector3 amplitude, float cubeCount, float dt)
    {
        var amp = new Vector3(amplitude.X, amplitude.Y, amplitude.Z);
        var visibleCount = (int) cubeCount;
        var cubeBasis = Basis.FromScale(Godot.Vector3.One * CubeMotion.CubeScale(cubeCount));

        // Simulate all cubes (visible or not), but only touch the nodes that are on screen.
        for (var i = 0; i < _nodes.Count; i++)
        {
            CubeMotion.Simulate(i, time, cubeCount, dt, ref _positions[i]);

            var show = i < visibleCount;
            if (show != i < _prevVisible) _nodes[i].Visible = show;
            if (!show) continue;

            var position = _positions[i] * amp;
            _nodes[i].Transform = new Transform3D(cubeBasis, new Godot.Vector3(position.X, position.Y, position.Z));
        }

        _prevVisible = visibleCount;
    }
}
