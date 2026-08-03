---
title: Has & Not
layout: doc
outline: [2, 3]
order: 1
description: 'Archetype-level Stream filters in fennecs: Has and Not clauses on a FilteredStream prune whole Archetypes by component presence before iteration starts.'
---

# Has & Not Archetype Filters

The coarse (and fastest) way to narrow a Stream: additional component clauses that prune whole **Archetypes** before iteration even starts. Entire tables of Entities are skipped in one go – no per-Entity work at all.

## Creating a FilteredStream

`Stream<>.Has(...)` and `Stream<>.Not(...)` each return a `FilteredStream<>` applying the clause for all its operations. The original Stream and the underlying Query are untouched.

```csharp
var stream = world.Stream<Position, Velocity>();

var filtered = stream
    .Has(Comp<Alive>.Plain)
    .Not(Comp<OneRing>.Matching(TheOneRing));
```

## Has Clause
This works exactly like an additional `Has<>` clause on a QueryBuilder.

> `includes only` Entities whose Archetype has **all** of the given components or relations. Multiple expressions – whether passed in one call or chained – combine to a logical `A AND B AND C`.

## Not Clause
This works exactly like an additional `Not<>` clause on a QueryBuilder.

> `excludes` any Entities whose Archetype has **any** of the given components. Multiple expressions can be compared to a logical `NOT (A OR B OR C)`, aka. `(NOT A) AND (NOT B) AND (NOT C)`.

## Chaining Accumulates

`Has` and `Not` accumulate on a `FilteredStream` – each call unions its expressions into the existing clause:

```csharp
var picky = stream
    .Has(Comp<Alive>.Plain)
    .Has(Comp<Hungry>.Plain)               // same as .Has(Comp<Alive>.Plain, Comp<Hungry>.Plain)
    .Not(Comp<Dead>.Plain, Comp<Sleeping>.Plain);
```

::: tip :neofox_thumbsup: Need to filter by component *values* instead?
Has & Not decide by component *presence*. To skip Entities based on what their components *contain*, add a [Where](Where.md) lambda filter on top.
:::

## Future Features
The `Comp<T>` expression API used to create the necessary filter expressions is likely to have its API reviewed and tightened, to make the syntax more readable and easier to use. It might get unified into a Mask-like system as used internally to power QueryBuilders.
