using fennecs.pools;

namespace fennecs.tests;

// Round-trip behavior of the object pools: Return/Dispose must actually hand instances
// back to their pool, and clear them where the pool contract says so.
public class PoolTests
{
    // Test-only job types: JobPool<T> is a static pool per T, so these are uncontended
    // and same-thread ConcurrentBag round-trips are deterministic.
    private class SoloJob;
    private class BatchJob;
    private struct ListProbe;


    [Fact]
    public void JobPool_Return_Puts_Job_Back_In_Pool()
    {
        var job = JobPool<SoloJob>.Rent();

        JobPool<SoloJob>.Return(job);

        Assert.Same(job, JobPool<SoloJob>.Rent());
    }


    [Fact]
    public void JobPool_Return_List_Recycles_All_Jobs_And_Clears_List()
    {
        var jobs = new List<BatchJob> { JobPool<BatchJob>.Rent(), JobPool<BatchJob>.Rent(), JobPool<BatchJob>.Rent() };
        var returned = new HashSet<BatchJob>(jobs);

        JobPool<BatchJob>.Return(jobs);

        Assert.Empty(jobs);
        Assert.Contains(JobPool<BatchJob>.Rent(), returned);
        Assert.Contains(JobPool<BatchJob>.Rent(), returned);
        Assert.Contains(JobPool<BatchJob>.Rent(), returned);
    }


    [Fact]
    public void PooledList_Dispose_Returns_Cleared_List_To_Pool()
    {
        var list = PooledList<ListProbe>.Rent();
        list.Add(new ListProbe());

        list.Dispose();

        var again = PooledList<ListProbe>.Rent();
        Assert.Same(list, again);
        Assert.Empty(again);
    }


    // MaskPool is a single shared pool; a parallel test's thread can steal our returned mask
    // between Return and Rent, so retry until a same-thread round-trip is observed.
    // A Return/Dispose that never pools the mask can never pass any attempt.
    private static void AssertMaskRoundTrip(Action<Mask> giveBack)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var mask = MaskPool.Rent();
            mask.Has(TypeExpression.Of<int>(Match.Plain));

            giveBack(mask);

            var again = MaskPool.Rent();
            var sameInstance = ReferenceEquals(mask, again);
            var clean = again.HasTypes.Count == 0 && again.NotTypes.Count == 0 && again.AnyTypes.Count == 0;
            again.Dispose();

            if (sameInstance)
            {
                Assert.True(clean, "Mask came back from the pool without being cleared.");
                return;
            }
        }

        Assert.Fail("Mask was never returned to the MaskPool.");
    }


    [Fact]
    public void MaskPool_Return_Puts_Cleared_Mask_Back_In_Pool()
    {
        AssertMaskRoundTrip(MaskPool.Return);
    }


    [Fact]
    public void Mask_Dispose_Returns_Mask_To_Pool()
    {
        AssertMaskRoundTrip(mask => mask.Dispose());
    }
}
