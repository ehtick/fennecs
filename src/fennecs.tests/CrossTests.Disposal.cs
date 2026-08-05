using System.Collections;

namespace fennecs.tests;

// Tests in this collection observe process-wide shared pools (PooledList, Cross's ArrayPool,
// the World tag registry) and must not run concurrently with anything that rents from them.
[CollectionDefinition(nameof(SharedPoolTests), DisableParallelization = true)]
public class SharedPoolTests;


[Collection(nameof(SharedPoolTests))]
public class CrossDisposalTests
{
    private static void AssertDirectDispose<TJoin>(TJoin join,
        (int[] Counter, int[] Limiter, IEnumerable[] Storages) state) where TJoin : IDisposable
    {
        Assert.Null(state.Counter);
        Assert.Null(state.Limiter);
        Assert.All(state.Storages, storage => Assert.Null(storage));
        join.Dispose();
    }


    [Fact]
    public void Direct_Join_Dispose_Has_No_Pooled_State()
    {
        using var world = CrossTests.Setup(out var archetype);

        var join1 = new Cross.Join<int>(archetype, [CrossTests.Plain<int>()]);
        AssertDirectDispose(join1, join1.TestState);

        var join2 = new Cross.Join<int, string>(archetype, [CrossTests.Plain<int>(), CrossTests.Plain<string>()]);
        AssertDirectDispose(join2, join2.TestState);

        var join3 = new Cross.Join<int, string, float>(archetype,
            [CrossTests.Plain<int>(), CrossTests.Plain<string>(), CrossTests.Plain<float>()]);
        AssertDirectDispose(join3, join3.TestState);

        var join4 = new Cross.Join<int, string, float, double>(archetype,
            [CrossTests.Plain<int>(), CrossTests.Plain<string>(), CrossTests.Plain<float>(), CrossTests.Plain<double>()]);
        AssertDirectDispose(join4, join4.TestState);

        var join5 = new Cross.Join<int, string, float, double, byte>(archetype,
            [CrossTests.Plain<int>(), CrossTests.Plain<string>(), CrossTests.Plain<float>(), CrossTests.Plain<double>(),
                CrossTests.Plain<byte>()]);
        AssertDirectDispose(join5, join5.TestState);
    }


    // Dispose must return the matched storage lists to their pool (which clears them).
    private static void AssertDisposeClearsStorages<TJoin>(TJoin join, IEnumerable[] lists) where TJoin : IDisposable
    {
        foreach (var list in lists) Assert.NotEmpty(list);

        join.Dispose();
        foreach (var list in lists) Assert.Empty(list);
    }


    [Fact]
    public void Join_Dispose_Returns_Storage_Lists()
    {
        using var world = CrossTests.Setup(out var archetype);

        var join1 = new Cross.Join<int>(archetype,
            [CrossTests.Any<int>()]);
        AssertDisposeClearsStorages(join1, join1.TestState.Storages);

        var join2 = new Cross.Join<int, string>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>()]);
        AssertDisposeClearsStorages(join2, join2.TestState.Storages);

        var join3 = new Cross.Join<int, string, float>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>()]);
        AssertDisposeClearsStorages(join3, join3.TestState.Storages);

        var join4 = new Cross.Join<int, string, float, double>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>(), CrossTests.Any<double>()]);
        AssertDisposeClearsStorages(join4, join4.TestState.Storages);

        var join5 = new Cross.Join<int, string, float, double, byte>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>(), CrossTests.Any<double>(), CrossTests.Any<byte>()]);
        AssertDisposeClearsStorages(join5, join5.TestState.Storages);
    }


    // Dispose must return the counter/limiter arrays to Cross's ArrayPool: with no concurrent
    // renters, a fresh join of the same arity is handed exactly the recycled arrays.
    private static void AssertArraysRecycled((int[] Counter, int[] Limiter, IEnumerable[] Storages) first,
        (int[] Counter, int[] Limiter, IEnumerable[] Storages) second)
    {
        var firstArrays = new[] { first.Counter, first.Limiter };
        Assert.Contains(second.Counter, firstArrays);
        Assert.Contains(second.Limiter, firstArrays);
    }


    [Fact]
    public void Join_Dispose_Recycles_Counter_Arrays()
    {
        using var world = CrossTests.Setup(out var archetype);

        var first1 = new Cross.Join<int>(archetype, [CrossTests.Any<int>()]);
        var state1 = first1.TestState;
        first1.Dispose();
        var second1 = new Cross.Join<int>(archetype, [CrossTests.Any<int>()]);
        AssertArraysRecycled(state1, second1.TestState);
        second1.Dispose();

        var first2 = new Cross.Join<int, string>(archetype, [CrossTests.Any<int>(), CrossTests.Any<string>()]);
        var state2 = first2.TestState;
        first2.Dispose();
        var second2 = new Cross.Join<int, string>(archetype, [CrossTests.Any<int>(), CrossTests.Any<string>()]);
        AssertArraysRecycled(state2, second2.TestState);
        second2.Dispose();

        var first3 = new Cross.Join<int, string, float>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>()]);
        var state3 = first3.TestState;
        first3.Dispose();
        var second3 = new Cross.Join<int, string, float>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>()]);
        AssertArraysRecycled(state3, second3.TestState);
        second3.Dispose();

        var first4 = new Cross.Join<int, string, float, double>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>(), CrossTests.Any<double>()]);
        var state4 = first4.TestState;
        first4.Dispose();
        var second4 = new Cross.Join<int, string, float, double>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>(), CrossTests.Any<double>()]);
        AssertArraysRecycled(state4, second4.TestState);
        second4.Dispose();

        var first5 = new Cross.Join<int, string, float, double, byte>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>(), CrossTests.Any<double>(), CrossTests.Any<byte>()]);
        var state5 = first5.TestState;
        first5.Dispose();
        var second5 = new Cross.Join<int, string, float, double, byte>(archetype,
            [CrossTests.Any<int>(), CrossTests.Any<string>(), CrossTests.Any<float>(), CrossTests.Any<double>(), CrossTests.Any<byte>()]);
        AssertArraysRecycled(state5, second5.TestState);
        second5.Dispose();
    }
}
