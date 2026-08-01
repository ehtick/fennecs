// SPDX-License-Identifier: MIT

using fennecs.CRUD;

namespace fennecs;

/// <summary>
/// <para>
/// <b>EntityRef</b> — a live Entity, as handed out by Stream runners.
/// </para>
/// <para>
/// An EntityRef is guaranteed to be alive for its lifetime, so its component accessors skip
/// liveness checks and read the Entity's Archetype storage directly. As a <c>ref struct</c>, it
/// cannot be stored or captured — to keep an Entity around, convert it to a 64-bit
/// <see cref="fennecs.Entity"/> via <see cref="Entity"/> or the implicit conversion; that stored
/// handle carries the generation and can be validated later.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// Inside a runner, the World is in Deferred mode: structural changes (<see cref="Add{C}(C)"/>,
/// <see cref="Remove{C}()"/>, <see cref="Despawn"/>) are queued and applied after the loop, so
/// this reference stays valid for the remainder of the iteration — but reads after a deferred
/// Remove still see the old data.
/// </para>
/// <para>
/// As a <c>ref struct</c>, EntityRef can never be boxed into its interfaces — generic code
/// reaches them via <c>where T : IAddRemove&lt;T&gt;, allows ref struct</c> (constrained,
/// allocation-free dispatch).
/// </para>
/// </remarks>
public readonly ref struct EntityRef : IAddRemove<EntityRef>, IHasTyped
{
    private readonly Archetype _archetype;
    private readonly int _row;


    internal EntityRef(Archetype archetype, int row)
    {
        _archetype = archetype;
        _row = row;
    }


    /// <summary>
    /// The World this Entity lives in.
    /// </summary>
    public World World => _archetype.World;

    /// <summary>
    /// The stored (64-bit) form of this Entity, carrying its generation — the "keep me" operation.
    /// </summary>
    public Entity Entity => _archetype.World.EntityFor(_archetype.EntityStorage[_row]);

    /// <summary>
    /// Converts a live EntityRef into its storable 64-bit <see cref="fennecs.Entity"/> handle.
    /// </summary>
    public static implicit operator Entity(EntityRef self) => self.Entity;

    /// <summary>
    /// Always true while iterating — EntityRefs are only handed out for live Entities.
    /// (a <see cref="Despawn"/> during the loop is deferred, and takes effect after it)
    /// </summary>
    public bool Alive => Entity.Alive;


    #region Component Access

    /// <summary>
    /// Returns a reference to the Component of type <typeparamref name="C"/> on this Entity.
    /// Reads the Archetype storage directly when the Component lives here; falls back to the
    /// World lookup for Components stored in other Aspects.
    /// </summary>
    /// <param name="match">Specific (targeted) Match Expression for the Component type. No Wildcards!</param>
    /// <exception cref="InvalidOperationException">If the Entity does not have such a Component.</exception>
    public ref C Ref<C>(Match match = default) where C : notnull
    {
        if (_archetype.TryGetStorage(TypeExpression.Of<C>(match), out var storage)) return ref ((Storage<C>) storage).Span[_row];

        // Component may live in another Aspect of the World (or not at all — the World throws).
        return ref World.GetComponent<C>(Entity, match);
    }


    /// <summary>
    /// Checks if the Entity has a Plain Component of a specific type.
    /// </summary>
    public bool Has<C>() where C : notnull => Has<C>(default(Match));


    /// <summary>
    /// Checks if the Entity has a Component of a specific type.
    /// Allows for a <see cref="Match"/> Expression to be specified (Wildcards).
    /// </summary>
    public bool Has<C>(Match match) where C : notnull =>
        _archetype.Matches(TypeExpression.Of<C>(match)) || World.HasComponent<C>(Entity, match);


    /// <summary>
    /// Checks if the Entity has a relation Component backed by <typeparamref name="R"/> targeting the given Entity.
    /// </summary>
    public bool Has<R>(Entity relation) where R : notnull => Has<R>(new Relate(relation));


    /// <summary>
    /// Checks if the Entity has an Object Link with the specified linked object.
    /// </summary>
    public bool Has<L>(L linkedObject) where L : class => Has(Link<L>.With(linkedObject));


    /// <summary>
    /// Checks if the Entity has an Object Link of a specific type and specific target.
    /// </summary>
    public bool Has<L>(Link<L> link) where L : class => Has<L>((Match) link);

    #endregion


    #region Structural Changes (deferred inside runners)

    /// <summary>
    /// Adds a Plain Component with specific data to the Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Add<C>(C data) where C : notnull
    {
        World.AddComponent(Entity, TypeExpression.Of<C>(default(Match)), data);
        return this;
    }


    /// <summary>
    /// Adds a newable Plain Component to the Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Add<C>() where C : notnull, new() => Add(new C());


    /// <summary>
    /// Adds a newable relation Component targeting the given Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Add<T>(Entity relation) where T : notnull, new() => Add(new T(), relation);


    /// <summary>
    /// Adds a relation Component backed by <typeparamref name="R"/> targeting the given Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Add<R>(R value, Entity relation) where R : notnull
    {
        World.AddComponent(Entity, TypeExpression.Of<R>(relation), value);
        return this;
    }


    /// <summary>
    /// Adds an Object Link to the Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Add<L>(Link<L> link) where L : class
    {
        World.AddComponent(Entity, TypeExpression.Of<L>(link), link.Target);
        return this;
    }


    /// <summary>
    /// Removes a Plain Component from the Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Remove<C>() where C : notnull
    {
        World.RemoveComponent(Entity, TypeExpression.Of<C>(Match.Plain));
        return this;
    }


    /// <summary>
    /// Removes all Components of type <typeparamref name="C"/> matching the given Match Expression
    /// from the Entity. Wildcard Match Expressions are permitted and remove all matching Components
    /// in a single structural change. (deferred inside runners)
    /// </summary>
    /// <exception cref="InvalidOperationException">if the Entity has no Component matching the expression</exception>
    public EntityRef Remove<C>(Match match) where C : notnull
    {
        World.RemoveComponent(Entity, TypeExpression.Of<C>(match));
        return this;
    }


    /// <summary>
    /// Removes all Components of type <typeparamref name="C"/> matching the given Match Expression
    /// from the Entity, with an explicit conflict resolution mode: <see cref="RemoveConflict.Strict"/>
    /// (default) throws if no Component matches, <see cref="RemoveConflict.Allow"/> makes the
    /// removal idempotent (a no-op when nothing matches). (deferred inside runners)
    /// </summary>
    /// <exception cref="InvalidOperationException">under <see cref="RemoveConflict.Strict"/>, if the Entity has no Component matching the expression</exception>
    public EntityRef Remove<C>(Match match, RemoveConflict mode) where C : notnull
    {
        World.RemoveComponent(Entity, TypeExpression.Of<C>(match), mode);
        return this;
    }


    /// <summary>
    /// Removes a relation Component targeting the given Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Remove<R>(Entity relation) where R : notnull
    {
        World.RemoveComponent(Entity, TypeExpression.Of<R>(new Relate(relation)));
        return this;
    }


    /// <summary>
    /// Removes an Object Link with the specified linked object from the Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Remove<L>(L linkedObject) where L : class => Remove(Link<L>.With(linkedObject));


    /// <summary>
    /// Removes an Object Link from the Entity. (deferred inside runners)
    /// </summary>
    public EntityRef Remove<L>(Link<L> link) where L : class
    {
        World.RemoveComponent(Entity, link.TypeExpression);
        return this;
    }


    /// <summary>
    /// Despawns the Entity from its World. (deferred inside runners)
    /// </summary>
    public void Despawn() => World.Despawn(Entity);

    #endregion


    /// <inheritdoc cref="fennecs.Entity.ToString"/>
    public override string ToString() => Entity.ToString();
}
