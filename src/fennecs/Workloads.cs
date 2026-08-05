using System.Runtime.ExceptionServices;

namespace fennecs;

/// <summary>
/// A pooled unit of parallel work. Guarantees the countdown is signaled even when the
/// workload's delegates throw; the caller collects <see cref="Exception"/> after waiting.
/// </summary>
internal abstract class Workload : IThreadPoolWorkItem
{
    public CountdownEvent CountDown = null!;
    public Exception? Exception;

    public void Execute()
    {
        try
        {
            Run();
        }
        catch (Exception exception)
        {
            Exception = exception;
        }
        finally
        {
            CountDown.Signal();
        }
    }

    protected abstract void Run();

    protected static void RethrowSegmentFaults(List<Exception>? faults)
    {
        if (faults is null) return;
        if (faults.Count == 1) ExceptionDispatchInfo.Capture(faults[0]).Throw();
        throw new AggregateException(faults);
    }
}

internal static class Workloads
{
    /// <summary>
    /// Collects and clears any Exceptions the workloads captured. (call before returning them to their pool)
    /// </summary>
    public static void CollectFaults<W>(ref List<Exception>? faults, pools.PooledList<W> jobs) where W : Workload
    {
        foreach (var job in jobs)
        {
            if (job.Exception is null) continue;
            if (job.Exception is AggregateException aggregate)
            {
                (faults ??= []).AddRange(aggregate.Flatten().InnerExceptions);
            }
            else
            {
                (faults ??= []).Add(job.Exception);
            }
            job.Exception = null;
        }
    }

    /// <summary>
    /// Throws an <see cref="AggregateException"/> if any workloads faulted.
    /// </summary>
    public static void Rethrow(List<Exception>? faults)
    {
        if (faults is not null) throw new AggregateException(faults);
    }
}

internal class Work<C1> : Workload
{
    public Memory<C1> Memory1 = null!;
    public pools.PooledList<Memory<C1>>? Segments;
    public ComponentAction<C1> Action = null!;

    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var memory in segments)
            {
                try
                {
                    foreach (ref var component in memory.Span) Action(ref component);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        foreach (ref var c in Memory1.Span)
        {
            Action(ref c);
        }
    }
}

internal class UniformWork<U, C1> : Workload
{
    public Memory<C1> Memory1 = null!;
    public pools.PooledList<Memory<C1>>? Segments;
    public UniformComponentAction<U, C1> Action = null!;
    public U Uniform = default!;

    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var memory in segments)
            {
                try
                {
                    foreach (ref var component in memory.Span) Action(Uniform, ref component);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        foreach (ref var c in Memory1.Span)
        {
            Action(Uniform, ref c);
        }
    }
}

internal class DualWork<C1> : Workload
{
    public Memory<C1> Memory1 = null!;
    public FilterDelegate<C1> Pass = null!;
    public ComponentAction<C1> Included = null!;
    public ComponentAction<C1>? Excluded;

    protected override void Run()
    {
        var excluded = Excluded;
        if (excluded is null)
        {
            foreach (ref var c in Memory1.Span)
            {
                if (Pass(in c)) Included(ref c);
            }
            return;
        }

        foreach (ref var c in Memory1.Span)
        {
            if (Pass(in c)) Included(ref c);
            else excluded(ref c);
        }
    }
}

internal class UniformDualWork<U, C1> : Workload
{
    public Memory<C1> Memory1 = null!;
    public FilterDelegate<C1> Pass = null!;
    public UniformComponentAction<U, C1> Included = null!;
    public UniformComponentAction<U, C1>? Excluded;
    public U Uniform = default!;

    protected override void Run()
    {
        var excluded = Excluded;
        if (excluded is null)
        {
            foreach (ref var c in Memory1.Span)
            {
                if (Pass(in c)) Included(Uniform, ref c);
            }
            return;
        }

        foreach (ref var c in Memory1.Span)
        {
            if (Pass(in c)) Included(Uniform, ref c);
            else excluded(Uniform, ref c);
        }
    }
}

