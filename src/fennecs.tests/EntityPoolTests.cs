using fennecs.pools;

namespace fennecs.tests;

public class EntityPoolTests
{
    [Fact]
    public void Prefills_Recycled_Indices()
    {
        var pool = new EntityPool(1, 8);

        Assert.Equal(8, pool.Created);
        Assert.Equal(0, pool.Count);
    }


    [Fact]
    public void Bulk_Spawn_Reuses_Recycled_Indices_First()
    {
        var pool = new EntityPool(1, 4);

        Span<Entity> destination = stackalloc Entity[2];
        pool.Spawn(destination);

        Assert.Equal(4, pool.Created); // no fresh indices were minted
        foreach (var entity in destination) Assert.InRange(entity.Index, 1u, 4u);
    }


    [Fact]
    public void Bulk_Spawn_Beyond_Capacity_Mints_Generation_One()
    {
        var pool = new EntityPool(1, 4);

        var destination = new Entity[40]; // forces the generations array to grow
        pool.Spawn(destination);

        Assert.Equal(40, pool.Created);
        Assert.All(destination, entity => Assert.Equal(1, entity.Generation));
    }
}
