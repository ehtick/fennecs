---
title: Filters
layout: doc
outline: [2, 3]
order: 20
description: 'Stream filters in fennecs narrow a Query on the fly - FilteredStream views with archetype-level Has & Not clauses, and LINQ-style per-entity Where(lambda) predicates on component values.'
---

# Stream Filters

Sometimes, a dynamic on-the-fly filter is needed to process only a subset of Entities; this enables us to quickly adjust our ECS logic to do different subsets of work without requiring a growing amount of queries to be defined and tracked.

Calling `Has`, `Not`, or `Where` on a Stream returns a `FilteredStream<>` – a *lightweight view* that carries all the filter state. Filtering never mutates the original Stream nor the underlying Query – you simply use a new, narrower view to run.

## Two Flavors of Filtering

| Flavor | Granularity | How it narrows | Cost |
|--------|-------------|----------------|------|
| [Has & Not](Archetypes.md) | whole **Archetypes** | additional `Has`/`Not` clauses that prune Archetypes before iteration | one signature check per Archetype |
| [Where](Where.md) | individual **Entities** | LINQ-style lambda predicates on component *values*, evaluated live during the run | one predicate call per Entity |

Both compose freely: the Archetype filters prune entire tables up front, and the `Where` predicates skim the Entities that remain. All of a `FilteredStream`'s operations honor all of its filters: `For`, `Job`, `Count`, enumeration, and `Despawn`.

```csharp
var stream = world.Stream<Position, Health>();

var wounded = stream
    .Not(Comp<Dead>.Plain)                      // Has & Not (Archetypes)
    .Where((in Health h) => h.Current < h.Max); // Where (lambda)
```

## Both Sides of the Cut

`FilteredStream` runners can visit the *complement*, too. Each `For` and `Job` variant has an overload taking two delegates: `included` runs on every Entity passing all filters, and `excluded` runs on every other Entity the underlying Query matches – whether its whole Archetype was pruned or it just failed a `Where` predicate. Together, the two delegates visit each Entity exactly once.

```csharp
wounded.For(
    included: (ref Position p, ref Health h) => LimpTowardsHealer(ref p),
    excluded: (ref Position p, ref Health h) => h.Regenerate());
```

::: info :neofox_science: One Pass, No Wildcards
Two-delegate runners partition the Query in a single pass – no second iteration, no allocation. Since the partition must visit each Entity exactly once, they are not available on Streams with Wildcard [Match Expressions](/docs/Basic/Queries/Matching.md).
:::

::: tip :neofox_floof_mug: Can I interest you in a Grape-Colored Example
There's an appetizer on this topic! Check out [Thanos](/cookbook/appetizers/Thanos.md) for an, ahem, "practical" example of using filters.
:::
