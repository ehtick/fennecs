namespace fennecs.tests;

public class RelateTests(ITestOutputHelper output)
{
    [Fact]
    public void Relate_has_ToString()
    {
        using var world = new World();
        var entity = world.Spawn();

        var target = Relate.To(entity);
        output.WriteLine(target.ToString());
        Assert.Equal(entity.Key.ToString(), target.ToString());
    }

    [Fact]
    public void Default_Relate_Converts_To_Plain_Match()
    {
        using var world = new World();
        var entity = world.Spawn();

        Match plain = default(Relate);
        Assert.Equal(Match.Plain, plain);

        Match related = Relate.To(entity);
        Assert.NotEqual(Match.Plain, related);
    }
}