internal class Work<C1, C2> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public pools.PooledList<(Memory<C1>, Memory<C2>)>? Segments;
    public ComponentAction<C1, C2> Action = null!;

    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var (memory1, memory2) in segments)
            {
                try
                {
                    var segment1 = memory1.Span;
                    var segment2 = memory2.Span;
                    for (var i = 0; i < segment1.Length; i++) Action(ref segment1[i], ref segment2[i]);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        for (var i = 0; i < Memory1.Length; i++)
        {
            Action(ref s1[i], ref s2[i]);
        }
    }
}

internal class UniformWork<U, C1, C2> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public pools.PooledList<(Memory<C1>, Memory<C2>)>? Segments;

    public UniformComponentAction<U, C1, C2> Action = null!;
    public U Uniform = default!;


    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var (memory1, memory2) in segments)
            {
                try
                {
                    var segment1 = memory1.Span;
                    var segment2 = memory2.Span;
                    for (var i = 0; i < segment1.Length; i++) Action(Uniform, ref segment1[i], ref segment2[i]);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        var s1 = Memory1.Span;
        var s2 = Memory2.Span;

        for (var i = 0; i < Memory1.Length; i++)
        {
            Action(Uniform, ref s1[i], ref s2[i]);
        }
    }
}

internal class DualWork<C1, C2> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public FilterDelegate<C1, C2> Pass = null!;
    public ComponentAction<C1, C2> Included = null!;
    public ComponentAction<C1, C2>? Excluded;

    protected override void Run()
    {
        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var excluded = Excluded;
        if (excluded is null)
        {
            for (var i = 0; i < Memory1.Length; i++)
            {
                if (Pass(in s1[i], in s2[i])) Included(ref s1[i], ref s2[i]);
            }
            return;
        }

        for (var i = 0; i < Memory1.Length; i++)
        {
            if (Pass(in s1[i], in s2[i])) Included(ref s1[i], ref s2[i]);
            else excluded(ref s1[i], ref s2[i]);
        }
    }
}

internal class UniformDualWork<U, C1, C2> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public FilterDelegate<C1, C2> Pass = null!;
    public UniformComponentAction<U, C1, C2> Included = null!;
    public UniformComponentAction<U, C1, C2>? Excluded;
    public U Uniform = default!;

    protected override void Run()
    {
        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var excluded = Excluded;
        if (excluded is null)
        {
            for (var i = 0; i < Memory1.Length; i++)
            {
                if (Pass(in s1[i], in s2[i])) Included(Uniform, ref s1[i], ref s2[i]);
            }
            return;
        }

        for (var i = 0; i < Memory1.Length; i++)
        {
            if (Pass(in s1[i], in s2[i])) Included(Uniform, ref s1[i], ref s2[i]);
            else excluded(Uniform, ref s1[i], ref s2[i]);
        }
    }
}

internal class Work<C1, C2, C3> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public pools.PooledList<(Memory<C1>, Memory<C2>, Memory<C3>)>? Segments;

    public ComponentAction<C1, C2, C3> Action = null!;


    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var (memory1, memory2, memory3) in segments)
            {
                try
                {
                    var segment1 = memory1.Span;
                    var segment2 = memory2.Span;
                    var segment3 = memory3.Span;
                    for (var i = 0; i < segment1.Length; i++)
                        Action(ref segment1[i], ref segment2[i], ref segment3[i]);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;

        for (var i = 0; i < Memory1.Length; i++)
        {
            Action(ref s1[i], ref s2[i], ref s3[i]);
        }
    }
}

