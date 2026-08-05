namespace fennecs.CRUD;

/// <summary>
/// Objects of this type can perform Add and Remove operations on Entities or sets of Entities.
/// </summary>
/// <remarks>
/// <c>allows ref struct</c> admits <see cref="EntityRef"/> as an implementer. Generic code
/// constrained on this interface must re-declare the anti-constraint to accept it:
/// <c>where T : IAddRemove&lt;T&gt;, allows ref struct</c>.
/// </remarks>
public interface IAddRemove<out SELF> where SELF : allows ref struct
{
    /// <summary>
    /// Add a default, Plain newable Component of type C to the Entity/Entities.
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Add<C>() where C : notnull, new();

    /// <summary>
    /// Add a Plain Component with value of type C to the Entity/Entities.
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Add<C>(C value) where C : notnull;

    /// <summary>
    /// Add a newable Relation Component backed by a value of type R to the Entity/Entities. (default value)
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Add<T>(Entity target) where T : notnull, new();

    /// <summary>
    /// Add a Relation Component backed by a value of type R to the Entity/Entities.
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Add<R>(R value, Entity relation) where R : notnull;


    /// <summary>
    /// Add a Object Link Component with an Object of type L to the Entity/Entities.
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Add<L>(Link<L> link) where L : class;

    /// <summary>
    /// Remove a Plain Component of type C from the Entity/Entities.
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Remove<C>() where C : notnull;

    /// <summary>
    /// Remove all Components of type C matching the given Match Expression from the Entity/Entities.
    /// </summary>
    /// <remarks>
    /// <para>Accepts Wildcards: <see cref="Match.Any"/> removes Plain, Relation, and Object Link Components of the type;
    /// <see cref="Match.Target"/> removes Relations and Object Links; <see cref="Match.Entity"/> removes all Relations;
    /// <see cref="Match.Object"/> removes all Object Links.</para>
    /// <para>Specific terms work as well: <see cref="Match.Plain"/> is equivalent to <see cref="Remove{C}()"/>,
    /// and <see cref="Match.Relation"/> / <see cref="Match.Link{T}"/> remove that single Relation or Link.</para>
    /// </remarks>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Remove<C>(Match match) where C : notnull;

    /// <summary>
    /// Remove a Relation Component of type R with the specified relation from the Entity/Entities.
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Remove<R>(Entity relation) where R : notnull;

    /// <summary>
    /// Remove an Object Link Component with the specified linked object from the Entity/Entities.
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Remove<L>(L linkedObject) where L : class;

    /// <summary>
    /// Remove an Object Link component with the specified link from the Entity/Entities.
    /// </summary>
    /// <returns>itself (fluent pattern)</returns>
    public SELF Remove<L>(Link<L> link) where L : class;
}
