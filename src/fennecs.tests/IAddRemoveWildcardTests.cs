// SPDX-License-Identifier: MIT

using fennecs.CRUD;

namespace fennecs.tests;

/// <summary>
/// Coverage for the <see cref="IAddRemove{SELF}.Remove{C}(Match)"/> overload, which accepts
/// Wildcard Match Expressions, across all implementers of the interface.
/// </summary>
public class IAddRemoveWildcardTests
{
    private record struct Marker(int Value);

    private record struct Rel(int Value);

    // Distinct required Component types for the generated EntityTemplate<C0..C5> arities.
    private record struct Req0(int Value);

    private record struct Req1(int Value);

    private record struct Req2(int Value);

    private record struct Req3(int Value);

    private record struct Req4(int Value);

    private record struct Req5(int Value);


    // Proves the overload is dispatchable through the interface itself (not just the implementers).
    private static SELF RemoveAllStrings<SELF>(IAddRemove<SELF> subject) => subject.Remove<string>(Match.Any);


    private static Entity SpawnTriKind(World world, Entity target, out string linked)
    {
        linked = "linked";
        return world.Spawn()
            .Add(new Marker(42))
            .Add("plain")
            .Add("related", target)
            .Add(Link.With(linked));
    }


    #region Entity

    [Fact]
    public void Entity_Remove_Any_Removes_Plain_Relation_And_Link()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = SpawnTriKind(world, target, out _);

        entity.Remove<string>(Match.Any);