internal class UniformWork<U, C1, C2, C3> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public pools.PooledList<(Memory<C1>, Memory<C2>, Memory<C3>)>? Segments;

    public UniformComponentAction<U, C1, C2, C3> Action = null!;
    public U Uniform = default!;


    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var (memory1, memory2, memory3) in segments)
            {
                try
                {
                    var segment1 = memory1.Span;
                    var segment2 = memory2.Span;
                    var segment3 = memory3.Span;
                    for (var i = 0; i < segment1.Length; i++)
                        Action(Uniform, ref segment1[i], ref segment2[i], ref segment3[i]);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;

        for (var i = 0; i < Memory1.Length; i++)
        {
            Action(Uniform, ref s1[i], ref s2[i], ref s3[i]);
        }
    }
}

internal class DualWork<C1, C2, C3> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public FilterDelegate<C1, C2, C3> Pass = null!;
    public ComponentAction<C1, C2, C3> Included = null!;
    public ComponentAction<C1, C2, C3>? Excluded;

    protected override void Run()
    {
        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var excluded = Excluded;
        if (excluded is null)
        {
            for (var i = 0; i < Memory1.Length; i++)
            {
                if (Pass(in s1[i], in s2[i], in s3[i])) Included(ref s1[i], ref s2[i], ref s3[i]);
            }
            return;
        }

        for (var i = 0; i < Memory1.Length; i++)
        {
            if (Pass(in s1[i], in s2[i], in s3[i])) Included(ref s1[i], ref s2[i], ref s3[i]);
            else excluded(ref s1[i], ref s2[i], ref s3[i]);
        }
    }
}

internal class UniformDualWork<U, C1, C2, C3> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public FilterDelegate<C1, C2, C3> Pass = null!;
    public UniformComponentAction<U, C1, C2, C3> Included = null!;
    public UniformComponentAction<U, C1, C2, C3>? Excluded;
    public U Uniform = default!;

    protected override void Run()
    {
        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var excluded = Excluded;
        if (excluded is null)
        {
            for (var i = 0; i < Memory1.Length; i++)
            {
                if (Pass(in s1[i], in s2[i], in s3[i])) Included(Uniform, ref s1[i], ref s2[i], ref s3[i]);
            }
            return;
        }

        for (var i = 0; i < Memory1.Length; i++)
        {
            if (Pass(in s1[i], in s2[i], in s3[i])) Included(Uniform, ref s1[i], ref s2[i], ref s3[i]);
            else excluded(Uniform, ref s1[i], ref s2[i], ref s3[i]);
        }
    }
}

internal class Work<C1, C2, C3, C4> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public Memory<C4> Memory4 = null!;
    public pools.PooledList<(Memory<C1>, Memory<C2>, Memory<C3>, Memory<C4>)>? Segments;

    public ComponentAction<C1, C2, C3, C4> Action = null!;


    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var (memory1, memory2, memory3, memory4) in segments)
            {
                try
                {
                    var segment1 = memory1.Span;
                    var segment2 = memory2.Span;
                    var segment3 = memory3.Span;
                    var segment4 = memory4.Span;
                    for (var i = 0; i < segment1.Length; i++)
                        Action(ref segment1[i], ref segment2[i], ref segment3[i], ref segment4[i]);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var s4 = Memory4.Span;

        for (var i = 0; i < Memory1.Length; i++)
        {
            Action(ref s1[i], ref s2[i], ref s3[i], ref s4[i]);
        }
    }
}

internal class UniformWork<U, C1, C2, C3, C4> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public Memory<C4> Memory4 = null!;
    public pools.PooledList<(Memory<C1>, Memory<C2>, Memory<C3>, Memory<C4>)>? Segments;

    public UniformComponentAction<U, C1, C2, C3, C4> Action = null!;
    public U Uniform = default!;


    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var (memory1, memory2, memory3, memory4) in segments)
            {
                try
                {
                    var segment1 = memory1.Span;
                    var segment2 = memory2.Span;
                    var segment3 = memory3.Span;
                    var segment4 = memory4.Span;
                    for (var i = 0; i < segment1.Length; i++)
                        Action(Uniform, ref segment1[i], ref segment2[i], ref segment3[i], ref segment4[i]);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var s4 = Memory4.Span;
        for (var i = 0; i < Memory1.Length; i++)
        {
            Action(Uniform, ref s1[i], ref s2[i], ref s3[i], ref s4[i]);
        }
    }
}

