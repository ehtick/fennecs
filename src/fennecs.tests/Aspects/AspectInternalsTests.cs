namespace fennecs.tests.Aspects;

// Bookkeeping details of Aspect membership, capacity, and archetype lifecycle.
public class AspectInternalsTests
{
    private record struct Hot(int Value);


    [Fact]
    public void Count_Sums_All_Archetypes()
    {
        using var world = new World();
        world.Spawn().Add(1);
        world.Spawn().Add(2);
        world.Spawn().Add(3).Add("tag");
        world.Spawn().Add(4).Add("tag");
        world.Spawn().Add(5).Add("tag");

        Assert.Equal(5, world.Main.Count);
    }


    [Fact]
    public void EnsureCapacity_Never_Shrinks_Meta()
    {
        using var world = new World();
        var entity = world.Spawn().Add(42);

        world.Main.EnsureCapacity(1);

        Assert.Equal(42, entity.Ref<int>());
    }


    [Fact]
    public void GC_Disposes_Empty_Archetypes_And_Allows_Recreation()
    {
        using var world = new World();
        var entity = world.Spawn().Add(1).Add("tag");
        entity.Remove<string>(); // (int, string) archetype is now empty

        var before = world.Main.ArchetypeCount;
        world.GC();
        Assert.True(world.Main.ArchetypeCount < before);

        entity.Add("again"); // recreates the (int, string) archetype
        var query = world.Query<int>().Has<string>().Compile();
        Assert.Single(query);
        Assert.Equal("again", entity.Ref<string>());
    }


    [Fact]
    public void Lazy_Aspect_Join_Grows_Meta_For_High_Index_Entities()
    {
        using var world = new World(0);
        world.AddAspect("hot").Owns<Hot>();

        // Index 128 sits exactly on a power-of-two boundary of the Aspect's meta table.
        Entity last = default;
        for (var i = 0; i < 128; i++) last = world.Spawn();

        last.Add(new Hot(7));
        Assert.Equal(7, last.Ref<Hot>().Value);
    }


    [Fact]
    public void Aspect_Handles_Entity_Index_At_Meta_Boundary()
    {
        using var world = new World(0);
        world.AddAspect("hot").Owns<Hot>(); // meta table length is exactly 1
        var entity = world.Spawn();         // index 1 == meta length

        Assert.False(entity.Has<Hot>());
        Assert.Throws<InvalidOperationException>(() => entity.Ref<Hot>());
        entity.Despawn(); // membership boundary check must not read out of bounds
    }


    [Fact]
    public void AddAspect_Rejects_Blank_Names()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() => world.AddAspect(null!));
        Assert.Throws<ArgumentException>(() => world.AddAspect(""));
        Assert.Throws<ArgumentException>(() => world.AddAspect("   "));
    }


    [Fact]
    public void Remove_Allow_Missing_Component_Is_NoOp()
    {
        using var world = new World();
        var entity = world.Spawn().Add(7).Add("keep");

        entity.Remove<double>(Match.Any, RemoveConflict.Allow); // wildcard, nothing matches
        entity.Remove<double>(default, RemoveConflict.Allow);   // plain, not present

        Assert.Equal(7, entity.Ref<int>());
        Assert.Equal("keep", entity.Ref<string>());
    }


    [Fact]
    public void Lazy_Join_Invalidates_Archetype_Enumerators()
    {
        using var world = new World();
        world.AddAspect("hot").Owns<Hot>();
        world.Spawn().Add(new Hot(1));

        var query = world.Query<Hot>().Compile();
        var table = query.Archetypes.Single();

        using var enumerator = table.GetEnumerator();
        Assert.True(enumerator.MoveNext());

        world.Spawn().Add(new Hot(2)); // joins the same archetype lazily

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }
}
