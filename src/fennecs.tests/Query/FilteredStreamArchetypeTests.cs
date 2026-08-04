namespace fennecs.tests.Query;

using fennecs;

public class FilteredStreamArchetypeTests
{
    private readonly World _world;
    private readonly Stream<ComponentA> _stream;

    public FilteredStreamArchetypeTests()
    {
        _world = new World();
        _stream = _world.Query<ComponentA>().Stream();
    }


    [Fact]
    public void Has_ShouldNarrowDownResults()
    {
        // Arrange (entity2 is in the stream but lacks ComponentB, in a distinct archetype)
        var entity1 = _world.Spawn().Add(new ComponentA());
        var entity2 = _world.Spawn().Add(new ComponentA()).Add(new ComponentC());
        var entity3 = _world.Spawn().Add(new ComponentA()).Add(new ComponentB());

        // Act
        var filtered = _stream.Has(Comp<ComponentB>.Plain);

        var results = filtered.ToList().Select(r => r.Item1).ToArray();

        // Assert
        Assert.DoesNotContain(entity1, results);
        Assert.DoesNotContain(entity2, results);
        Assert.Contains(entity3, results);

        //Ensure count is reduced
        Assert.Single(results);
    }

    [Fact]
    public void Not_ShouldNarrowDownResults()
    {
        // Arrange (entity2 is in the stream and has ComponentB, in a distinct archetype)
        var entity1 = _world.Spawn().Add(new ComponentA());
        var entity2 = _world.Spawn().Add(new ComponentA()).Add(new ComponentB()).Add(new ComponentC());
        var entity3 = _world.Spawn().Add(new ComponentA()).Add(new ComponentB());

        // Act
        var filtered = _stream.Not(Comp<ComponentB>.Plain);

        var results = filtered.Select(r => r.Item1).ToArray();

        // Assert
        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);
        Assert.DoesNotContain(entity3, results);

        //Ensure count is reduced
        Assert.Single(results);
    }

    [Fact]
    public void Not_ShouldNarrowDownResults_EntityAny()
    {
        using var world = new World();

        // Arrange
        var target = world.Spawn();
        var entity1 = world.Spawn().Add(new ComponentA());
        var entity2 = world.Spawn().Add(new ComponentA()).Add(new ComponentB(), target);

        var stream = world.Query<ComponentA>().Stream();

        // Act
        var filtered = stream.Not(Comp<ComponentB>.Matching(Match.Entity));

        var results = new List<Entity>();
        filtered.For((in entity, ref _) => results.Add(entity));

        // Assert
        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);

        //Ensure count is reduced
        Assert.Single(results);
    }

    [Fact]
    public void Not_ShouldNarrowDownResults_MatchAny()
    {
        using var world = new World();

        // Arrange
        var target = world.Spawn();
        var entity1 = world.Spawn().Add(new ComponentA());
        var entity2 = world.Spawn().Add(new ComponentA()).Add(new ComponentB(), target);

        var stream = world.Query<ComponentA>().Stream();

        // Act
        var filtered = stream.Not(Comp<ComponentB>.Matching(Match.Any));

        var results = new List<Entity>();
        filtered.For((in entity, ref _) => results.Add(entity));

        // Assert
        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);

        //Ensure count is reduced
        Assert.Single(results);
    }

    [Fact]
    public void Has_ShouldMatchWildcardRelations()
    {
        using var world = new World();

        // Arrange
        var target = world.Spawn();
        var entity1 = world.Spawn().Add(new ComponentA());
        var entity2 = world.Spawn().Add(new ComponentA()).Add(new ComponentB(), target);

        var stream = world.Query<ComponentA>().Stream();

        // Act - the wildcard expression matches the relation via the expanded signature
        var filtered = stream.Has(Comp<ComponentB>.Matching(Match.Any));

        var results = new List<Entity>();
        filtered.For((in entity, ref _) => results.Add(entity));

        // Assert
        Assert.DoesNotContain(entity1, results);
        Assert.Contains(entity2, results);
        Assert.Single(results);
    }

    [Fact]
    public void Count_Counts_Wildcard_Permutations_Without_Predicates()
    {
        using var world = new World();

        // Two relations of the same backing type -> two storages -> two permutations per entity.
        var target1 = world.Spawn();
        var target2 = world.Spawn();
        var entity = world.Spawn().Add(new ComponentA());
        entity.Add(new ComponentB(), target1);
        entity.Add(new ComponentB(), target2);

        var stream = world.Query<ComponentB>(Match.Entity).Stream();
        var filtered = stream.Has(Comp<ComponentA>.Plain);

        // No per-entity predicates set: the count must still match one visit per permutation.
        var visits = 0;
        filtered.For((in _, ref _) => visits++);
        Assert.Equal(2, visits);
        Assert.Equal(visits, filtered.Count);
    }


    [Fact]
    public void Filters_SupportMultipleExpressions()
    {
        using var world = new World();

        // Arrange
        var target = world.Spawn();
        var entity1 = world.Spawn().Add(new ComponentA()).Add(new ComponentC());
        var entity2 = world.Spawn().Add(new ComponentA()).Add(new ComponentC()).Add(new ComponentB());
        var entity3 = world.Spawn().Add(new ComponentA()).Add(new ComponentC()).Add(new ComponentD(), target);

        var stream = world.Query<ComponentA>().Stream();

        // Act - Comp must be comparable for ImmutableSortedSet to hold more than one expression
        var filtered = stream
            .Has(Comp<ComponentA>.Plain, Comp<ComponentC>.Plain)
            .Not(Comp<ComponentB>.Plain, Comp<ComponentD>.Matching(target));

        var results = new List<Entity>();
        filtered.For((in entity, ref _) => results.Add(entity));

        // Assert
        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);
        Assert.DoesNotContain(entity3, results);
        Assert.Single(results);
    }
}


public struct ComponentA
{
    // Add properties or fields relevant to the Component here
    // For testing purposes, it can be left empty
}


public struct ComponentB
{
    // Add properties or fields relevant to the Component here
    // For testing purposes, it can be left empty
}


public struct ComponentC
{
    // Add properties or fields relevant to the Component here
    // For testing purposes, it can be left empty
}


public struct ComponentD
{
    // Add properties or fields relevant to the Component here
    // For testing purposes, it can be left empty
}


public struct ComponentE
{
    // Add properties or fields relevant to the Component here
    // For testing purposes, it can be left empty
}
