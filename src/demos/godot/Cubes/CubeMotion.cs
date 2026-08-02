// SPDX-License-Identifier: MIT

using Godot;
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
        var motionIndex = (index + time * Mathf.Tau * 69f) % cubeCount - cubeCount / 2f;

        var entityRatio = cubeCount / MaxEntities;

        var phase1 = motionIndex / 3f * Mathf.Sin(motionIndex / 1500f * Mathf.Tau) * 7f * Mathf.Tau / cubeCount;
        var phase2 = motionIndex / 3f * Mathf.Sin(motionIndex / 1700f * Mathf.Tau) * (Mathf.Sin(time * 23f) + 1.5f) * 5f * Mathf.Tau / cubeCount;
        var phase3 = motionIndex / 3f * Mathf.Sin(motionIndex / 1000f * Mathf.Tau) * (Mathf.Sin(time * 13f) + 1.5f) * 11f * entityRatio * Mathf.Tau / cubeCount;

        var vector = new Vector3
        {
            X = Mathf.Sin(phase1 + time * 500f + motionIndex / 150f),
            Y = Mathf.Sin(phase2 + time * 500f + motionIndex / 100f),
            Z = Mathf.Sin(phase3 + time * 500f + motionIndex / 200f),
        };

        var cubic = Mathf.Sin(time * 300f * Mathf.Tau) * 0.5f + 0.5f;
        var shell = Mathf.Clamp(vector.Length(), 0, 1);
        vector = (1.0f - cubic) * shell * vector / vector.Length() + cubic * vector;

        // Smooth the position to illustrate accumulative operations using data from the past frame.
        position = Fir(position, vector, 0.99f, dt);
    }


    /// <summary>
    ///     Uniform cube scale, between 1 and 3: cubes shrink as more of them become visible.
    /// </summary>
    public static float CubeScale(float cubeCount) => 2f * (1.5f - Mathf.Sqrt(cubeCount / MaxEntities));


    /// <summary>
    ///     Calculation: A basic finite impulse response filter... for Vectors!
    /// </summary>
    private static Vector3 Fir(Vector3 from, Vector3 to, float k, float dt)
    {
        var exponent = dt * 120f; // reference frame rate, it's 2026, for fox’s sake!

        var alpha = Mathf.Pow(k, exponent);

        return alpha * from + to * (1.0f - alpha);
    }
}