        Assert.False(entity.Has<string>(Match.Any));
        Assert.False(entity.Has<string>());
        Assert.False(entity.Has<string>(target));
        Assert.False(entity.Has<string>(Match.Object));
    }


    [Fact]
    public void Entity_Remove_Any_Preserves_Other_Components()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = SpawnTriKind(world, target, out _);

        entity.Remove<string>(Match.Any);

        Assert.True(entity.Has<Marker>());
        Assert.Equal(new Marker(42), entity.Ref<Marker>());
    }


    [Fact]
    public void Entity_Remove_Entity_Wildcard_Removes_All_Relations_Only()
    {
        using var world = new World();
        var target1 = world.Spawn();
        var target2 = world.Spawn();
        var entity = SpawnTriKind(world, target1, out _)
            .Add("related, too", target2);

        entity.Remove<string>(Match.Entity);

        Assert.False(entity.Has<string>(target1));
        Assert.False(entity.Has<string>(target2));
        Assert.False(entity.Has<string>(Match.Entity));
        Assert.True(entity.Has<string>());
        Assert.True(entity.Has<string>(Match.Object));
    }


    [Fact]
    public void Entity_Remove_Object_Wildcard_Removes_Links_Only()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = SpawnTriKind(world, target, out _)
            .Add(Link.With("second link"));

        entity.Remove<string>(Match.Object);

        Assert.False(entity.Has<string>(Match.Object));
        Assert.True(entity.Has<string>());
        Assert.True(entity.Has<string>(target));
    }


    [Fact]
    public void Entity_Remove_Target_Wildcard_Keeps_Plain()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = SpawnTriKind(world, target, out _);

        entity.Remove<string>(Match.Target);

        Assert.False(entity.Has<string>(Match.Target));
        Assert.True(entity.Has<string>());
        Assert.Equal("plain", entity.Ref<string>());
    }


    [Fact]
    public void Entity_Remove_Plain_Match_Equals_Parameterless_Remove()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = SpawnTriKind(world, target, out _);

        entity.Remove<string>(Match.Plain);

        Assert.False(entity.Has<string>());
        Assert.True(entity.Has<string>(target));
        Assert.True(entity.Has<string>(Match.Object));
    }


    [Fact]
    public void Entity_Remove_Default_Match_Removes_Plain()
    {
        using var world = new World();
        var entity = world.Spawn().Add("plain");

        entity.Remove<string>(default(Match));

        Assert.False(entity.Has<string>());
    }


    [Fact]
    public void Entity_Remove_Specific_Relation_Match_Removes_Only_That_Relation()
    {
        using var world = new World();
        var target1 = world.Spawn();
        var target2 = world.Spawn();
        var entity = world.Spawn()
            .Add("one", target1)
            .Add("two", target2);

        entity.Remove<string>(Match.Relation(target1));

        Assert.False(entity.Has<string>(target1));
        Assert.True(entity.Has<string>(target2));
    }


    [Fact]
    public void Entity_Remove_Specific_Link_Match_Removes_Only_That_Link()
    {
        using var world = new World();
        var entity = world.Spawn()
            .Add(Link.With("hello"))
            .Add(Link.With("world"));

        entity.Remove<string>(Match.Link("hello"));

        Assert.False(entity.Has<string>("hello"));
        Assert.True(entity.Has<string>("world"));
    }


    [Fact]
    public void Entity_Remove_Wildcard_Throws_When_Nothing_Matches()
    {
        using var world = new World();
        var entity = world.Spawn().Add(new Marker(1));

        Assert.Throws<InvalidOperationException>(() => entity.Remove<string>(Match.Any));
    }


    [Fact]
    public void Entity_Remove_Entity_Wildcard_Throws_When_Only_Plain_Present()
    {
        using var world = new World();
        var entity = world.Spawn().Add("plain");

        Assert.Throws<InvalidOperationException>(() => entity.Remove<string>(Match.Entity));

        // the plain Component was not disturbed
        Assert.True(entity.Has<string>());
    }


    [Fact]
    public void Entity_Remove_Wildcard_Throws_On_Dead_Entity()
    {
        using var world = new World();
        var entity = world.Spawn().Add("plain");
        entity.Despawn();

        Assert.Throws<ObjectDisposedException>(() => entity.Remove<string>(Match.Any));
    }


    [Fact]
    public void Entity_Remove_Wildcard_Is_Fluent()
    {
        using var world = new World();
        var entity = world.Spawn().Add("plain");

        var result = entity.Remove<string>(Match.Any).Add(new Marker(7));

        Assert.Equal(entity, result);
        Assert.True(entity.Has<Marker>());
    }


    [Fact]
    public void Entity_Remove_Wildcard_Is_Deferred_While_World_Locked()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = SpawnTriKind(world, target, out _);

        using (world.Lock())
        {
            entity.Remove<string>(Match.Any);

            // Structural change is deferred; the Components are still present.
            Assert.True(entity.Has<string>(Match.Any));
        }

        // Lock disposed: the deferred removal has been applied.
        Assert.False(entity.Has<string>(Match.Any));
        Assert.True(entity.Has<Marker>());
    }


    [Fact]
    public void Entity_Remove_Wildcard_Dispatches_Through_Interface()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = SpawnTriKind(world, target, out _);

        var result = RemoveAllStrings(entity);

        Assert.Equal(entity, result);
        Assert.False(entity.Has<string>(Match.Any));
    }


    [Fact]
    public void Entity_Remove_Wildcard_Evicts_From_Aspect_When_Last_Owned_Component_Removed()
    {
        using var world = new World();
        var aspect = world.AddAspect("game").Owns<Rel>();

        var target = world.Spawn();
        var entity = world.Spawn()
            .Add(new Rel(1))
            .Add(new Rel(2), target);

        Assert.Equal(1, aspect.Count);

        entity.Remove<Rel>(Match.Any);

        Assert.Equal(0, aspect.Count);
        Assert.True(entity.Alive);
        Assert.False(entity.Has<Rel>(Match.Any));
    }

    #endregion


    #region Batch & Query

    [Fact]
    public void Batch_Remove_Entity_Wildcard_Removes_All_Relations()
    {
        using var world = new World();
        var target1 = world.Spawn();
        var target2 = world.Spawn();

        var entity1 = world.Spawn().Add(new Marker(1)).Add(new Rel(1), target1);
        var entity2 = world.Spawn().Add(new Marker(2)).Add(new Rel(2), target1).Add(new Rel(3), target2);

        var query = world.Query().Has<Rel>(Match.Entity).Compile();
        query.Batch(Batch.RemoveConflict.Strict).Remove<Rel>(Match.Entity).Submit();

        Assert.False(entity1.Has<Rel>(Match.Entity));
        Assert.False(entity2.Has<Rel>(Match.Entity));
        Assert.True(entity1.Has<Marker>());
        Assert.True(entity2.Has<Marker>());
    }


    [Fact]
    public void Batch_Remove_Any_Wildcard_Removes_All_Kinds()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = SpawnTriKind(world, target, out _);

        var query = world.Query().Has<string>(Match.Any).Compile();
        query.Batch(Batch.RemoveConflict.Strict).Remove<string>(Match.Any).Submit();

        Assert.False(entity.Has<string>(Match.Any));
        Assert.True(entity.Has<Marker>());
    }


    [Fact]
    public void Batch_Remove_Wildcard_Strict_Throws_Without_Matching_Has()
    {
        using var world = new World();
        world.Spawn().Add(new Marker(1));

        var query = world.Query().Has<Marker>().Compile();
        var batch = query.Batch(Batch.RemoveConflict.Strict);

        Assert.Throws<InvalidOperationException>(() => batch.Remove<string>(Match.Any));
    }


    [Fact]
    public void Batch_Remove_Wildcard_Allow_Is_Idempotent_On_Unmatched_Archetypes()
    {
        using var world = new World();
        var target = world.Spawn();

        var related = world.Spawn().Add(new Marker(1)).Add(new Rel(1), target);
        var unrelated = world.Spawn().Add(new Marker(2));

        var query = world.Query().Has<Marker>().Compile();
        query.Batch(Batch.RemoveConflict.Allow).Remove<Rel>(Match.Entity).Submit();

        Assert.False(related.Has<Rel>(Match.Entity));
        Assert.True(unrelated.Has<Marker>());
        Assert.Equal(new Marker(2), unrelated.Ref<Marker>());
    }


    [Fact]
    public void Batch_Wildcard_Removal_Conflicts_With_Concrete_Addition()
    {
        using var world = new World();
        world.Spawn().Add("present");

        var query = world.Query().Has<string>().Compile();
        var batch = query.Batch(Batch.AddConflict.Preserve, Batch.RemoveConflict.Allow).Add("added");

        Assert.Throws<InvalidOperationException>(() => batch.Remove<string>(Match.Any));
    }


    [Fact]
    public void Batch_Concrete_Addition_Conflicts_With_Wildcard_Removal()
    {
        using var world = new World();
        world.Spawn().Add("present");

        var query = world.Query().Has<string>().Compile();
        var batch = query.Batch(Batch.AddConflict.Preserve, Batch.RemoveConflict.Allow).Remove<string>(Match.Any);

        Assert.Throws<InvalidOperationException>(() => batch.Add("added"));
    }


    [Fact]
    public void Batch_Duplicate_Wildcard_Removal_Throws()
    {
        using var world = new World();
        world.Spawn().Add("present");

        var query = world.Query().Has<string>().Compile();
        var batch = query.Batch(Batch.RemoveConflict.Allow).Remove<string>(Match.Any);

        Assert.Throws<InvalidOperationException>(() => batch.Remove<string>(Match.Any));
    }


    [Fact]
    public void Query_OneShot_Remove_With_Wildcard()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity1 = world.Spawn().Add(new Rel(1), target).Add(new Marker(1));
        var entity2 = world.Spawn().Add(new Rel(2), target);

        var query = world.Query().Has<Rel>(Match.Entity).Compile();
        query.Remove<Rel>(Match.Entity);

        Assert.False(entity1.Has<Rel>(Match.Entity));
        Assert.False(entity2.Has<Rel>(Match.Entity));
        Assert.True(entity1.Has<Marker>());
    }


    [Fact]
    public void Query_OneShot_Wildcard_Remove_Is_Deferred_While_World_Locked()
    {
        using var world = new World();
        var target = world.Spawn();
        var entity = world.Spawn().Add(new Rel(1), target).Add(new Marker(1));

        var query = world.Query().Has<Rel>(Match.Entity).Compile();

        using (world.Lock())
        {
            query.Remove<Rel>(Match.Entity);
            Assert.True(entity.Has<Rel>(Match.Entity));
        }

        Assert.False(entity.Has<Rel>(Match.Entity));
        Assert.True(entity.Has<Marker>());
    }

    #endregion


    #region EntityTemplate

    [Fact]
    public void Template_Remove_Any_Removes_All_Configured_Kinds()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add(new Marker(5))
            .Add("plain")
            .Add("related", target)
            .Add(Link.With("linked"));

        template.Remove<string>(Match.Any);
        var entity = template.Spawn();

        Assert.False(entity.Has<string>(Match.Any));
        Assert.Equal(new Marker(5), entity.Ref<Marker>());
    }


    [Fact]
    public void Template_Remove_Entity_Wildcard_Removes_Relations_Only()
    {
        using var world = new World();
        var target1 = world.Spawn();
        var target2 = world.Spawn();

        using var template = world.Template()
            .Add("plain")
            .Add("one", target1)
            .Add("two", target2)
            .Add(Link.With("linked"));

        template.Remove<string>(Match.Entity);
        var entity = template.Spawn();

        Assert.False(entity.Has<string>(Match.Entity));
        Assert.True(entity.Has<string>());
        Assert.True(entity.Has<string>(Match.Object));
    }


    [Fact]
    public void Template_Remove_Plain_Match_Equals_Parameterless_Remove()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add("plain")
            .Add("related", target);

        template.Remove<string>(Match.Plain);
        var entity = template.Spawn();

        Assert.False(entity.Has<string>());
        Assert.True(entity.Has<string>(target));
    }


    [Fact]
    public void Template_Remove_Specific_Relation_Match()
    {
        using var world = new World();
        var target1 = world.Spawn();
        var target2 = world.Spawn();

        using var template = world.Template()
            .Add("one", target1)
            .Add("two", target2);

        template.Remove<string>(Match.Relation(target1));
        var entity = template.Spawn();

        Assert.False(entity.Has<string>(target1));
        Assert.True(entity.Has<string>(target2));
    }


    [Fact]
    public void Template_Remove_Wildcard_Throws_When_Nothing_Matches()
    {
        using var world = new World();
        using var template = world.Template().Add(new Marker(1));

        Assert.Throws<InvalidOperationException>(() => template.Remove<string>(Match.Any));
    }


    [Fact]
    public void Template_Remove_Wildcard_Throws_When_Disposed()
    {
        using var world = new World();
        var template = world.Template().Add("plain");
        template.Dispose();

        Assert.Throws<ObjectDisposedException>(() => template.Remove<string>(Match.Any));
    }


    [Fact]
    public void Template_Remove_Wildcard_Dispatches_Through_Interface()
    {
        using var world = new World();
        using var template = world.Template().Add("plain");

        var result = RemoveAllStrings(template);

        Assert.Same(template, result);
        Assert.False(template.Spawn().Has<string>(Match.Any));
    }

    #endregion


    #region EntityTemplate<C0> (generated)

    [Fact]
    public void Generic_Template_Remove_Any_Removes_Configured_Components()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add("plain")
            .Add("related", target)
            .Needs<Marker>();

        template.Remove<string>(Match.Any);
        var entity = template.Spawn(new Marker(9));

        Assert.False(entity.Has<string>(Match.Any));
        Assert.Equal(new Marker(9), entity.Ref<Marker>());
    }


    [Fact]
    public void Generic_Template_Remove_Wildcard_Covering_Required_Plain_Throws()
    {
        using var world = new World();
        using var template = world.Template().Add("optional").Needs<Marker>();

        var exception = Assert.Throws<InvalidOperationException>(() => template.Remove<Marker>(Match.Any));
        Assert.Contains("required", exception.Message);
    }


    [Fact]
    public void Generic_Template_Remove_Wildcard_Covering_Required_Relation_Throws()
    {
        using var world = new World();
        var target = world.Spawn();
        using var template = world.Template().Needs<Rel>(target);

        Assert.Throws<InvalidOperationException>(() => template.Remove<Rel>(Match.Entity));
        Assert.Throws<InvalidOperationException>(() => template.Remove<Rel>(Match.Any));
        Assert.Throws<InvalidOperationException>(() => template.Remove<Rel>(Match.Target));
    }


    [Fact]
    public void Generic_Template_Remove_Wildcard_Not_Covering_Required_Relation_Throws_No_Match()
    {
        using var world = new World();
        var target = world.Spawn();
        using var template = world.Template().Needs<Rel>(target);

        // Match.Object does not cover the required Entity relation, and nothing else is configured.
        var exception = Assert.Throws<InvalidOperationException>(() => template.Remove<Rel>(Match.Object));
        Assert.Contains("no Component matching", exception.Message);
    }


    [Fact]
    public void Generic_Template_Remove_Specific_Match_Still_Works()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add("plain")
            .Add("related", target)
            .Needs<Marker>();

        template.Remove<string>(Match.Relation(target));
        var entity = template.Spawn(new Marker(1));

        Assert.True(entity.Has<string>());
        Assert.False(entity.Has<string>(target));
    }


    [Fact]
    public void Generic_Template_Remove_Wildcard_Throws_When_Nothing_Matches()
    {
        using var world = new World();
        using var template = world.Template().Needs<Marker>();

        Assert.Throws<InvalidOperationException>(() => template.Remove<string>(Match.Any));
    }

    #endregion


    #region EntityTemplate<C0..C5> per-arity coverage

    // Each arity is a distinct generated class; these walk the full Remove<T>(Match) cycle on
    // every one of them: wildcard removal (skipping a non-matching Component), the exact
    // (non-wildcard) path via Match.Plain, the no-match throw, and the required-Component throw.

    [Fact]
    public void Generic_Template_Arity1_Wildcard_Remove_Full_Cycle()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add(new Marker(1))
            .Add("plain")
            .Add("related", target)
            .Needs<Req0>();

        template.Remove<string>(Match.Any);
        template.Remove<Marker>(Match.Plain);

        Assert.Throws<InvalidOperationException>(() => template.Remove<string>(Match.Any));
        Assert.Throws<InvalidOperationException>(() => template.Remove<Req0>(Match.Any));

        var entity = template.Spawn(new Req0(1));
        Assert.False(entity.Has<string>(Match.Any));
        Assert.False(entity.Has<Marker>());
        Assert.True(entity.Has<Req0>());
    }


    [Fact]
    public void Generic_Template_Arity2_Wildcard_Remove_Full_Cycle()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add(new Marker(1))
            .Add("plain")
            .Add("related", target)
            .Needs<Req0>()
            .Needs<Req1>();

        template.Remove<string>(Match.Any);
        template.Remove<Marker>(Match.Plain);

        Assert.Throws<InvalidOperationException>(() => template.Remove<string>(Match.Any));
        Assert.Throws<InvalidOperationException>(() => template.Remove<Req1>(Match.Any));

        var entity = template.Spawn(new Req0(1), new Req1(2));
        Assert.False(entity.Has<string>(Match.Any));
        Assert.False(entity.Has<Marker>());
        Assert.True(entity.Has<Req0>());
        Assert.True(entity.Has<Req1>());
    }


    [Fact]
    public void Generic_Template_Arity3_Wildcard_Remove_Full_Cycle()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add(new Marker(1))
            .Add("plain")
            .Add("related", target)
            .Needs<Req0>()
            .Needs<Req1>()
            .Needs<Req2>();

        template.Remove<string>(Match.Any);
        template.Remove<Marker>(Match.Plain);

        Assert.Throws<InvalidOperationException>(() => template.Remove<string>(Match.Any));
        Assert.Throws<InvalidOperationException>(() => template.Remove<Req2>(Match.Any));

        var entity = template.Spawn(new Req0(1), new Req1(2), new Req2(3));
        Assert.False(entity.Has<string>(Match.Any));
        Assert.False(entity.Has<Marker>());
        Assert.True(entity.Has<Req0>());
        Assert.True(entity.Has<Req2>());
    }


    [Fact]
    public void Generic_Template_Arity4_Wildcard_Remove_Full_Cycle()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add(new Marker(1))
            .Add("plain")
            .Add("related", target)
            .Needs<Req0>()
            .Needs<Req1>()
            .Needs<Req2>()
            .Needs<Req3>();

        template.Remove<string>(Match.Any);
        template.Remove<Marker>(Match.Plain);

        Assert.Throws<InvalidOperationException>(() => template.Remove<string>(Match.Any));
        Assert.Throws<InvalidOperationException>(() => template.Remove<Req3>(Match.Any));

        var entity = template.Spawn(new Req0(1), new Req1(2), new Req2(3), new Req3(4));
        Assert.False(entity.Has<string>(Match.Any));
        Assert.False(entity.Has<Marker>());
        Assert.True(entity.Has<Req0>());
        Assert.True(entity.Has<Req3>());
    }


    [Fact]
    public void Generic_Template_Arity5_Wildcard_Remove_Full_Cycle()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add(new Marker(1))
            .Add("plain")
            .Add("related", target)
            .Needs<Req0>()
            .Needs<Req1>()
            .Needs<Req2>()
            .Needs<Req3>()
            .Needs<Req4>();

        template.Remove<string>(Match.Any);
        template.Remove<Marker>(Match.Plain);

        Assert.Throws<InvalidOperationException>(() => template.Remove<string>(Match.Any));
        Assert.Throws<InvalidOperationException>(() => template.Remove<Req4>(Match.Any));

        var entity = template.Spawn(new Req0(1), new Req1(2), new Req2(3), new Req3(4), new Req4(5));
        Assert.False(entity.Has<string>(Match.Any));
        Assert.False(entity.Has<Marker>());
        Assert.True(entity.Has<Req0>());
        Assert.True(entity.Has<Req4>());
    }


    [Fact]
    public void Generic_Template_Arity6_Wildcard_Remove_Full_Cycle()
    {
        using var world = new World();
        var target = world.Spawn();

        using var template = world.Template()
            .Add(new Marker(1))
            .Add("plain")
            .Add("related", target)
            .Needs<Req0>()
            .Needs<Req1>()
            .Needs<Req2>()
            .Needs<Req3>()
            .Needs<Req4>()
            .Needs<Req5>();

        template.Remove<string>(Match.Any);
        template.Remove<Marker>(Match.Plain);

        Assert.Throws<InvalidOperationException>(() => template.Remove<string>(Match.Any));
        Assert.Throws<InvalidOperationException>(() => template.Remove<Req5>(Match.Any));

        var entity = template.Spawn(new Req0(1), new Req1(2), new Req2(3), new Req3(4), new Req4(5), new Req5(6));
        Assert.False(entity.Has<string>(Match.Any));
        Assert.False(entity.Has<Marker>());
        Assert.True(entity.Has<Req0>());
        Assert.True(entity.Has<Req5>());
    }

    #endregion
}
