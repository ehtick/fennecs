// SPDX-License-Identifier: MIT

using System;
using Godot;

namespace fennecs.demos.godot.Cubes;

/// <summary>
///     <para>
///         DemoCubes (Godot version)
///     </para>
///     <para>
///         All motion is 100% CPU simulation (no GPU). The identical simulation runs in seven
///         interchangeable modes — GDScript, plain C#, or fennecs; rendered either through
///         individual MeshInstance3D nodes or a single MultiMesh — selectable at runtime.
///     </para>
///     <para>
///         This node owns the shared state (time, amplitude, entity counts, sliders) and forwards
///         the per-frame work to the active mode sim in Modes/. All sims share the same calling
///         convention: Activate / Deactivate / SetSimulatedCount / UpdateSim.
///     </para>
/// </summary>
[GlobalClass]
[Icon("res://icon.svg")]
public partial class DemoCubes : Node
{
	// Calculation: Internal Speed of the Simulation.
	private const float BaseTimeScale = 0.0003f;

	private static readonly StringName UpdateSimName = "UpdateSim";

	// Godot: Exports to interact with the UI
	[Export] public Camera3D Camera = null!;

	// Godot: The MultiMeshInstance3D shared by all MultiMesh modes.
	[Export] public MultiMeshInstance3D MeshInstance = null!;

	// Config: Size of the simulation space
	[Export] public float MaxAmplitude = 400;
	[Export] public float MinAmplitude = 250;
	[Export] public Slider RenderedSlider = null!;
	[Export] public Slider SimulatedSlider = null!;
	[Export] public RichTextLabel InfoText = null!;

	// Godot: The seven mode sims, in button order (a..g).
	[Export] public Node SimGdNodes = null!;
	[Export] public Node SimCsNodes = null!;
	[Export] public Node SimFennecsNodes = null!;
	[Export] public Node SimGdMultimesh = null!;
	[Export] public Node SimCsMultimesh = null!;
	[Export] public Node SimFennecsFor = null!;
	[Export] public Node SimFennecsJobs = null!;

	// Godot: Read by the UI to show the simulated Entity count. (not just the visible ones)
	public int QueryCount { get; private set; }

	private Node[] _sims = [];
	private int _mode = 6; // default: g) fennecs + MultiMesh + Jobs

	// Calculation: Smoothed values for the simulation.
	private float _time;
	private float _currentTimeScale = BaseTimeScale;
	private float _currentRenderedFraction;
	private Vector3 _currentAmplitude;
	private Vector3 _goalAmplitude;

	private const string InfoCommon = "All motion is 100% CPU simulation (no GPU). Rendered output and motion are identical in every mode - only the machinery differs.\n\n";

	private const string InfoNodeWarning = "\n\nSpawning tens of thousands of Nodes takes a while - mind the sliders!";

	private static readonly string[] ModeInfos =
	[
		InfoCommon + "a) GDScript + Nodes\n\nEvery cube is its own MeshInstance3D. A GDScript loop runs the motion math and writes each node's Transform3D, one by one, every frame." + InfoNodeWarning,
		InfoCommon + "b) C# + Nodes\n\nEvery cube is its own MeshInstance3D. A C# loop over plain arrays runs the motion math and writes each node's Transform3D, one by one, every frame." + InfoNodeWarning,
		InfoCommon + "c) fennecs + Nodes\n\nCube state lives in Components on fennecs Entities - Position, index, and the cube's own MeshInstance3D as a reference-type Component. A single Stream.For runs the motion math and writes each Entity's node Transform3D." + InfoNodeWarning,
		InfoCommon + "d) GDScript + MultiMesh\n\nPositions live in a PackedVector3Array. A GDScript loop runs the motion math and fills a PackedFloat32Array that is submitted to a MultiMesh in a single call.",
		InfoCommon + "e) C# + MultiMesh\n\nState lives in plain C# arrays. A single-threaded loop runs the motion math and fills a Matrix4x3 buffer that is submitted to a MultiMesh in a single call.",
		InfoCommon + "f) fennecs + MultiMesh\n\nState is stored in Components on the Entities:\n[ul]\n1x System.Numerics.Vector3 (as Position)\n1x Matrix4x3 (custom struct, as Transform)\n1x integer (as a simple identifier)\n[/ul]\nA single-threaded Stream.For runs the motion math, then Stream.Raw submits the raw Matrix4x3 structs directly to a MultiMesh.",
		InfoCommon + "g) fennecs + MultiMesh + Jobs\n\nState is stored in Components on the Entities:\n[ul]\n1x System.Numerics.Vector3 (as Position)\n1x Matrix4x3 (custom struct, as Transform)\n1x integer (as a simple identifier)\n[/ul]\nA parallel Stream.Job spreads the motion math across all CPU cores, then Stream.Raw submits the raw Matrix4x3 structs directly to a MultiMesh.",
	];


