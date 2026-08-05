using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

using fennecs;

namespace Benchmark.ECS;

/// <summary>
/// Measures storage cleanup costs for reference-free and reference-containing component types.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[HideColumns("Job", "Error", "RatioSD")]
public class CoreStorageLifecycleBenchmarks
{
    [InlineArray(16)]
    private struct Value128
    {
        private long _element0;
    }

    [InlineArray(16)]
    private struct Reference128
    {
        private object? _element0;
    }

    [Params(1_024, 65_536)]
    public int ElementCount { get; set; }

    private readonly object _reference = new();

    private Storage<int> _ints = null!;
    private Storage<Value128> _values = null!;
    private Storage<object> _objects = null!;
    private Storage<Reference128> _references = null!;
    private Storage<int> _intDestination = null!;
    private Storage<Reference128> _referenceDestination = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ints = new();
        _values = new();
        _objects = new();
        _references = new();
        _intDestination = new();
        _referenceDestination = new();

        var reference128 = new Reference128();
        reference128[..].Fill(_reference);
        _ints.EnsureCapacity(ElementCount);
        _values.EnsureCapacity(ElementCount);
        _objects.EnsureCapacity(ElementCount);
        _references.EnsureCapacity(ElementCount);
        _intDestination.EnsureCapacity(ElementCount);
        _referenceDestination.EnsureCapacity(ElementCount);

        _intDestination.Append(1, ElementCount);
        _referenceDestination.Append(reference128, ElementCount);
    }

    [Benchmark]
    public void Clear_Int32()
    {
        _ints.Append(1, ElementCount);
        _ints.Clear();
    }

    [Benchmark]
    public void Clear_Value128()
    {
        _values.Append(default, ElementCount);
        _values.Clear();
    }

    [Benchmark]
    public void Clear_Object()
    {
        _objects.Append(_reference, ElementCount);
        _objects.Clear();
    }

    [Benchmark]
    public void Clear_Reference128()
    {
        var value = new Reference128();
        value[..].Fill(_reference);
        _references.Append(value, ElementCount);
        _references.Clear();
    }

    [Benchmark]
    public void Delete_Int32()
    {
        _ints.Append(1, ElementCount);
        _ints.Delete(0, ElementCount);
    }

    [Benchmark]
    public void Delete_Reference128()
    {
        var value = new Reference128();
        value[..].Fill(_reference);
        _references.Append(value, ElementCount);
        _references.Delete(0, ElementCount);
    }

    [Benchmark]
    public void Migrate_Int32()
    {
        _intDestination.Migrate(_ints);
        _ints.Migrate(_intDestination);
    }

    [Benchmark]
    public void Migrate_Reference128()
    {
        _referenceDestination.Migrate(_references);
        _references.Migrate(_referenceDestination);
    }
}
