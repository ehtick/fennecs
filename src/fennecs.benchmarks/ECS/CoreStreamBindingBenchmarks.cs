using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

using fennecs;

namespace Benchmark.ECS;

/// <summary>
/// Measures per-archetype stream binding and dispatch overhead without meaningful component work.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[HideColumns("Job", "Error", "RatioSD")]
public class CoreStreamBindingBenchmarks
{
    private record struct Component0(int Value);
    private record struct Component1(int Value);
    private record struct Component2(int Value);
    private record struct Component3(int Value);
    private record struct Component4(int Value);
    private record struct Fragment;

    [Params(1, 64, 1024)]
    public int ArchetypeCount { get; set; }

    private const int EntityCount = 65_536;

    private World _world = null!;
    private Stream<Component0> _stream1;
    private Stream<Component0, Component1> _stream2;
    private Stream<Component0, Component1, Component2, Component3, Component4> _stream5;

    [GlobalSetup]
    public void Setup()
    {
        _world = new(EntityCount + ArchetypeCount);
        var targets = new Entity[ArchetypeCount];
        for (var i = 0; i < targets.Length; i++) targets[i] = _world.Spawn();

        var entitiesPerArchetype = EntityCount / ArchetypeCount;
        var remainder = EntityCount % ArchetypeCount;
        for (var i = 0; i < targets.Length; i++)
        {
            using var template = _world.Template()
                .Add(new Component0(1))
                .Add(new Component1(2))
                .Add(new Component2(3))
                .Add(new Component3(4))
                .Add(new Component4(5))
                .Add(new Fragment(), targets[i]);
            template.Spawn(entitiesPerArchetype + (i < remainder ? 1 : 0));
        }

        _stream1 = _world.Query<Component0>().Stream();
        _stream2 = _world.Query<Component0, Component1>().Stream();
        _stream5 = _world.Query<Component0, Component1, Component2, Component3, Component4>().Stream();
        if (_stream1.Count != EntityCount) throw new InvalidOperationException("Benchmark entity count mismatch.");
    }

    [GlobalCleanup]
    public void Cleanup() => _world.Dispose();

    [Benchmark(Baseline = true)]
    public void Raw_Arity1() => _stream1.Raw(static _ => { });

    [Benchmark]
    public void Raw_Arity2() => _stream2.Raw(static (_, _) => { });

    [Benchmark]
    public void Raw_Arity5() => _stream5.Raw(static (_, _, _, _, _) => { });

    [Benchmark]
    public void For_Arity1() => _stream1.For(static (ref Component0 _) => { });

    [Benchmark]
    public void For_Arity2() => _stream2.For(static (ref Component0 _, ref Component1 _) => { });

    [Benchmark]
    public void For_Arity5() => _stream5.For(static (ref Component0 _, ref Component1 _, ref Component2 _,
        ref Component3 _, ref Component4 _) => { });
}
