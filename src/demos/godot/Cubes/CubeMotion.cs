// SPDX-License-Identifier: MIT

using System;
using Vector3 = System.Numerics.Vector3;

namespace fennecs.demos.godot.Cubes;

/// <summary>
///     The cube motion math shared by all demo modes, so every mode renders the exact same animation.
///     The GDScript modes use the identical port in Modes/cube_motion.gd — keep both in sync!
/// </summary>
public static class CubeMotion
{
    // Config: Maximum # of Entities that can be spawned.
    public const int MaxEntities = 100_000;


    /// <summary>
    ///     Advance one cube's smoothed position by one frame of chaotic Lissajous-like motion.
    /// </summary>
    public static void Simulate(int index, float time, float cubeCount, float dt, ref Vector3 position)
    {
        // The motion equations divide by cubeCount; clamp so the sim stays finite with 0 visible.
        cubeCount = MathF.Max(cubeCount, 1f);

        var motionIndex = (index + time * float.Tau * 69f) % cubeCount - cubeCount / 2f;

        var entityRatio = cubeCount / MaxEntities;

        var phase1 = motionIndex / 3f * MathF.Sin(motionIndex / 1500f * float.Tau) * 7f * float.Tau / cubeCount;
        var phase2 = motionIndex / 3f * MathF.Sin(motionIndex / 1700f * float.Tau) * (MathF.Sin(time * 23f) + 1.5f) * 5f * float.Tau / cubeCount;
        var phase3 = motionIndex / 3f * MathF.Sin(motionIndex / 1000f * float.Tau) * (MathF.Sin(time * 13f) + 1.5f) * 11f * entityRatio * float.Tau / cubeCount;

        var vector = new Vector3
        {
            X = MathF.Sin(phase1 + time * 500f + motionIndex / 150f),
            Y = MathF.Sin(phase2 + time * 500f + motionIndex / 100f),
            Z = MathF.Sin(phase3 + time * 500f + motionIndex / 200f),
        };

        var cubic = MathF.Sin(time * 300f * float.Tau) * 0.5f + 0.5f;
        var shell = Math.Clamp(vector.Length(), 0f, 1f);
        vector = (1.0f - cubic) * shell * vector / vector.Length() + cubic * vector;

        // Smooth the position to illustrate accumulative operations using data from the past frame.
        position = Fir(position, vector, 0.99f, dt);
    }


    /// <summary>
    ///     Uniform cube scale, between 1 and 3: cubes shrink as more of them become visible.
    /// </summary>
    public static float CubeScale(float cubeCount) => 2f * (1.5f - MathF.Sqrt(cubeCount / MaxEntities));


    /// <summary>
    ///     Calculation: A basic finite impulse response filter... for Vectors!
    /// </summary>
    private static Vector3 Fir(Vector3 from, Vector3 to, float k, float dt)
    {
        var exponent = dt * 120f; // reference frame rate, it's 2026, for fox’s sake!

        var alpha = MathF.Pow(k, exponent);

        return alpha * from + to * (1.0f - alpha);
    }
}
