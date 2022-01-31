# Developing DeBroglie


DeBroglie has grown quite large over the years, and hasn't really been written to optimize legibility. This page serves as an introduction to how it works internally. If you are just interested in the WaveFunctionCollapse algorithm, there are probably easier examples to learn from.

## Abstractions

In order to support so many features overlapping features, DeBroglie introduces some key abstractions. You must understand these, or else the core code is quite hard to follow.

### Topology

DeBroglie supports square grids, hex grids and other more obscure grids. Collectively, these are handled by an [ITopology](xref:DeBroglie.Topo.ITopology) interface. 
ITopology treats each grid as a graph, with each cell corresponding to an integer index. The edges between nodes in the graph have labels which controls how the nodes relate to each other: The direction label uniquely identifies which edge is which leading out of a cell, and the `edgeLabel` label is used to control how tiles can connect accross the edge.

All grid logic is handled by calling the topology interface, allowing different grids to be swapped out easily. There's two actual implementations, one used for regular grids, and one for irregular ones.

### Tiles and patterns

The external API works in terms of `Tile` objects, which can contain arbitrary data. But internally, unique integers called `patterns` are used in place of tiles.

For the AdjacentModel, each tile is one-to-one with a pattern.

For OverlappingModel, the relationship is more complicated, and is stored in TileModelMapping. The patterns are constructed so that solving an *adjacent* constraint on patterns is equivalent to solving an overlapping constraint on tiles. The pattern array is differently sized to the tile array to, so the mapping also shows how to convert between them.

This setup  means that core code only needs focus on solving adjacency problems. This transformation is called "constraint binarization".

All code that details with tiles essentially goes through a translation layer to look up the corresponding patterns. Usually this can be done before generation begins, so it has little overhead.

### Rotation

Rotation is handled almost entirely as a pre-processing step, and none of the core code deals with it. More details on the [rotation page](rotation.md). 

Note I think this approach has worked rather poorly in practise, and I wouldn't recommend it.

### Other interfaces

I've experimented with multiple implementations of different parts of the algorithm. This has lead to a number of interfaces allowing the implementations to be easily swapped.

Generally, most interfaces contain an `Init()` method which gives them an opporunity to subscribe for relevant information.


## WaveFunctionCollapse

At it's heart, DeBroglie is derived the WaveFunctionCollapse algorithm and the similar Model Synthesis algorithm.

This is just a fancy way of saying it's a constraint solver, but it is optimised finding *random* solutions. More explanation [here](https://www.boristhebrave.com/2020/04/13/wave-function-collapse-explained/).

The core algorithm is in the `DeBroglie.Wfc` namespace. Due to the abstractions mentioned above, it's relatively simple code as many of the details have been simplified away via pre-processing.

The default propagator is ArcConsistency4, and is implemented in Ac4PatternModelConstraint. AC4 stores, for every index/pattern/direction, a count of valid patterns that it can connect to. Every change to the domain updates these counts, and when one drops to 0 it indicates that the pattern has become impossible.

The domains of each variable are stored in the `Wave` class.

## Backtracking

A key feature of DeBroglie is backtracking - the ability to undo choices if they lead to a contradiction.

This is handled by keeping a precise log of every change to the `Wave` (the domains). This is essentially a list of which patterns have been banned from which cells.

When we need to backtrack, this log can be read in reverse, and opposite actions taken to undo state. Objects can use other backtracking techniques (such as trailing), but generally they rely on the logs.

## Constraints

While the key constraint in WFC is the "model", which controls how tiles relate to each other locally, DeBroglie has support for adding more constraints, by implementing `ITileConstraint`.

Note, all constraints are "global", DeBroglie has no visibility which constraints depend on which cells.

Constraints can be stateful, though most are not. Writing stateful constraints requires care to work with backtracking as described above.

During every step of evaluation, constraints will be called to `Check` the state of the generation. In this method:
* Constraints inspect the `TilePropagator` to see what progress has been made.
  * They can also inspect any trackers they've subscribed to. This also monitors the state of the TilePropagator, but is usually more efficient.
* If the constraint determines that some tiles cannot be placed at a given location, it calls `Ban` on TilePropagator.
  * Similarly, if it determines a tile *must* be placed, it calls `Select`.
* If the constraint determines that no progress is possible, it can call `SetContradiction` on TilePropagator.

Select/Ban/SetContradiction will guide the generator to only output results consistent with the generator. It's generally best to avoid SetContradiction, as the generator has no choice but to give up or backtrack.


### Example
As a worked example, consider the CountConstraint. It wants to ensure that there are *at most* 10 instances of a given tile X in the final generation.

The simplest possible implementation would iterate over every cell, and ask the TilePropagator if it has been selected as X. If there are more than 10, it calls SetContradiction.

We can improve this. Suppose the current count of X tiles is exactly 10, and we are midway through generation. Then placing any more would be a problem. That means we should Ban X everywhere it's not already been placed, to tell the generator not to pick it any more. So the constraint should count the tiles, and do this if the count is 10, and SetContradction if the count is greater than 10.

Another improvement would be to avoid recounting every cell every time Check is called. It's likely that most of them haven't changed. There are several trackers that can supply a list of changed cells. The actual implementation in DeBroglie, uses the SelectedChangeTracker which can fire a callback every an X tile is selected. We can then keep a running count of X tiles, without having to recount.

