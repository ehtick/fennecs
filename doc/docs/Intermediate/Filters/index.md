---
title: Filters
layout: doc
outline: [2, 3]
order: 20
description: 'Stream filters in fennecs narrow a Query on the fly - archetype-level Subset & Exclude sets, and LINQ-style per-entity Where(lambda) predicates on component values.'
---

# Filters

Sometimes, a dynamic on-the-fly filter is needed to process only a subset of Entities; this enables us to quickly adjust our ECS logic to do different subsets of work without requiring a growing amount of queries to be defined.

Streams are lightweight views over their Query, so filtering never mutates the original Stream nor the underlying Query – you simply get a new, narrower view to run.

## Two Flavors of Filtering

| Flavor | Granularity | How it narrows | Honored by |
|--------|-------------|----------------|------------|
| [Subset & Exclude](Subset.md) | whole **Archetypes** | additional `Has`/`Not`-style clauses that prune Archetypes before iteration | everything: `For`, `Job`, `Raw`, `Blit`, enumeration, `Count` |
| [Where](Where.md) | individual **Entities** | LINQ-style lambda predicates on component *values*, evaluated live during the run | `For` and `Job` |

Both compose freely: the Archetype filters prune entire tables up front, and the `Where` predicates skim the Entities that remain.

```csharp
var stream = world.Stream<Position, Health>();

var wounded = (stream with { Exclude = [ Comp<Dead>.Plain ] })  // Subset & Exclude
    .Where((in Health h) => h.Current < h.Max);                 // Where (lambda)
```

::: tip :neofox_floof_mug: Can I interest you in a Grape-Colored Example
There's an appetizer on this topic! Check out [Thanos](/cookbook/appetizers/Thanos.md) for an, ahem, "practical" example of using filters.
:::