internal class DualWork<C1, C2, C3, C4> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public Memory<C4> Memory4 = null!;
    public FilterDelegate<C1, C2, C3, C4> Pass = null!;
    public ComponentAction<C1, C2, C3, C4> Included = null!;
    public ComponentAction<C1, C2, C3, C4>? Excluded;

    protected override void Run()
    {
        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var s4 = Memory4.Span;
        var excluded = Excluded;
        if (excluded is null)
        {
            for (var i = 0; i < Memory1.Length; i++)
            {
                if (Pass(in s1[i], in s2[i], in s3[i], in s4[i])) Included(ref s1[i], ref s2[i], ref s3[i], ref s4[i]);
            }
            return;
        }

        for (var i = 0; i < Memory1.Length; i++)
        {
            if (Pass(in s1[i], in s2[i], in s3[i], in s4[i])) Included(ref s1[i], ref s2[i], ref s3[i], ref s4[i]);
            else excluded(ref s1[i], ref s2[i], ref s3[i], ref s4[i]);
        }
    }
}

internal class UniformDualWork<U, C1, C2, C3, C4> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public Memory<C4> Memory4 = null!;
    public FilterDelegate<C1, C2, C3, C4> Pass = null!;
    public UniformComponentAction<U, C1, C2, C3, C4> Included = null!;
    public UniformComponentAction<U, C1, C2, C3, C4>? Excluded;
    public U Uniform = default!;

    protected override void Run()
    {
        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var s4 = Memory4.Span;
        var excluded = Excluded;
        if (excluded is null)
        {
            for (var i = 0; i < Memory1.Length; i++)
            {
                if (Pass(in s1[i], in s2[i], in s3[i], in s4[i])) Included(Uniform, ref s1[i], ref s2[i], ref s3[i], ref s4[i]);
            }
            return;
        }

        for (var i = 0; i < Memory1.Length; i++)
        {
            if (Pass(in s1[i], in s2[i], in s3[i], in s4[i])) Included(Uniform, ref s1[i], ref s2[i], ref s3[i], ref s4[i]);
            else excluded(Uniform, ref s1[i], ref s2[i], ref s3[i], ref s4[i]);
        }
    }
}

internal class Work<C1, C2, C3, C4, C5> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public Memory<C4> Memory4 = null!;
    public Memory<C5> Memory5 = null!;
    public pools.PooledList<(Memory<C1>, Memory<C2>, Memory<C3>, Memory<C4>, Memory<C5>)>? Segments;

    public ComponentAction<C1, C2, C3, C4, C5> Action = null!;


    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var (memory1, memory2, memory3, memory4, memory5) in segments)
            {
                try
                {
                    var segment1 = memory1.Span;
                    var segment2 = memory2.Span;
                    var segment3 = memory3.Span;
                    var segment4 = memory4.Span;
                    var segment5 = memory5.Span;
                    for (var i = 0; i < segment1.Length; i++)
                        Action(ref segment1[i], ref segment2[i], ref segment3[i], ref segment4[i], ref segment5[i]);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var s4 = Memory4.Span;
        var s5 = Memory5.Span;

        for (var i = 0; i < Memory1.Length; i++)
        {
            Action(ref s1[i], ref s2[i], ref s3[i], ref s4[i], ref s5[i]);
        }
    }
}

