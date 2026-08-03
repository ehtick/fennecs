---
title: Where (Lambda)
layout: doc
outline: [2, 3]
order: 2
description: 'LINQ-style per-entity Stream filters in fennecs: Stream<>.Where(lambda) predicates on component values, evaluated live during For and Job runs.'
---

# `Where` Lambda Filters

Sometimes narrowing by Archetype isn't enough – you want to skip Entities based on their component **values**. `Stream<>.Where(...)` attaches a predicate for one of the Stream's component types, and the runners skip every Entity whose values don't pass.

```csharp
var stream = world.Query<Health, Position, Velocity>().Stream();

var wounded = stream.Where((in Health h) => h.Current < h.Max);

wounded.For((ref health, ref position, ref velocity) =>
{
    // spawn blood splatter, etc.
    // only wounded entities make it in here! since the filter rejects early,
    // this function is called & position/velocity refs only passed when needed. 
});
```

:::info :neofox_think: ISN'T THIS JUST LINQ?
The semantics may feel similar, but there are no allocations and filters benefit from [.NET's PGO & inlining](https://devblogs.microsoft.com/dotnet/bing-on-dotnet-8-the-impact-of-dynamic-pgo/) at runtime. It's a short-cirquit way to omit excess memory transfers and function calls at the expense of one or two quick tests!
:::

It is up to you to examine your program's domain and decide whether Stream Filters, Stream Subsets, or fixed Query Matching are the way to go.

Filters are best when they can *reject* a large number of Entities based on a simple test, saving you the most memory bandwidth and in turn sparing you fragmentation. In the above example, the alternative approach would be to add and maintain a `Wounded` Tag, putting all wounded Entities into a separate archetype from their normal one.


## Semantics

`Where` takes a `ComponentFilter<C>` – a lightweight predicate over a component value:

```csharp
public delegate bool ComponentFilter<C>(in C c);
```

- return `true` to include the Entity, `false` to skip it
- `C` must be one of the Stream's type parameters; the lambda's parameter type selects the `Where` overload, so spell it out: `(in Health h) => ...`
- like [Subset & Exclude](Subset.md), `Where` returns a **new Stream** – the original Stream and the underlying Query are untouched

## One Slot per Stream Type

Each stream type has exactly one predicate slot. Chaining `Where` for *different* components combines them (logical `AND`); calling `Where` again for the *same* component **replaces** that slot's predicate:

```csharp
var apex = stream
    .Where((in Health h) => h.Current > 100)                  // Health slot
    .Where((in Velocity v) => v.Value.LengthSquared() > 1f);  // Velocity slot – ANDed!

var lowHp = apex.Where((in Health h) => h.Current < 10);      // replaces the Health predicate

// two conditions on the same component? combine them in one lambda:
var goldilocks = stream.Where((in Health h) => h.Current > 10 && h.Current < 100);
```

## Where `Where` applies (and where it doesn't!)

The predicates run per Entity, live inside the runners:

| Operation | Honors `Where`? |
|-----------|-----------------|
| `For` | ✅ per Entity, on every run |
| `Job` | ✅ per Entity *(predicate must be thread-safe, just like your delegate)* |
| `Raw` | ⛔ hands out whole memory blocks |
| `Blit` | ⛔ writes whole Archetypes |
| `foreach` / LINQ enumeration | ⛔ |
| `Count` | ⛔ counts the [Subset & Exclude](Subset.md)-filtered Archetypes |

::: info :neofox_science: Live Evaluation
Predicates are evaluated fresh on every run – change your World's data, and the same filtered Stream processes a different set of Entities next frame. Keep them cheap: unlike [Subset & Exclude](Subset.md), which skips whole Archetypes, `Where` still visits each Entity to ask.
:::

## Combining with Subset & Exclude

Both mechanisms compose freely – the Archetype filters prune entire tables up front, then the predicates skim the Entities that remain:

```csharp
var survivors = (stream with { Exclude = [ Comp<Dead>.Plain ] })
    .Where((in Health h) => h.Current < h.Max);
```
