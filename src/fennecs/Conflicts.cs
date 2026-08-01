// SPDX-License-Identifier: MIT

namespace fennecs;

/// <summary>
/// Specifies behavior when adding a Component that may already be present on the target
/// Entity or Archetype (used by <see cref="Batch"/> operations).
/// </summary>
public enum AddConflict
{
    /// <summary>
    /// Disallows the addition of Components that could already be present in a query.
    /// </summary>
    /// <remarks>
    /// Exclude the Component from the query via <see cref="QueryBuilderBase{QB}.Not{T}(Match)"/> or similar
    /// means. If you want to allow the addition of Components that are already present, use <see cref="Preserve"/>
    /// to keep any values already present, or use <see cref="Replace"/> if you'd like to overwrite the Component
    /// value everywhere it is already encountered in the query.
    /// </remarks>
    Strict = default,

    /// <summary>
    /// Keeps the existing Component data whenever trying to add a duplicate.
    /// </summary>
    Preserve,

    /// <summary>
    /// Overwrites existing Component data with the addded component if it is already present.
    /// </summary>
    /// <remarks>
    /// Alternatively, you can use the faster <see cref="Stream{C0}.Blit"/> if you
    /// can ensure that the component is present on all Entities in the query.
    /// </remarks>
    Replace,
}


/// <summary>
/// Specifies behavior when removing a Component that may not be present on the target
/// Entity or Archetype (used by <see cref="Batch"/> operations and
/// <see cref="Entity.Remove{C}(Match, RemoveConflict)"/>).
/// </summary>
public enum RemoveConflict
{
    /// <summary>
    /// Disallow the remove operation if the Component to be removed is not present on the
    /// Entity, or (for Batches) not guaranteed to be present on ALL matched Archetypes,
    /// see <see cref="QueryBuilderBase{QB}.Has{T}(Match)"/>.
    /// </summary>
    Strict = default,

    /// <summary>
    /// Allow operating on Entities or Archetypes where the Component to be removed is not
    /// present. Removal operations are Idempotent on these, i.e. they don't change them
    /// on their own.
    /// </summary>
    Allow,
}
