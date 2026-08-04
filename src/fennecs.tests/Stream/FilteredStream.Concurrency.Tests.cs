namespace fennecs.tests.Stream;

public class FilteredStreamConcurrencyTests
{
    // Regression: Streams used to share one CountdownEvent through with-expression copies,
    // corrupting each other when derived views ran Jobs concurrently.
    [Fact]
    public void Derived_FilteredStreams_Run_Jobs_Independently()
    {
        using var world = new World();
        for (var i = 0; i < 10_000; i++) world.Spawn().Add(i);

        var stream = world.Stream<int>();
        var low = stream.Where((in int v) => v < 5_000);
        var high = stream.Where((in int v) => v >= 5_000);

        var lowCount = 0;
        var highCount = 0;
        Parallel.Invoke(
            () => low.Job((ref int _) => Interlocked.Increment(ref lowCount)),
            () => high.Job((ref int _) => Interlocked.Increment(ref highCount)));

        Assert.Equal(5_000, lowCount);
        Assert.Equal(5_000, highCount);
    }
}
