---
title: 5. Thanos (Filters)
outline: [2, 3]
order: 5
description: Teaches fennecs stream filtering with Has and Not, then uses FilteredStream.Despawn to bulk-remove half the entity population in one call.
---

# How to Snap ~25% of the World away

::: info :neofox_floof_mug: MMMH, REAL CODE
This **RUNS**! *Playful premises aside*, this is a functioning showcase of **fenn**ecs principles.

Get comfy, grab a cup of ~~Java~~ ~~CoffeeScript~~ ~~Visual J#~~ whatever, and get your paws dirty playing around in the code! It's good fun!

All `.csproj` and `.cs` files are [over here on Github!](https://github.com/outfox/fennecs/tree/main/src/cookbook) 

:::

### Premise
Hey there, mighty Titan (who flunked probabilitics)! Ready to bring perfect balance to your `fennecs.World`?

In this example, we'll show you how to use fennecs' `Has` and `Not` stream filters to ~~snap away~~ `Despawn` half the entities in your world. 

::: details SPOILER
Well... randomly half of randomly half!
:::
 
### How it works
First we create a bunch of entities, then flip two coins for each one to hand out "Lucky" and "Unlucky" components. Next, we narrow down our Stream with the `Has` and `Not` filters — targeting the Unlucky while leaving anyone Lucky out of harm's way.

Finally, we unleash the power of the ~~Infinity Gauntlet~~ `FilteredStream.Despawn()` to bring an awkward balance to the Universe. 

I'm sure you already see that nothing can go wrong! Let's get snapping!


::: code-group
<<< ../../../src/cookbook/Thanos/Thanos.cs {cs:line-numbers} [Implementation]
<<< ../../../src/cookbook/Thanos/Thanos.output.txt{txt} [Output]
:::
