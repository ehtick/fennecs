using System.Collections;
using System.Reflection;

namespace fennecs.tests;

public class CrossTests
{
    [Theory]
    [InlineData(new[] {1, 1, 1})]
    [InlineData(new[] {1, 1, 3})]
    [InlineData(new[] {1, 5, 1})]
    [InlineData(new[] {1, 1, 5})]
    [InlineData(new[] {5, 1, 1})]
    [InlineData(new[] {9, 5, 3})]
    [InlineData(new[] {42, 23, 69})]
    private void CrossJoin_Counts_All(int[] limiter)
    {
        int[] counter = [0, 0, 0];

        var count = 0;
        do
        {
            count++;
        } while (Cross.FullPermutation(counter, limiter));

        var product = limiter.Aggregate(1, (current, i) => current * i);
        Assert.Equal(product, count);
    }


    // Archetype with int, string, float, double, byte; long is deliberately absent to produce empty matches.
    private static World Setup(out Archetype archetype)
    {
        var world = new World();
        var entity = world.Spawn().Add(42).Add("fox").Add(1.5f).Add(2.5).Add((byte)7);
        archetype = world.GetEntityMeta(entity).Archetype;
        return world;
    }

    private static TypeExpression Plain<T>() => TypeExpression.Of<T>(Match.Plain);

    private static T GetField<T>(object join, string name) =>
        (T)join.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(join)!;


    [Fact]
    public void Join1_Empty_Iff_Unmatched()
    {
        using var world = Setup(out var archetype);
        using (var join = new Cross.Join<int>(archetype, [Plain<int>()])) Assert.False(join.Empty);
        using (var join = new Cross.Join<long>(archetype, [Plain<long>()])) Assert.True(join.Empty);
    }


