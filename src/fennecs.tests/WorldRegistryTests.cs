// SPDX-License-Identifier: MIT

using fennecs.pools;

namespace fennecs.tests;

public class WorldRegistryTests
{
    [Fact]
    public void World_Tags_are_Recycled_on_Dispose()
    {
        // Far more Worlds than the 255 concurrent slots — only possible if Dispose recycles tags.
        for (var i = 0; i < 300; i++)
        {
            using var world = new World(0);
            Assert.NotEqual(0, world.Tag);
            var entity = world.Spawn();
            Assert.True(world.IsAlive(entity));
        }
    }


    [Fact]
    public void Entities_of_Disposed_World_are_not_Alive()
    {
        var world = new World(0);
        var entity = world.Spawn();
        Assert.True(entity.Alive);

        world.Dispose();
        Assert.False(entity.Alive);
    }


    [Fact]
    public void CRUD_on_Entity_of_Disposed_World_Throws()
    {
        var world = new World(0);
        var entity = world.Spawn();
        world.Dispose();

        Assert.Throws<InvalidOperationException>(() => entity.Add(123));
    }


    [Fact]
    public void Double_Dispose_is_Safe()
    {
        var world = new World(0);
        world.Dispose();
        world.Dispose();
    }


    [Fact]
    public void EntityPool_Retires_Index_on_Generation_Wrap()
    {
        var pool = new EntityPool(1, 0);

        // Exhaust the full generation space of a single index.
        var entity = pool.Spawn();
        var index = entity.Index;
        for (var gen = 1; gen < ushort.MaxValue; gen++)
        {
            Assert.Equal(index, entity.Index); // index gets recycled...
            pool.Recycle(entity);
            entity = pool.Spawn();
        }

        // ...until its generation wraps: then the index is retired and a fresh one minted.
        pool.Recycle(entity);
        var successor = pool.Spawn();
        Assert.NotEqual(index, successor.Index);

        // Retired indices don't leak into the live count.
        pool.Recycle(successor);
        Assert.Equal(0, pool.Count);
    }


    [Fact]
    public void Relation_does_not_Resurrect_on_Index_Reuse()
    {
        using var world = new World(0);

        var target = world.Spawn();
        var origin = world.Spawn().Add("payload", target);
        Assert.True(origin.Has<string>(target));

        // Despawning the target eagerly cleans up relations targeting it.
        world.Despawn(target);
        Assert.False(origin.Has<string>(Match.Target));

        // A new Entity reusing the same index must not inherit the old relation.
        var successor = world.Spawn();
        Assert.Equal(target.Index, successor.Index);
        Assert.False(origin.Has<string>(successor));
    }


    [Fact]
    public void Stale_Handle_Reports_Already_Despawned()
    {
        using var world = new World(0);
        var entity = world.Spawn();
        world.Despawn(entity);

        var ex = Assert.Throws<ObjectDisposedException>(() => entity.Add(123));
        Assert.Contains("already despawned", ex.Message);
        Assert.DoesNotContain("new generation", ex.Message);
    }


    [Fact]
    public void Stale_Handle_Reports_Respawned_Index()
    {
        using var world = new World(0);
        var entity = world.Spawn();
        world.Despawn(entity);

        var successor = world.Spawn();
        Assert.Equal(entity.Index, successor.Index);

        var ex = Assert.Throws<ObjectDisposedException>(() => entity.Add(123));
        Assert.Contains("already despawned", ex.Message);
        Assert.Contains("new generation", ex.Message);
        Assert.Contains(successor.ToString(), ex.Message);
    }


    [Fact]
    public void Diagnostics_Describe_Dead_Handles()
    {
        using var world1 = new World(0);
        using var world2 = new World(0);

        Assert.Contains("default(Entity)", world1.DescribeDead(default));

        var foreign = world2.Spawn();
        Assert.Contains("another World", world1.DescribeDead(foreign));

        var neverSpawned = new Entity(world1.Tag, 9001, 1);
        Assert.Contains("never spawned", world1.DescribeDead(neverSpawned));

        var real = world1.Spawn();
        var forged = new Entity(world1.Tag, real.Index, (ushort) (real.Generation + 1));
        Assert.Contains("does not exist yet", world1.DescribeDead(forged));

        // Fall-through: diagnosing a handle that is, in fact, alive.
        Assert.Contains("is not alive", world1.DescribeDead(real));
    }


    [Fact]
    public void Worlds_have_Distinct_HashCodes()
    {
        using var world1 = new World(0);
        using var world2 = new World(0);

        Assert.Equal(world1.GetHashCode(), world1.GetHashCode());
        Assert.NotEqual(world1.GetHashCode(), world2.GetHashCode());
    }


    [Fact]
    public void Deferred_Double_Despawn_Throws_on_CatchUp()
    {
        using var world = new World(0);
        var entity = world.Spawn();

        var worldLock = world.Lock();
        world.Despawn(entity);
        world.Despawn(entity); // both deferred; the second one is stale at catch-up time.

        Assert.Throws<ObjectDisposedException>(() => worldLock.Dispose());
    }


}


// Observes the shared tag free-list, which every World construction/disposal in the process
// mutates — a parallel test could claim the freshly-released tag between Dispose and the assert.
[Collection(nameof(SharedPoolTests))]
public class WorldRegistryIsolatedTests
{
    [Fact]
    public void Double_Dispose_Releases_Tag_Exactly_Once()
    {
        var world = new World(0);
        var tag = world.Tag;

        world.Dispose();
        world.Dispose(); // must be a no-op

        Assert.Equal(1, World.FreeTagCount(tag)); // the World's tag was recycled exactly once
        Assert.Equal(0, World.FreeTagCount(0));   // the reserved tag never enters the free list
    }
}
