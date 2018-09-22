Introduction
=====================================

DeBroglie is a C# library implementing the [Wave Function Collapse](https://github.com/mxgmn/WaveFunctionCollapse) algorithm with support for additional non-local constraints, and other useful features.

Wave Function Collapse (WFC) is an constraint-based algorithm for which takes a small input image or tilemap
and procedurally generating a larger image in the same style, such as:

<figure>
<img src="../images/city_input.png">
<img src="../images/arrow.png"/>
<img src="../images/city_output.png">
</figure>
 
DeBroglie is stocked with loads of features to help customize the generation process.

TODO: Motivating examples:



Feature Overview
--------

* ["Overlapped"](#overlapping) model implementation of WFC
* [Non-local constraints](#constraints) allow you to specify other desired properties of the result
* [Backtracking](#backtracking) support - other WFC implementations immediately give up when a contradiction occurs.
* [supports 2d tiles, hex grids, and 3d voxels](#topology) 

What is WFC?
------------

Wave Function Collapse is a constraint based algorithm that generates bitmaps, tilemaps etc one tile at a time, based off a sample image.

<video src="../images/pathway.webm" autoplay loop></video>

The original author of WCF has an excellent [explanation of the core algorithm](https://github.com/mxgmn/WaveFunctionCollapse)

DeBroglie uses the core idea mostly unchanged, though enhanced in various ways explained in [Features](#features).

Usage
---------------

To use DeBroglie, you start with a simple image or tilemap you want to generalize.

Then, select one of the [models](#models) that controls the generation process. 
There's lot of [features](#features) that can be applied at this point.

The last detail needed is the size of the output image desired.

Then you run a propagator that will generate the output one tile at a time. 
Depending on the difficulty of the generation, the process can fail and require restarting.

Quick Start (C#)
=================

Right now, DeBroglie isn't in NuGet. The easiest way to get going is to git clone the repo, and include the DeBroglie project in your solution.

Then here's a simple that constructs the relevant objects and runs them.

```csharp
// Define some sample data
ITopoArray<char> sample = TopoArray.Create(new[]
{
    new[]{ '_', '_', '_'},
    new[]{ '_', '*', '_'},
    new[]{ '_', '_', '_'},
}, periodic: false);
// Specify the model used for generation
var model = new AdjacentModel(sample.ToTiles());
// Set the output dimensions
var topology = new Topology(10, 10, periodic: false);
// Acturally run the algorithm
var propagator = new TilePropagator(model, topology);
var status = propagator.Run();
if (status != Resolution.Decided) throw new Exception("Undecided");
var output = propagator.ToValueArray<char>();
// Display the results
for (var y = 0; y < 10; y++)
{
    for (var x = 0; x < 10; x++)
    {
        System.Console.Write(output.Get(x, y));
    }
    System.Console.WriteLine();
}
```

Quick Start (Command Line)
==========================

Right now, the binary isn't published, so you must download the source and compile it.

```json
{
    TODO
}
```

Key Concepts
============

**2d/3d** - DeBroglie works for both 2d and 3d generation, by selecting an appropriate <xref:DeBroglie.Topo.Topology>. Generally, most APIs accept `(x,y,z)` co-ordinates - the `z` value should just be 0 if you are working in 2d.

**<xref:DeBroglie.Tile>** - The individual units the generation algorithm works with. Tiles wrap a value of any type, but they are usually an integer index into a tileset, or a @System.Drawing.Color when working with bitmaps. The value isn't important, all relevant information about a tile is stored externally.

**<xref:DeBroglie.Topo.Topology>** - Specifies an area or volume of [space](https://en.wikipedia.org/wiki/Discrete_space) and how to navigate it. There's more detail in the [topology section](#topology).

**<xref:DeBroglie.Topo.ITopoArray`1>** - A 2d or 3d read-only array with one entry per space in the corresponding @DeBroglie.Topo.Topology. They are used as both the input and output format for the library. You can construct these with methods on @DeBroglie.Topo.TopoArray. ITopoArray objects can also have a mask associated with them, indicating missing values.

**[Model](xref:DeBroglie.Models.TileModel)** - A model specifies constraints between nearby tiles in the generated output. See [models](#models) for more info.

**[Propagator](xref:DeBroglie.TilePropagator)** - A propogator is responsible for generating an output that satifies the constraints specified by the [model](xref:DeBroglie.Models.TileModel) and other [constraints](xref:DeBroglie.Constraints.ITileConstraint).


Features
========

Models
------
Models are the key way to control the generation process in DeBroglie. They specify what combinations of tiles are legal to place near each other.

Models have only have a few parameters - most information is inferred by giving them samples tilemaps.

### Adjacent

<xref:DeBroglie.Models.AdjacentModel> constrains which tiles can be placed adjacent to which other ones. It does so by maintaining for each tile, 
a list of tiles that can be placed next to it in each direction. The list is always symmetric, i.e. if it is legal to place tile B directly above tile A, 
then it is legal to place A directly below B.

Adding a sample to an adjacent model adds all adjacent tile pairs in the sample into the legal adjacency lists. 

The adjacenct model is very "loose" - it doesn't constrain the choice of tiles as much as the overlapping model. This makes it a good choice
when the relationship between tiles is very complex, or you are adding a lot other [constraints](#constraints) directly.

**Example**

<figure>
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_adjacent.png"/>
<figcaption><a href="xref:DeBroglie.Models.AdjacentModel">AdjacentModel</a> can see that blue and green are never adjacent, but otherwise doesn't resemble the sample closely</figcaption>
</figure>

### Overlapping

<xref:DeBroglie.Models.OverlappingModel> constrains that every `n` by `n` rectangle in the output is a copy of a rectangle taken from the sample (potentially with rotation / reflection).

<img src="https://camo.githubusercontent.com/c9a04da9ed7815de79b3f2236cd32d3e2dffc68f/687474703a2f2f692e696d6775722e636f6d2f4b554c475838362e706e67"/>

The model has three parametrs, `nx`, `ny` and `nz` which specify the dimensions of the rectangle/cuboid in the output. For convenience, you can just specify a value for `n` that sets all three. Typically `n` is only 2 or 3 - any larger and the algorithm can run quite slow and becomes increasingly unlikely to find a result.

Compared to the adjacent model, the overlapping model is quite strict. This means it typically needs a larger amount of sample input to get good results, but when it does work, it can accurately reproduce many features of the samples that the adjacent model will simply scramble.

In particular, the overlapping model can detect corners, lines and junctions. In conjunction with the propogation by the Wave Function Collapse algorithm, this means that rooms and pathways will get detected and output, but with variations on the placement, size and direction.

**Example**

<figure>
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_overlapping_2.png"/>
<figcaption><a href="xref:DeBroglie.Models.OverlappingModel">OverlappingModel</a> with <tt>n</tt> = 2</figcaption>
</figure>

<figure>
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_overlapping.png"/>
<figcaption><a href="xref:DeBroglie.Models.OverlappingModel">OverlappingModel</a> with <tt>n</tt> = 3</figcaption>
</figure>

### Other model functionaltiy

As mentioned, models also track the frequency of tiles in the sample image. You can make changes to this by calling [MultiplyFrequency](xref:DeBroglie.Models.TileModel.MultiplyFrequency(DeBroglie.Tile,System.Double)).

<figure>
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_overlapping_high_freq.png"/>
<figcaption>Same example as overlapping model with frequency of green boosted.</figcaption>
</figure>

<figure>
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_overlapping_low_freq.png"/>
<figcaption>Same example as overlapping model with frequency of green reduced.</figcaption>
</figure>

Constraints
-----------

Constraints are a way to make additional hard requirements about the generated output.
Unlike models, constraints can be *non-local*, meaning they force some property of the entire image,
not just within a small rectangles. 

### Border

<xref:DeBroglie.Constraints.BorderConstraint> class restricts what tiles can be selected in various regions of the output. It's pretty common that you want to specify the borders as being ground, or empty, or whatever, as otherwise if DeBroglie will often generate structures that lead off the edge and are clipped.

> [!NOTE]
> <xref:DeBroglie.Constraints.BorderConstraint> only affects the initial set of tiles that can be legally placed. That means it is not doing anything except calling [Ban](xref:DeBroglie.TilePropagator.Ban(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) and [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) on startup, which you can also do manually. <xref:DeBroglie.Constraints.BorderConstraint> is just a convenience.

BorderConstraing specifies a set of cells using a simple logic. First, an inclusion set of cells is defined by the <xref:DeBroglie.Constraints.BorderConstraint.Sides> field. This field is a bit field of flags, where there is one flag for each of the boundary sides of the output area (4 in 2d, 6 in 3d). An exclusion set is defined similarly from the  <xref:DeBroglie.Constraints.BorderConstraint.ExcludeSides> field. To get the set of locations affected, subtract the exclusion set from the inclusion set, then optionally invert if <xref:DeBroglie.Constraints.BorderConstraint.InvertArea> is set.

For each affected location, BorderConstratin calls [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) with the Tile specified. If the <xref:DeBroglie.Constraints.BorderConstraint.Ban> field is set, then it calls [Ban](xref:DeBroglie.TilePropagator.Ban(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) isntead of [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)).

**Example**

<figure>
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_overlapping_border.png"/>
<figcaption>Using a border constraint ensures that none of the blue leaves the edge, forcing loops.</figcaption>
</figure>

### Path

The <xref:DeBroglie.Constraints.PathConstraint> checks that it is possible to connect several locations together via a continuous path of adjacent tiles. It does this by banning any tile placement that would make such a path impossible.

Set <xref:DeBroglie.Constraints.PathConstraint.PathTiles> to the set of tiles that are considered on the path. Any two adjacent locations with tiles in this set are connected, and if x is connected to y and y is connected to z, then x and z are also connected.

By default, <xref:DeBroglie.Constraints.PathConstraint> forces all path tiles to be connect to each others. However, if you set <xref:DeBroglie.Constraints.PathConstraint.EndPoints> then instead it forces that those specific points connect to each other, but doesn't stop extra path tiles being placed.

> [!WARNING]
> <xref:DeBroglie.Constraints.PathConstraint> does not have a great deal of lookahead, so adding it will significantly increase the amount of retries needed to get a successful generation. You may need to enable [backtracking](#backtracking) to get a successful result.

**Example**

<figure>
<img src="../images/pathway.png"/>
<img src="../images/arrow.png"/>
<img src="../images/pathway_overlapping_path.png"/>
<figcaption>Using a path constraint ensures you can trace a path from any blue pixel to any other one.</figcaption>
</figure>

### Custom Constraints

You can define your own constraints by extending <xref:DeBroglie.Constraints.ITileConstraint>. The Init method is called once per propagator run, and the Check method is called after each step, each time tiles are selected. 

Inside these methods, you can call [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) and [Ban](xref:DeBroglie.TilePropagator.Ban(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) to control what tiles can be legally placed. You can also return <xref:DeBroglie.Resolution.Contradiction> to indicate that something is wrong and generation cannot continue. Otherwise, return <xref:DeBroglie.Resolution.Undecided> to indicate that generation should continue.

Backtracking
------------

By default when you call <xref:DeBroglie.TilePropagator.Run> the WCF algorithm keeps adding tiles until it has filled every location, or until it is impossible to place a tile that satisfies all the constraints set up. It then returns <xref:DeBroglie.Resolution.Contradiction>.

If you set the backtrack argument to `true` when constructing the <xref:DeBroglie.TilePropagator>, instead, when a contradiction occurs it does not give up. Instead it rolls back the most recent tile placement, and tries another placment instead. [In this manner](https://en.wikipedia.org/wiki/Backtracking), it can explore the entire space of possible tile placements, seeking one that satifies the constraints. <xref:DeBroglie.Resolution.Contradiction> is only returned if all possibilities have been exhausted.

Backtracking is very powerful and general, and can solve extremely difficult layouts. However, it can be quite slow, and consumes a great deal of memory, so it is generally only appropriate for generating small arrays.

Topology
--------

The most common case of using DeBroglie is to generate 2d images and tile maps, however, that is not all that can be generated.

<figure>
<img src="../images/columns_in.png"/>
<img src="../images/arrow.png"/>
<img src="../images/columns_out.png"/>
<figcaption>Example of 3d generation. Rendered with <a href="http://magicavoxel.net">MagicaVoxel</a></figcaption>
</figure>


<figure>
<img src="../images/hexmini_in.png"/>
<img src="../images/arrow.png"/>
<img src="../images/hexmini_out.png"/>
<figcaption>Example of hex generation. Rendered with <a href="https://www.mapeditor.org">Tiled</a></figcaption>
</figure>

DeBroglie uses a mechanism called <xref:DeBroglie.Topo.Topology> to specify the type of area or volume to generate, what size it is, and whether it should wrap around at the edges (i.e. is it *periodic*). Topologies do not actually store data, they just specify the dimensions. Actual data is stored in an <xref:DeBroglie.Topo.ITopoArray`1>.

Currently, three types of topology are supported: 2d square grid, 2d hex grid, and 3d cube grid. 

The topology of the generated result is inferred from the input samples. When using the command line tool the topology of the input is based on the file being read. But as a C# library, samples are passed as <xref:DeBroglie.Topo.ITopoArray`1> objects.

So you must directly call the <xref:DeBroglie.Topo.Topology> constructor, and then create <xref:DeBroglie.Topo.ITopoArray`1> objects using the methods on <xref:DeBroglie.Topo.TopoArray>.  There's also many shortcut methods that don't require a topology if you just want to work with square grids.

### Hexagonal Topology

Hexagonal topologies use a convention of "pointy side up". The x-axis moves to the right, and the y-axis moves down and to the left. This means the library generates rhombus shaped output. Additionally, periodic input / output is not supported.

Using the [Tiled format](https://www.mapeditor.org/) for import/export of hexagonal tilemaps is recommended, as most software doesn't have support for hexagons. DeBroglie comes with <xref:DeBroglie.TiledUtil> to facilitate converting between <xref:DeBroglie.Topo.ITopoArray`1> objects and Tiled maps.

Rotation
--------

Handling rotation of the input sample is a complex topic, and is discussed [in a separate article](xref:rotation_article).


Using the Command Line
========================

As well as operating as a C# library, DeBroglie comes with an executable, `DeBroglie.Console.exe` for users who do not use C#, or wish to quickly prototype configuration. The console application is windows only at the moment.

To use it, you construct a config file in [JSON](https://en.wikipedia.org/wiki/JSON) format, and then invoke the executable on it, by dragging the file onto the .exe, or by running it from the command line:

```
./DeBroglie.Console.exe myconfig.json
```

File format
-----------

The file format very closely resembles the library API described in the main docs. In addition, there are many samples supplied with DeBroglie to give you an idea how to use it. You are advised to familiarize yourself with [JSON](https://en.wikipedia.org/wiki/JSON) before proceeding.

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
| `src`                | string | The file to load as the initial sample.  See [file formats](#file-formats) for what is supported. Required. |
| `dest`               | string |   The file to write the result to. The file has the same format as `src`. Required. |
| `baseDirectory`      | string |   The directory that `src` and `dest` are relative to. <br/>`baseDirectory` itself is relative to the directory of the config file, and defaults to that directory.|
| `model`              | [Model](#model-config) | Specifies the [model](#models) to use. Defaults to adjacent. |
|`periodicInput`       | bool | Shorthand for setting `periodicInputX`, `periodicInputY`, `periodicInputZ`.|
|`periodicInputX`      | bool | Does the input image wrap around on the x axis?|
|`periodicInputY`      | bool | Does the input image wrap around on the y axis?|
|`periodicInputZ`      | bool | Does the input image wrap around on the z axis?|
| `periodic`           | bool | Shorthand for setting `periodicX`, `periodicY`, `periodicZ`.|
|`periodicX`           | bool | Should the output wrap around on the x axis.|
|`periodicY`           | bool | Should the output wrap around on the y axis.|
|`periodicZ`           | bool | Should the output wrap around on the z axis.|
|`width`               |int| Length of the x-axis in pixels / tiles of the output result. Default 48.|
|`height`              |int| Length of the y-axis in pixels / tiles of the output result. Default 48.|
|`depth`               |int| Length of the z-axis in pixels / tiles of the output result. Default 48.|
|`ground`              |[Tile](#tile-references)|Shorthand for adding a pair of [border constraints](#border). <br/> The first one constrains the bottom of the output to be the specified tile.<br/> The second bans the tile from all other locations.<br/> The bottom is taken to be ymax for 2d generation, zmin for 3d.|
|`symmetry`            |int|Shorthand for setting `reflectionalSymmetry` and `rotationalSymmetry`. <br/> If even, reflections are on, and rotations is half `symmetry`. <br/>Otherwise reflections are off and rotations are equal to `symmetry`|
|`reflectionalSymmetry`|bool|If set, extra copies of the `src` are used as samples, as described in [Rotation](xref:rotation_article)|
|`rotationalSymmetry`  |int|If set, extra copies of the `src` are used as samples, as described in [Rotation](xref:rotation_article)|
|`backtrack`           |bool|Specifies if [backtracking](#backtracking) is enabled.|
|`animate`             |bool|Dumps snapshots of the output while the generation process is running. Experimental.|
|`tiles`               |array of [TileData](#tile-data-config)|Specifies various per-tile information.|
|`constraints`         |array of [Constraint](#constraint-config)|Specifies constraints to add.|

### Model Config

Models are a JSON object taking one of the following formats. The `type` field is set to a constant to indicate what sort of model is used.

For constructing an [adjacent model](#adjacent)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"adjacent"`|

For constructing an [overlapping model](#overlapping)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"overlapping"`|
|`n`|int| Shorthand for setting `nX`, `nY` and `nZ`|
|`nX`|int| Size of the rectangles to sample along the x-axis. Default 2.|
|`nY`|int| Size of the rectangles to sample along the y-axis. Default 2.|
|`nZ`|int| Size of the rectangles to sample along the z-axis. Default 2. Ignored for 2d topologies.|

### Constraint Config

Constraints are a JSON object taking one of the following formats. The `type` field is set to a constant to indicate what sort of model is used.

For constructing a [path constraint](#path)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"path"`|
|`pathTiles`|array of [Tile](#tile-references)| The set of tiles that are considered "on the path".|

For constructing a [border constraint](#border)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"border"`|
|`tile`|[Tile](#tile-references)| The tile to select or ban fromthe  border area. |
|`sides`|string|A comma separated list of the values `"xmin"`, `"xmax"`, `"ymin"`, `"ymax"`, `"zmin"`, `"zmax"`, specifying which sides of the output are affected by the constraint. Defaults to all of them (except zmin/zmax for 2d).|
|`excludeSides`|string| Same format as `sides`, these locations are subtracted from the ones specified in `sides`. Defaults to empty.|
|`invertArea`|bool| Inverts the area specified by `sides` and `excludeSides` |
|`ban`|bool| If true, ban `tile` from the area. Otherwise, select it (i.e. ban every other tile). |


### Tile Data Config

There are several rotation/reflection based fields, see the [Rotation section](xref:rotation_article) for details on how they work.


| Field                | Type           | Description  |
| -------------------- |---------------|-------|
| `value` | [Tile](#tile-references) | Specifies which tile this object is configurating. `value` is interpreted differently for different [file formats](#tile-references). Required. |
| `multiplyFrequency` | number | Scales the frequency of the configured tile, using [MultiplyFrequency](xref:DeBroglie.Models.TileModel.MultiplyFrequency(DeBroglie.Tile,System.Double)) |
| `tileSymmetry`| string | [See here](#tile-symmetry) |
| `reflectX`| [Tile](#tile-references)| Gives the tile you get if you reflect the configured tile in the x-axis.|
| `reflectY`| [Tile](#tile-references)| Gives the tile you get if you reflect the configured tile in the y-axis.|
| `rotateCw`| [Tile](#tile-references)| Gives the tile you get if you rotate the configured tile clockwise  in the xy-plane.|
| `rotateCcw`| [Tile](#tile-references)| Gives the tile you get if you rotate the configured tile counter clockwise in the xy-plane.|
| `noRotate` | bool | Set this to opt out of the default rotation setting that a tile is fully symmetric. This is implicitly set if you set any other rotation options|


### Tile Symmetry

<xref:DeBroglie.TileSymmetry>, is a quick way of saying that a tile is unaffected by some reflections or rotations. It's equivalent to setting the reflect and rotation properties of the tile data to the same value as the tile itself. There several possibilities, each named after a letter that has the same symmetry:

| Letter| Description|
|-------|-------------|
|`F` or `none`| No symmetry. |
|`X` or `full` | Fully symmetric. |
|`T`| Reflectable on y-axis. |
|`I`| Reflectable on x-axis and y-axis. |
|`L`| Reflectable on one diagonal. |
|`\`| Reflectable on both diagonals.|
|`Q`| Reflectable on other diagonal. |
|`E`| Reflectable on x-axis. |
|`N`| Can rotate 180 degrees. |
|`cyclic`| Any rotation, but no reflection. |

### Tile References

Various parts of the config expect a reference to a tile. Tile references can either be the name of a tile, or the value. Names and values of tiles depend on what file format is being used.

**Bitmap** - Tile values are colors, and can be expressed as a HTML style hex code, e.g. `"#FF0000"`.

**Tiled** - Tile values are integer "global ids" e.g. `1`. Using zero means the empty tile. If you set a [custom property](http://docs.mapeditor.org/en/stable/manual/custom-properties/) called `name` on a tile in the tileset, you can also use that to reference a tile.

**MagicaVoxel** - Tile values are palette indices (a value between 0 and 255). Using zero means the empty voxel.

File formats
------------

When specifying the `src` property, you can load files from a number of different sources. `dest` is written to in the same format. In each case DeBroglie tries to handle the format sensibly, which means there are some notes particular to each format.

### Bitmap

When loading bitmaps (or pngs), each pixel is assumed to be one tile, with the color desribing which tile.

Additionally, the `animate` setting behaves differently for bitmaps. Uncertain tiles will be drawn as a color blend of all possible tiles.

### Tiled

[Tiled](https://www.mapeditor.org/) .tmx files are supported, though Tiled has many features that are ignored. Both square and hex grids can be read. If there are multiple layers in a square grid, then they are taken to be a 3d topology.

In my experience, most tilesets are drawn that way anyway.

### MagicaVoxel

[MagicaVoxel](http://magicavoxel.net/) .vox files are supported. Each voxel becomes one tile. You must abide to the limits of the format when using this, specifically there can only be 255 different tiles (plus the empty tile), and the maximum dimension generated is a cube of size 126.

FAQ
===

**Q:** What does the Wave Function Collapse algorithm have to do with the quantum physics concept of [Wave Function Collapse](https://en.wikipedia.org/wiki/Wave_function_collapse)

**A:** Very little really. The original idea was that you have a probability space of possible tile choices similar to how a wave function is a possibility space for particle properties. 
But the comparison is only really skin deep - it's just a cool name.

**Q:** How do you pronounce De Broglie

**A:** "duh broy". [Wikipedia has a pronounciation guide](https://en.wikipedia.org/wiki/Louis_de_Broglie)

**Q:** How do I...

**A:** There are many samples supplied with DeBroglie, try and find one that best matches your goal. I plan to write some articles on the best way to use the library. Contact me if you have something in mind, and I can prioritize writing it up.