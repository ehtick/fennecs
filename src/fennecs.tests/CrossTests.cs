using System.Collections;

namespace fennecs.tests;

public class CrossTests
{
    [Theory]
    [InlineData(new[] { 1, 1, 1 })]
    [InlineData(new[] { 1, 1, 3 })]
    [InlineData(new[] { 1, 5, 1 })]
    [InlineData(new[] { 1, 1, 5 })]
    [InlineData(new[] { 5, 1, 1 })]
    [InlineData(new[] { 9, 5, 3 })]
    [InlineData(new[] { 42, 23, 69 })]
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
    internal static World Setup(out Archetype archetype)
    {
        var world = new World();
        var entity = world.Spawn().Add(42).Add("fox").Add(1.5f).Add(2.5).Add((byte)7);
        archetype = world.GetEntityMeta(entity).Archetype;
        return world;
    }

    internal static TypeExpression Plain<T>() => TypeExpression.Of<T>(Match.Plain);
    internal static TypeExpression Any<T>() => TypeExpression.Of<T>(Match.Any);


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


}
