namespace fennecs.tests.Stream;

// A worker delegate that throws must neither hang the caller (missed countdown signal)
// nor crash the process (unhandled thread pool exception) — it surfaces as AggregateException.
public class JobExceptionTests
{
    private static World Setup(out fennecs.Stream<int> stream)
    {
        var world = new World();
        for (var i = 0; i < 1000; i++) world.Spawn().Add(i);
        stream = world.Query<int>().Stream();
        return world;
    }

    // Runs the Job on a task with a bounded wait, so a regression of the missed-signal hang
    // fails this test cleanly instead of stalling the whole suite.
    private static void AssertJobFaults(Action jobCall)
    {
        var task = Task.Run(() => Assert.Throws<AggregateException>(jobCall));
        Assert.True(task.Wait(TimeSpan.FromSeconds(30)), "Job hung instead of propagating the worker exception.");
        Assert.All(task.Result.InnerExceptions, e => Assert.IsType<InvalidOperationException>(e));
    }


    [Fact]
    public void Job_Propagates_Worker_Exceptions()
    {
        using var world = Setup(out var stream);

        AssertJobFaults(() => stream.Job((ref int _) => throw new InvalidOperationException("boom")));

        world.Spawn().Add(1); // lock released, world still usable
    }


    [Fact]
    public void Uniform_Job_Propagates_Worker_Exceptions()
    {
        using var world = Setup(out var stream);

        AssertJobFaults(() => stream.Job(7, (int _, ref int _) => throw new InvalidOperationException("boom")));

        world.Spawn().Add(1);
    }


    [Fact]
    public void Job_Propagates_Worker_Exceptions_At_Higher_Arity()
    {
        using var world = new World();
        for (var i = 0; i < 1000; i++) world.Spawn().Add(i).Add((float)i);
        var stream = world.Query<int, float>().Stream();

        AssertJobFaults(() => stream.Job((ref int _, ref float _) => throw new InvalidOperationException("boom")));

        world.Spawn().Add(1);
    }


    [Fact]
    public void Filtered_Job_Propagates_Worker_Exceptions()
    {
        using var world = Setup(out var stream);
        world.Spawn().Add(9001).Add("covered");

        var filtered = stream.Has(Comp<string>.Plain);

        AssertJobFaults(() => filtered.Job((ref int _) => throw new InvalidOperationException("boom")));

        world.Spawn().Add(1);
    }


    [Fact]
    public void Filtered_DualDelegate_Job_Propagates_Excluded_Exceptions()
    {
        using var world = Setup(out var stream); // 1000 entities fail the filter -> excluded path
        world.Spawn().Add(9001).Add("covered");

        var filtered = stream.Has(Comp<string>.Plain);

        AssertJobFaults(() => filtered.Job(
            included: (ref int _) => { },
            excluded: (ref int _) => throw new InvalidOperationException("boom")));

        world.Spawn().Add(1);
    }
}
