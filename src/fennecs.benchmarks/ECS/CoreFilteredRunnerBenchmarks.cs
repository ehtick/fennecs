using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

using fennecs;

namespace Benchmark.ECS;

/// <summary>
/// Compares manual branching with FilteredStream predicate dispatch and filtered Job orchestration.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[HideColumns("Job", "Error", "RatioSD")]
public class CoreFilteredRunnerBenchmarks
{
    private struct Item
    {
        public int Bucket;
        public int Value;
    }

    [Params(0, 1, 50, 100)]
    public int SelectivityPercent { get; set; }

    private const int EntityCount = 100_000;

    private World _world = null!;
    private Stream<Item> _stream;
    private FilteredStream<Item> _filtered;

    [GlobalSetup]
    public void Setup()
    {
        _world = new(EntityCount);
        using var template = _world.Template().Needs<Item>();
        template.Spawn(EntityCount, i => new Item { Bucket = i % 100 });

        _stream = _world.Query<Item>().Stream();
        _filtered = _stream.Where(Passes);
    }

    private bool Passes(in Item item) => item.Bucket < SelectivityPercent;

    [GlobalCleanup]
    public void Cleanup() => _world.Dispose();

    [Benchmark(Baseline = true)]
    public void Manual_For() => _stream.For(SelectivityPercent, static (int threshold, ref Item item) =>
    {
        if (item.Bucket < threshold) item.Value++;
    });

    [Benchmark]
    public void Filtered_For() => _filtered.For(static (ref Item item) => item.Value++);

    [Benchmark]
    public void Manual_Job() => _stream.Job(SelectivityPercent, static (int threshold, ref Item item) =>
    {
        if (item.Bucket < threshold) item.Value++;
    });

    [Benchmark]
    public void Filtered_Job() => _filtered.Job(static (ref Item item) => item.Value++);
}
