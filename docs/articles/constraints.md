---
uid: constraints
title: Constraints
---

# Constraints

Constraints are a way to make additional hard requirements about the generated output.
Unlike models, constraints can be *non-local*, meaning they force some property of the entire output,
not just within a small rectangles.

You can have as many constraints as you like during generation, however with multiple constraints it 
can be hard for the WFC to find a valid solution. Turning on backtracking can aleviate this.

Each constraint is for enforcing a different effect. The available constraints are discussed below.

## Border

<xref:DeBroglie.Constraints.BorderConstraint> class restricts what tiles can be selected in various regions of the output. It's pretty common that you want to specify the borders as being ground, or empty, or whatever, as otherwise if DeBroglie will often generate structures that lead off the edge and are clipped.

> [!NOTE]
> <xref:DeBroglie.Constraints.BorderConstraint> only affects the initial set of tiles that can be legally placed. That means it is not doing anything except calling [Ban](xref:DeBroglie.TilePropagator.Ban(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) and [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) on startup, which you can also do manually. <xref:DeBroglie.Constraints.BorderConstraint> is just a convenience.

BorderConstraing specifies a set of cells using a simple logic. First, an inclusion set of cells is defined by the <xref:DeBroglie.Constraints.BorderConstraint.Sides> field. This field is a bit field of flags, where there is one flag for each of the boundary sides of the output area (4 in 2d, 6 in 3d). An exclusion set is defined similarly from the  <xref:DeBroglie.Constraints.BorderConstraint.ExcludeSides> field. To get the set of locations affected, subtract the exclusion set from the inclusion set, then optionally invert if <xref:DeBroglie.Constraints.BorderConstraint.InvertArea> is set.

For each affected location, BorderConstratin calls [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) with the Tile specified. If the <xref:DeBroglie.Constraints.BorderConstraint.Ban> field is set, then it calls [Ban](xref:DeBroglie.TilePropagator.Ban(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) isntead of [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)).

**Example**

<figure>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/docs/pathway_overlapping_border.json">
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_overlapping_border.png"/>
</a>
<figcaption>Using a border constraint ensures that none of the blue leaves the edge, forcing loops.</figcaption>
</figure>


## Path

The <xref:DeBroglie.Constraints.PathConstraint> checks that it is possible to connect several locations together via a continuous path of adjacent tiles. It does this by banning any tile placement that would make such a path impossible.

Set <xref:DeBroglie.Constraints.PathConstraint.Tiles> to the set of tiles that are considered on the path. Any two adjacent locations with tiles in this set are connected, and if x is connected to y and y is connected to z, then x and z are also connected.

By default, <xref:DeBroglie.Constraints.PathConstraint> forces all path tiles to be connect to each others. However, if you set <xref:DeBroglie.Constraints.PathConstraint.EndPoints> then instead it forces that those specific points connect to each other, but doesn't stop extra path tiles being placed.

> [!WARNING]
> <xref:DeBroglie.Constraints.PathConstraint> does not have a great deal of lookahead, so adding it will significantly increase the amount of retries needed to get a successful generation. You may need to enable [backtracking](features.md#backtracking) to get a successful result.

**Example**

<figure>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/docs/pathway_overlapping_path.json">
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_overlapping_path.png"/>
</a>
<figcaption>Using a path constraint ensures you can trace a path from any blue pixel to any other one.</figcaption>
</figure>

## Edged Path

The <xref:DeBroglie.Constraints.EdgedPathConstraint> is a more advanced variant of the <xref:DeBroglie.Constraints.PathConstraint>.
The normal path constraint records which tiles can part of the path, and finds a path consisting of adjacent tiles.
The edged path constraint is more restrictive. Not only do the tiles have to be placed adjacent, but their edges must match. This is configured by setting a set of "exits" for each tile. Other than this restriction, the constraint works identically.

For example consider placing the tiles <img src="../images/grass_corner3.png"> and <img src="../images/grass_corner4.png"> together. Both tiles have a picture of a path on them, but the path exits the tiles only on certain edges. <span><img src="../images/grass_corner3.png"><img src="../images/grass_corner4.png"></span> means they are connected, but <span><img src="../images/grass_corner4.png"><img src="../images/grass_corner3.png"></span> is not. We can use Edged Path Constraint to specify this, the normal path constraint isn't specific enough.

**Example**

<figure>
<img src="../images/edged_path_constraint_missing.png"/>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/grass/edged_path_constraint.json">
<img src="../images/edged_path_constraint.png"/>
</a>
<figcaption>Generating dirt paths before and adding EdgedPathConstraint.<br/>
The normal PathConstraint cannot cope with path tiles so close together.</figcaption>
</figure>


## Fixed Tile

The <xref:DeBroglie.Constraints.FixedTileConstraint> class forces a given location to be the specified Tile at initialization. If you don't specify a location, a random legal location is chosen.

You can use <xref:DeBroglie.Constraints.FixedTileConstraint> to force certain features to always be generated.

**Example**

<figure>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/castle/castle.json">
<video autoplay loop muted>
  <source src="../images/castle_fixed.webm" type="video/webm">
</video>
</a>
<figcaption>Several castles all generated with a fixed turret location</figcaption>
</figure>

## Max Consecutive

The <xref:DeBroglie.Constraints.MaxConsecutiveConstraint> class prevents more than a certain number of tiles appearing consecutively along the x, y or z axis.

<figure>
<table>
<tr>
<td>
MaxCount
</td>
<td></td>
</tr>
<tr>
<td>2</td>
<td>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/docs/max_constraint_2.json">
<img src="../images/max_constraint_2.png" />
</a>
</td>
</tr>
<tr>
<td>3</td>
<td>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/docs/max_constraint_3.json">
<img src="../images/max_constraint_3.png" />
</a>
</td>
</tr>
<tr>
<td>10</td>
<td>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/docs/max_constraint_10.json">
<img src="../images/max_constraint_10.png" />
</a>
</td>
</tr>
<figcaption>Platforms generated with the MaxConsecutive constraint with different values of MaxCount.</figcaption>
</figure>

## Mirror

The <xref:DeBroglie.Constraints.MirrorConstraint> class forces the generated output to be symetric about the x-axis.

**Example**

<figure>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/grass/mirror_constraint.json">
<img src="../images/mirror_constraint.png"/>
</a>
</figure>

## Count

The <xref:DeBroglie.Constraints.CountConstraint> class forces the number of a given tile or set of tiles to be at most or at least a given number.


## Custom Constraints

You can define your own constraints by extending <xref:DeBroglie.Constraints.ITileConstraint>. The Init method is called once per propagator run, and the Check method is called after each step, each time tiles are selected. 

Inside these methods, you can call [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) and [Ban](xref:DeBroglie.TilePropagator.Ban(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) to control what tiles can be legally placed. You can also call <xref:DeBroglie.TilePropagator.SetContradiction> to indicate that something is wrong and generation cannot continue. This will cause the propagator to give up, or backtrack, according to settings.
