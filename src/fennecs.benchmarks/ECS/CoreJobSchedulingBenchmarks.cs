using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

using fennecs;

namespace Benchmark.ECS;

/// <summary>
/// Tracks the For/Job crossover and work-item growth as matching archetypes become fragmented.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[HideColumns("Job", "Error", "RatioSD")]
public class CoreJobSchedulingBenchmarks
{
    private struct Counter
    {
        public int Value;
    }

    private record struct Fragment;

    [Params(1, 64, 1024)]
    public int ArchetypeCount { get; set; }

    private const int EntityCount = 131_072;

    private World _world = null!;
    private Stream<Counter> _stream;

    [GlobalSetup]
    public void Setup()
    {
        _world = new(EntityCount + ArchetypeCount);
        var targets = new Entity[ArchetypeCount];
        for (var i = 0; i < targets.Length; i++) targets[i] = _world.Spawn();

        var entitiesPerArchetype = EntityCount / ArchetypeCount;
        for (var i = 0; i < targets.Length; i++)
        {
            using var template = _world.Template()
                .Add(new Counter())
                .Add(new Fragment(), targets[i]);
            template.Spawn(entitiesPerArchetype);
        }

        _stream = _world.Query<Counter>().Stream();
    }

    [GlobalCleanup]
    public void Cleanup() => _world.Dispose();

    [Benchmark(Baseline = true)]
    public void For_NoOp() => _stream.For(static (ref Counter _) => { });

    [Benchmark]
    public void Job_NoOp() => _stream.Job(static (ref Counter _) => { });

    [Benchmark]
    public void For_LightWrite() => _stream.For(static (ref Counter counter) => counter.Value++);

    [Benchmark]
    public void Job_LightWrite() => _stream.Job(static (ref Counter counter) => counter.Value++);
}
