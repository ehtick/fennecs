---
title: Subset & Exclude
layout: doc
outline: [2, 3]
order: 1
description: 'Archetype-level Stream filters in fennecs: clone a Stream with a with-expression and set its Subset and Exclude component sets to prune whole Archetypes.'
---

# Subset Archetype Filters

The coarse (and fastest) way to narrow a Stream: additional component clauses that prune whole **Archetypes** before iteration even starts. Entire tables of Entities are skipped in one go – no per-Entity work at all.

## Creating a Stream Filter
Each `Stream<>` has two filter fields, `Subset` and `Exclude`, which are used to filter the Entities that are processed by the Stream.

You can specify them using the `with` syntax to create a new Stream with the filters applied. This gives you a new view that applies the filter for all its operations. It doesn't mutate the original Stream, nor the underlying Query.

```csharp
var stream = world.Stream<Position, Velocity>();

var filteredStream = stream with 
{
    Subset = [ Comp<Alive>.Plain ], // collection initializer
    Exclude = [ Comp<OneRing>.Matching(TheOneRing) ] // (the collections are immutable sets)
};
```

## Subset Clause
This works much like an additional `Has<>` clause.

> `includes only` Entities that have the given component or relation. Multiple `Has` statements can be compared to a logical `A AND B AND C`.

## Exclude Clause
This works much like an additional `Not<>` clause.

> `excludes` any Entities that have the given component. Multiple `Not` statements can be compared to a logical `NOT (A OR B OR C)`, aka. `(NOT A) AND (NOT B) AND (NOT C)`.

## Combining Filters
Subset and Exclude are `ImmutableSets`, so they can be combined to build or merge some filters together when specifying a new Stream using the `with` keyword.

```csharp
var stream = world.Stream<Position, Velocity>();

var filteredStream = stream with 
{
    Subset = otherFilter.Subset
        .Add(Comp<Alive>.Plain)
        .Remove(Comp<Dead>.Plain),
    Exclude = otherFilter.Exclude.Union([(Comp) Comp<Owes>.Matching(Match.Entity)]),
};
```

::: tip :neofox_thumbsup: Need to filter by component *values* instead?
Subset & Exclude decide by component *presence*. To skip Entities based on what their components *contain*, add a [Where](Where.md) lambda filter on top.
:::

## Future Features
The `Comp<T>` expression API used to create the necessary filter expressions is likely to have its API reviewed and tightened, to make the syntax more readable and easier to use. It might get unified into a Mask-like system as used internally to power QueryBuilders.