	/// <summary>
	///     Godot _Ready() method, sets up our simulation.
	/// </summary>
	public override void _Ready()
	{
		_sims = [SimGdNodes, SimCsNodes, SimFennecsNodes, SimGdMultimesh, SimCsMultimesh, SimFennecsFor, SimFennecsJobs];

		// Boilerplate: Put all sims in a known state, then bring up the default mode.
		foreach (var sim in _sims) sim.Call("Deactivate");
		_sims[_mode].Call("Activate");
		InfoText.Text = ModeInfos[_mode];

		// Boilerplate: Apply the initial state of the UI.
		_on_simulated_slider_value_changed(SimulatedSlider.Value);
		_on_rendered_slider_value_changed(RenderedSlider.Value);
	}


	/// <summary>
	///     Advances the shared simulation state and delegates the frame to the active mode sim.
	/// </summary>
	/// <param name="delta">time elapsed since last tick, in seconds</param>
	public override void _Process(double delta)
	{
		// Calculation: Convert the delta time to a float (preferred use here).
		var dt = (float) delta;

		// Calculation: Accumulate the total elapsed time by adding the current frame time.
		_time += dt * _currentTimeScale;

		// Calculation: Determine the number of entities that will be displayed (also used to smooth out animation).
		var cubeCount = Mathf.FloorToInt(_currentRenderedFraction * QueryCount);

		// Make the cloud of cubes denser if there are more cubes.
		var amplitudePortion = Mathf.Clamp(1.0f - QueryCount / (float) CubeMotion.MaxEntities, 0f, 1f);
		_goalAmplitude = Mathf.Lerp(MinAmplitude, MaxAmplitude, amplitudePortion) * Vector3.One;
		_currentAmplitude = _currentAmplitude * 0.9f + 0.1f * _goalAmplitude;

		// The active mode sim runs the motion math and hands the results to Godot's renderer.
		_sims[_mode].Call(UpdateSimName, _time, _currentAmplitude, (float) cubeCount, dt);
	}


	#region Signal Handlers

	/// <summary>
	///     Godot: Signal Handler (mode buttons, bound with the mode index)
	/// </summary>
	private void _on_mode_selected(int mode)
	{
		if (mode == _mode) return;

		_sims[_mode].Call("Deactivate");
		_mode = mode;
		_sims[_mode].Call("Activate");
		_sims[_mode].Call("SetSimulatedCount", QueryCount);

		InfoText.Text = ModeInfos[_mode];
	}


	/// <summary>
	///     Godot: Signal Handler
	/// </summary>
	private void _on_rendered_slider_value_changed(double value)
	{
		// Set the number of entities to render
		_currentRenderedFraction = (float) value;

		// Move cubes faster if there are fewer visible
		_currentTimeScale = BaseTimeScale / Mathf.Max((float) value, 0.3f);
	}


	/// <summary>
	///     Godot: Signal Handler
	/// </summary>
	private void _on_simulated_slider_value_changed(double value)
	{
		// Set the number of entities to simulate
		var count = (int) Math.Ceiling(Math.Pow(value, Mathf.Sqrt2) * CubeMotion.MaxEntities);
		count = Math.Clamp((count / 100 + 1) * 100, 0, CubeMotion.MaxEntities);

		QueryCount = count;
		_sims[_mode].Call("SetSimulatedCount", count);
	}

	#endregion
}
