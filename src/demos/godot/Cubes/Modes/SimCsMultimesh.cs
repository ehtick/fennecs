// SPDX-License-Identifier: MIT

using System;
using System.Runtime.InteropServices;
using Godot;
using Vector3 = System.Numerics.Vector3;

namespace fennecs.demos.godot.Cubes;

/// <summary>
///     Mode e) C# + MultiMesh: state in plain C# arrays, a single-threaded loop runs the motion math
///     and the Matrix4X3 buffer is submitted to the MultiMesh in one call.
///     Driven via Call() by <see cref="DemoCubes" />.
/// </summary>
public partial class SimCsMultimesh : Node
{
    [Export] public MultiMeshInstance3D MeshInstance = null!;

    private Vector3[] _positions = [];
    private Matrix4X3[] _transforms = [];


    public void Activate() => MeshInstance.Visible = true;


    public void Deactivate()
    {
        _positions = [];
        _transforms = [];
        MeshInstance.Visible = false;
    }


    public void SetSimulatedCount(int count)
    {
        Array.Resize(ref _positions, count);
        Array.Resize(ref _transforms, count);
    }


    public void UpdateSim(float time, Godot.Vector3 amplitude, float cubeCount, float dt)
    {
        var amp = new Vector3(amplitude.X, amplitude.Y, amplitude.Z);
        var scale = CubeMotion.CubeScale(cubeCount);

        // Simulate all cubes (visible or not); the excess transforms are simply not submitted.
        for (var i = 0; i < _positions.Length; i++)
        {
            CubeMotion.Simulate(i, time, cubeCount, dt, ref _positions[i]);
            _transforms[i] = new Matrix4X3(_positions[i] * amp, scale);
        }

        var visibleCount = (int) cubeCount;
        MeshInstance.Multimesh.InstanceCount = visibleCount;

        // Submitting an empty buffer is illegal in the Godot API.
        if (visibleCount == 0) return;

        var floatSpan = MemoryMarshal.Cast<Matrix4X3, float>(_transforms.AsSpan());
        RenderingServer.MultimeshSetBuffer(MeshInstance.Multimesh.GetRid(), floatSpan[..(visibleCount * Matrix4X3.SizeInFloats)]);
    }
}