internal class UniformWork<U, C1, C2, C3, C4, C5> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public Memory<C4> Memory4 = null!;
    public Memory<C5> Memory5 = null!;
    public pools.PooledList<(Memory<C1>, Memory<C2>, Memory<C3>, Memory<C4>, Memory<C5>)>? Segments;

    public UniformComponentAction<U, C1, C2, C3, C4, C5> Action = null!;
    public U Uniform = default!;


    protected override void Run()
    {
        if (Segments is { } segments)
        {
            List<Exception>? faults = null;
            foreach (var (memory1, memory2, memory3, memory4, memory5) in segments)
            {
                try
                {
                    var segment1 = memory1.Span;
                    var segment2 = memory2.Span;
                    var segment3 = memory3.Span;
                    var segment4 = memory4.Span;
                    var segment5 = memory5.Span;
                    for (var i = 0; i < segment1.Length; i++)
                        Action(Uniform, ref segment1[i], ref segment2[i], ref segment3[i], ref segment4[i],
                            ref segment5[i]);
                }
                catch (Exception exception)
                {
                    (faults ??= []).Add(exception);
                }
            }
            RethrowSegmentFaults(faults);
            return;
        }

        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var s4 = Memory4.Span;
        var s5 = Memory5.Span;

        for (var i = 0; i < Memory1.Length; i++)
        {
            Action(Uniform, ref s1[i], ref s2[i], ref s3[i], ref s4[i], ref s5[i]);
        }
    }
}

internal class DualWork<C1, C2, C3, C4, C5> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public Memory<C4> Memory4 = null!;
    public Memory<C5> Memory5 = null!;
    public FilterDelegate<C1, C2, C3, C4, C5> Pass = null!;
    public ComponentAction<C1, C2, C3, C4, C5> Included = null!;
    public ComponentAction<C1, C2, C3, C4, C5>? Excluded;

    protected override void Run()
    {
        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var s4 = Memory4.Span;
        var s5 = Memory5.Span;
        var excluded = Excluded;
        if (excluded is null)
        {
            for (var i = 0; i < Memory1.Length; i++)
            {
                if (Pass(in s1[i], in s2[i], in s3[i], in s4[i], in s5[i])) Included(ref s1[i], ref s2[i], ref s3[i], ref s4[i], ref s5[i]);
            }
            return;
        }

        for (var i = 0; i < Memory1.Length; i++)
        {
            if (Pass(in s1[i], in s2[i], in s3[i], in s4[i], in s5[i])) Included(ref s1[i], ref s2[i], ref s3[i], ref s4[i], ref s5[i]);
            else excluded(ref s1[i], ref s2[i], ref s3[i], ref s4[i], ref s5[i]);
        }
    }
}

internal class UniformDualWork<U, C1, C2, C3, C4, C5> : Workload
{
    public Memory<C1> Memory1 = null!;
    public Memory<C2> Memory2 = null!;
    public Memory<C3> Memory3 = null!;
    public Memory<C4> Memory4 = null!;
    public Memory<C5> Memory5 = null!;
    public FilterDelegate<C1, C2, C3, C4, C5> Pass = null!;
    public UniformComponentAction<U, C1, C2, C3, C4, C5> Included = null!;
    public UniformComponentAction<U, C1, C2, C3, C4, C5>? Excluded;
    public U Uniform = default!;

    protected override void Run()
    {
        var s1 = Memory1.Span;
        var s2 = Memory2.Span;
        var s3 = Memory3.Span;
        var s4 = Memory4.Span;
        var s5 = Memory5.Span;
        var excluded = Excluded;
        if (excluded is null)
        {
            for (var i = 0; i < Memory1.Length; i++)
            {
                if (Pass(in s1[i], in s2[i], in s3[i], in s4[i], in s5[i])) Included(Uniform, ref s1[i], ref s2[i], ref s3[i], ref s4[i], ref s5[i]);
            }
            return;
        }

        for (var i = 0; i < Memory1.Length; i++)
        {
            if (Pass(in s1[i], in s2[i], in s3[i], in s4[i], in s5[i])) Included(Uniform, ref s1[i], ref s2[i], ref s3[i], ref s4[i], ref s5[i]);
            else excluded(Uniform, ref s1[i], ref s2[i], ref s3[i], ref s4[i], ref s5[i]);
        }
    }
}