    [Fact]
    public void Join2_Empty_Iff_Any_Position_Unmatched()
    {
        using var world = Setup(out var archetype);
        using (var join = new Cross.Join<int, string>(archetype, [Plain<int>(), Plain<string>()]))
            Assert.False(join.Empty);
        using (var join = new Cross.Join<long, string>(archetype, [Plain<long>(), Plain<string>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, long>(archetype, [Plain<int>(), Plain<long>()]))
            Assert.True(join.Empty);
    }


    [Fact]
    public void Join3_Empty_Iff_Any_Position_Unmatched()
    {
        using var world = Setup(out var archetype);
        using (var join = new Cross.Join<int, string, float>(archetype, [Plain<int>(), Plain<string>(), Plain<float>()]))
            Assert.False(join.Empty);
        using (var join = new Cross.Join<long, string, float>(archetype, [Plain<long>(), Plain<string>(), Plain<float>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, long, float>(archetype, [Plain<int>(), Plain<long>(), Plain<float>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, string, long>(archetype, [Plain<int>(), Plain<string>(), Plain<long>()]))
            Assert.True(join.Empty);
    }


    [Fact]
    public void Join4_Empty_Iff_Any_Position_Unmatched()
    {
        using var world = Setup(out var archetype);
        using (var join = new Cross.Join<int, string, float, double>(archetype, [Plain<int>(), Plain<string>(), Plain<float>(), Plain<double>()]))
            Assert.False(join.Empty);
        using (var join = new Cross.Join<long, string, float, double>(archetype, [Plain<long>(), Plain<string>(), Plain<float>(), Plain<double>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, long, float, double>(archetype, [Plain<int>(), Plain<long>(), Plain<float>(), Plain<double>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, string, long, double>(archetype, [Plain<int>(), Plain<string>(), Plain<long>(), Plain<double>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, string, float, long>(archetype, [Plain<int>(), Plain<string>(), Plain<float>(), Plain<long>()]))
            Assert.True(join.Empty);
    }


    [Fact]
    public void Join5_Empty_Iff_Any_Position_Unmatched()
    {
        using var world = Setup(out var archetype);
        using (var join = new Cross.Join<int, string, float, double, byte>(archetype, [Plain<int>(), Plain<string>(), Plain<float>(), Plain<double>(), Plain<byte>()]))
            Assert.False(join.Empty);
        using (var join = new Cross.Join<long, string, float, double, byte>(archetype, [Plain<long>(), Plain<string>(), Plain<float>(), Plain<double>(), Plain<byte>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, long, float, double, byte>(archetype, [Plain<int>(), Plain<long>(), Plain<float>(), Plain<double>(), Plain<byte>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, string, long, double, byte>(archetype, [Plain<int>(), Plain<string>(), Plain<long>(), Plain<double>(), Plain<byte>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, string, float, long, byte>(archetype, [Plain<int>(), Plain<string>(), Plain<float>(), Plain<long>(), Plain<byte>()]))
            Assert.True(join.Empty);
        using (var join = new Cross.Join<int, string, float, double, long>(archetype, [Plain<int>(), Plain<string>(), Plain<float>(), Plain<double>(), Plain<long>()]))
            Assert.True(join.Empty);
    }


    [Fact]
    public void Join_Iterates_All_Permutations_With_Wildcard_Storages()
    {
        using var world = new World();
        var t1 = world.Spawn();
        var t2 = world.Spawn();
        var entity = world.Spawn().Add(42).Add("fox").Add(1.5f).Add((byte)7);
        entity.Add(2.5, t1);
        entity.Add(3.5, t2); // Match.Any on double yields two storages -> two permutations

        var archetype = world.GetEntityMeta(entity).Archetype;

        using (var join = new Cross.Join<int, string, float, double>(archetype,
                   [Plain<int>(), Plain<string>(), Plain<float>(), TypeExpression.Of<double>(Match.Any)]))
        {
            Assert.False(join.Empty);
            var permutations = 0;
            do { permutations++; } while (join.Iterate());
            Assert.Equal(2, permutations);
        }

        using (var join = new Cross.Join<int, string, float, double, byte>(archetype,
                   [Plain<int>(), Plain<string>(), Plain<float>(), TypeExpression.Of<double>(Match.Any), Plain<byte>()]))
        {
            Assert.False(join.Empty);
            var permutations = 0;
            do { permutations++; } while (join.Iterate());
            Assert.Equal(2, permutations);
        }
    }


    [Fact]
    public void Default_Joins_Are_Empty_And_Dispose_Is_NoOp()
    {
        var join1 = default(Cross.Join<int>);
        Assert.True(join1.Empty);
        join1.Dispose();

        var join2 = default(Cross.Join<int, string>);
        Assert.True(join2.Empty);
        join2.Dispose();

        var join3 = default(Cross.Join<int, string, float>);
        Assert.True(join3.Empty);
        join3.Dispose();

        var join4 = default(Cross.Join<int, string, float, double>);
        Assert.True(join4.Empty);
        join4.Dispose();

        var join5 = default(Cross.Join<int, string, float, double, byte>);
        Assert.True(join5.Empty);
        join5.Dispose();
    }


    // Dispose must return the matched storage lists to their pool (which clears them). The pooled
    // lists are shared process-wide and can be re-rented by parallel tests right after disposal,
    // so retry with a fresh join — a Join that fails to dispose still holds its rows and never passes.
    private static void AssertDisposeClearsStorages(Func<IDisposable> makeJoin, params string[] storageFields)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var join = makeJoin();
            var lists = storageFields.Select(name => GetField<IEnumerable>(join, name)).ToArray();
            foreach (var list in lists) Assert.NotEmpty(list);

            join.Dispose();
            if (lists.All(list => !list.Cast<object>().Any())) return;
        }

        Assert.Fail("Join.Dispose did not return its storage lists to the pool.");
    }


    [Fact]
    public void Join_Dispose_Returns_Storage_Lists()
    {
        using var world = Setup(out var archetype);

        AssertDisposeClearsStorages(() =>
            new Cross.Join<int>(archetype, [Plain<int>()]),
            "_storages0");
        AssertDisposeClearsStorages(() =>
            new Cross.Join<int, string>(archetype, [Plain<int>(), Plain<string>()]),
            "_storages0", "_storages1");
        AssertDisposeClearsStorages(() =>
            new Cross.Join<int, string, float>(archetype, [Plain<int>(), Plain<string>(), Plain<float>()]),
            "_storages0", "_storages1", "_storages2");
        AssertDisposeClearsStorages(() =>
            new Cross.Join<int, string, float, double>(archetype, [Plain<int>(), Plain<string>(), Plain<float>(), Plain<double>()]),
            "_storages0", "_storages1", "_storages2", "_storages3");
        AssertDisposeClearsStorages(() =>
            new Cross.Join<int, string, float, double, byte>(archetype, [Plain<int>(), Plain<string>(), Plain<float>(), Plain<double>(), Plain<byte>()]),
            "_storages0", "_storages1", "_storages2", "_storages3", "_storages4");
    }


    // Dispose must return the counter/limiter arrays to Cross's ArrayPool: after a dispose, a fresh join
    // of the same arity should be handed the recycled arrays. Retried because other tests share the pool.
    private static void AssertDisposeRecyclesArrays(Func<IDisposable> makeJoin)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var first = makeJoin();
            var firstArrays = new[] { GetField<int[]>(first, "_counter"), GetField<int[]>(first, "_limiter") };
            first.Dispose();

            var second = makeJoin();
            var secondArrays = new[] { GetField<int[]>(second, "_counter"), GetField<int[]>(second, "_limiter") };
            second.Dispose();

            if (secondArrays.All(array => firstArrays.Contains(array))) return;
        }

        Assert.Fail("Join.Dispose did not return its counter/limiter arrays to the pool.");
    }


    [Fact]
    public void Join_Dispose_Recycles_Counter_Arrays()
    {
        using var world = Setup(out var archetype);

        AssertDisposeRecyclesArrays(() => new Cross.Join<int>(archetype,
            [Plain<int>()]));
        AssertDisposeRecyclesArrays(() => new Cross.Join<int, string>(archetype,
            [Plain<int>(), Plain<string>()]));
        AssertDisposeRecyclesArrays(() => new Cross.Join<int, string, float>(archetype,
            [Plain<int>(), Plain<string>(), Plain<float>()]));
        AssertDisposeRecyclesArrays(() => new Cross.Join<int, string, float, double>(archetype,
            [Plain<int>(), Plain<string>(), Plain<float>(), Plain<double>()]));
        AssertDisposeRecyclesArrays(() => new Cross.Join<int, string, float, double, byte>(archetype,
            [Plain<int>(), Plain<string>(), Plain<float>(), Plain<double>(), Plain<byte>()]));
    }
}