---
uid: config_files
title: Using the Command Line
---
Using the Command Line
========================

As well as operating as a C# library, DeBroglie comes with an executable, `DeBroglie.Console.exe` for users who do not use C#, or wish to quickly prototype configuration. The console application is windows only at the moment.

To use it, you construct a config file in [JSON](https://en.wikipedia.org/wiki/JSON) format, and then invoke the executable on it, by dragging the file onto the .exe, or by running it from the command line:

```
./DeBroglie.Console.exe myconfig.json
```

You can find many example json files in the [samples directory](https://github.com/BorisTheBrave/DeBroglie/tree/master/samples) included with the library.

File format
-----------

The file format very closely resembles the library API described in the main docs. In addition, there are many samples supplied with DeBroglie to give you an idea how to use it. You are advised to familiarize yourself with [JSON](https://en.wikipedia.org/wiki/JSON) before proceeding.

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
| `src`                | string | The file to load as the initial sample. Only used for sample based formats. See [Supported Formats](#supported-formats). |
| `dest`               | string | The file to write the result to. The file has the same format as `src`. Required. |
| `baseDirectory`      | string | The directory that `src` and `dest` are relative to. <br/>`baseDirectory` itself is relative to the directory of the config file, and defaults to that directory.|
| `model`              | [Model](#model-config) | Specifies the [model](features.md#models) to use. Defaults to adjacent. |
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
|`ground`              |[Tile](#tile-references)|Shorthand for adding a pair of [border constraints](constraints.md#border). <br/> The first one constrains the bottom of the output to be the specified tile.<br/> The second bans the tile from all other locations.<br/> The bottom is taken to be ymax for 2d generation, zmin for 3d.|
|`symmetry`            |int|Shorthand for setting `reflectionalSymmetry` and `rotationalSymmetry`. <br/> If even, reflections are on, and rotations is half `symmetry`. <br/>Otherwise reflections are off and rotations are equal to `symmetry`|
|`reflectionalSymmetry`|bool|If set, extra copies of the `src` are used as samples, as described in [Rotation](xref:rotation_article)|
|`rotationalSymmetry`  |int|If set, extra copies of the `src` are used as samples, as described in [Rotation](xref:rotation_article)|
|`backtrack`           |bool|Specifies if [backtracking](features.md#backtracking) is enabled.|
|`animate`             |bool|Dumps snapshots of the output while the generation process is running.|
|`autoAdjacency`       |bool|Enables [auto adjacency detection](adjacency.md#auto-adjacency).|
|`autoAdjacencyTolerance`|double|Value between 0 and 1 indicating how close a match tiles have to be to be considered automatically adjacent.|
|`tiles`               |array of [TileData](#tile-data-config)|Specifies various per-tile information.|
|`adjacencies`         |array of [Adjacency](#adjacency-config)|Indicates which tiles can be adjacent to which other ones ([adjacent model only](features.md#adjacent)).|
|`constraints`         |array of [Constraint](#constraint-config)|Specifies constraints to add.|

### Model Config

Models are a JSON object taking one of the following formats. The `type` field is set to a constant to indicate what sort of model is used.

For constructing an [adjacent model](features.md#adjacent)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"adjacent"`|

For constructing an [overlapping model](features.md#overlapping)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"overlapping"`|
|`n`|int| Shorthand for setting `nX`, `nY` and `nZ`|
|`nX`|int| Size of the rectangles to sample along the x-axis. Default 2.|
|`nY`|int| Size of the rectangles to sample along the y-axis. Default 2.|
|`nZ`|int| Size of the rectangles to sample along the z-axis. Default 2. Ignored for 2d topologies.|

### Constraint Config

Constraints are a JSON object taking one of the following formats. The `type` field is set to a constant to indicate what sort of model is used.

For constructing a [path constraint](constraints.md#path)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"path"`|
|`tile`|[Tile](#tile-references)|Sorthand for setting `tiles` with a single value|
|`tiles`|array of [Tile](#tile-references)| The set of tiles that are considered "on the path".|
|`endPoints`|array of [Point](#point-config)| Set of points that must be connected by paths. If unset, then all path cells must be are connected.|

For constructing an [edged path constraint](constraints.md#edged-path)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"edgedPath"`|
|`exits`|dictionary from [Tile](#tile-references) to array of `Directions` (`"xplus"`, `"yminus"` etc)| The set of tiles that are considered "on the path", and which directions out of those tiles are path connections.|
|`endPoints`|array of [Point](#point-config)| Set of points that must be connected by paths. If unset, then all path cells must be are connected.|

For constructing a [border constraint](constraints.md#border)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"border"`|
|`tile`|[Tile](#tile-references)|Sorthand for setting `tiles` with a single value|
|`tiles`|array of [Tile](#tile-references)| The tiles to select or ban fromthe  border area. |
|`sides`|string|A comma separated list of the values `"xmin"`, `"xmax"`, `"ymin"`, `"ymax"`, `"zmin"`, `"zmax"`, specifying which sides of the output are affected by the constraint. Defaults to all of them (except zmin/zmax for 2d).|
|`excludeSides`|string| Same format as `sides`, these locations are subtracted from the ones specified in `sides`. Defaults to empty.|
|`invertArea`|bool| Inverts the area specified by `sides` and `excludeSides` |
|`ban`|bool| If true, ban `tile` from the area. Otherwise, select it (i.e. ban every other tile). |

For constructing a [fixed tile constraint](constraints.md#fixed-tile)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"fixedTile"`|
|`tile`|[Tile](#tile-references)|Sorthand for setting `tiles` with a single value|
|`tiles`|array of [Tile](#tile-references)| The tiles to select. |
|`point`|[Point](#point-config)|The location to select the tile at. If not specified, the location is chosen randomly.|

For constructing a [max consecutive constraint](constraints.md#max-consecutive)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"maxConsectuive"`|
|`tile`|[Tile](#tile-references)|Sorthand for setting `tiles` with a single value|
|`tiles`|array of [Tile](#tile-references)| The set of tiles to restrict|
|`maxCount`|int|The maximum number of tiles to allow to appear consecutive. Default 3.|
|`axes`|array of Axis (i.e. `"x"`, `"y"`, `"z"`)|Which axes should be restricted. Default: all axes|

For constructing a [mirror constraint](constraints.md#mirror)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"mirror"`|


### Point Config

The Point class is used for specifying a location in the input or output.

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`x`|int| |
|`y`|int| |
|`z`|int| Optional, defaults to 0.|

### Tile Data Config

There are several rotation/reflection based fields, see the [Rotation section](xref:rotation_article) for details on how they work.


| Field                | Type           | Description  |
| -------------------- |---------------|-------|
| `value` | [Tile](#tile-references) | Specifies which tile this object is configurating. `value` is interpreted differently for different [file formats](#tile-references).<br/> Specifically, for sample based formats, it is a reference to tiles occuring in the sample. For file set based formats, it's just a unique name for the tile. <br/> Required. |
| `src` | string | When using a [file set](#import--export-file-formats), indicates which file contains image data for the tile. |
| `multiplyFrequency` | number | Scales the frequency of the configured tile, using [MultiplyFrequency](xref:DeBroglie.Models.TileModel.MultiplyFrequency(DeBroglie.Tile,System.Double)) |
| `tileSymmetry`| string | [See here](rotation.md#tile-symmetries) |
| `reflectX`| [Tile](#tile-references)| Gives the tile you get if you reflect the configured tile in the x-axis.|
| `reflectY`| [Tile](#tile-references)| Gives the tile you get if you reflect the configured tile in the y-axis.|
| `rotateCw`| [Tile](#tile-references)| Gives the tile you get if you rotate the configured tile clockwise  in the xy-plane.|
| `rotateCcw`| [Tile](#tile-references)| Gives the tile you get if you rotate the configured tile counter clockwise in the xy-plane.|

### Tile References

Various parts of the config expect a reference to a tile. When using file sets, tile references are easy - they are just what you entered in the `value` field for the tile in the `tiles` array. But when working with samples you need to refer to the tiles that the samples use, which is done in a predefined way described below.

Tile references can either be the name of a tile, or the value. Names and values of tiles depend on what sample file format is being used.

**Bitmap** - Tile values are colors, and can be expressed as a HTML style hex code, e.g. `"#FF0000"`.

**Tiled** - Tile values are integer "global ids" e.g. `1`. Using zero means the empty tile. If you set a [custom property](http://docs.mapeditor.org/en/stable/manual/custom-properties/) called `name` on a tile in the tileset, you can also use that to reference a tile.

**MagicaVoxel** - Tile values are palette indices (a value between 0 and 255). Using zero means the empty voxel.

### Adjacency Config

Adjacency config is a way of configuring the [adjacent model](features.md#adjacent) without using sample inputs. You set an array of adjacency entries,
where each entry adds extra permissible neighbours for some tiles in some directions. Further details can be found on the [Adjacency page](adjacency.md).

Each adjacency entry it composed of two named lists of [Tile references](#tile-references). Specifically, they take one of three forms:

```javascript
{"left": [...], "right": [...] }
{"up": [...], "down": [...] }
{"above": [...], "below": [...] }
```

For `left`/`right`, this means that it is legal to place any tile found on the `left` list to the left of any tile in the `right` list. 
Similarly `up`/`down` are along the y-axis and `above`/`below` are on the z-axis.

A tile must be listed at least once in every relevant direction, or else it'll have no possible neighbours in some direction, and the generation will never include it (except maybe on the edge, where it doesn't need a neighbour).

As documented in [AddAdjacency](xref:DeBroglie.Models.AdjacentModel.AddAdjacency(System.Collections.Generic.IList{DeBroglie.Tile},System.Collections.Generic.IList{DeBroglie.Tile},System.Int32,System.Int32,System.Int32,DeBroglie.Rot.TileRotation)), if there are rotations specified for tiles, then adjacencies are added for the rotated pairs as appropriate.


Import / Export File formats
------------

DeBroglie supports reading and writing to a wide variety of formats. There are two sorts of inputs: **samples**, which are single files containing an arrangment of tiles, and **file sets** which are a collection of files, one per tile.

Samples are specified by setting the `src` property in the config. File sets are specified by setting the `src` property for each tile in the `tiles` array. When using file sets, there is no data for the model to learn from, so you must manually specify the model as documented in [Adjacency](#adjacency-config).

### Supported formats

Not all formats can be converted between. You must pick file extensions for
`src` and `dest` fields to match the following table.

<table>
<tr><td></td><td colspan="6">Inputs</td></tr>
<tr><td></td><td colspan="4">Samples</td><td colspan="2">File sets</td></tr>
<tr><td></td><td>Bitmap (.png)</td><td>Tiled Map(.tmx)</td><td>Tiled Tileset (.tsx)</td><td>MagicaVoxel (.vox)</td><td>File set of Bitmap (.png)</td><td>File set of MagicaVoxel (.vox)</td></tr>
<tr><td>Outputs</td><td></td><td></td><td></td><td></td><td></td><td></td></tr>
<tr><td>.csv</td><td>&#x2713;</td><td>&#x2713;</td><td>&#x2713;</td><td>&#x2713;</td><td>&#x2713;</td><td>&#x2713;</td></tr>
<tr><td>.png</td><td>&#x2713;</td><td>&#x2713;</td><td>&#x2713;</td><td></td><td>&#x2713;</td><td></td></tr>
<tr><td>.tmx</td><td></td><td>&#x2713;</td><td>&#x2713;</td><td></td><td></td><td></td></tr>
<tr><td>.vox</td><td></td><td></td><td></td><td>&#x2713;</td><td></td><td>&#x2713;</td></tr>
</table>

### Format details

Most formats have special properties and limitations, those are listed below.

#### Csv

This is a very basic export format.

Values are delimited with commas along the x-axis, and then new lines along the y-axis.
If 3d, then the plane z=0 is dumped first, then a new line, then z=1 and so on.

#### Bitmap

When loading bitmap samples, each pixel is assumed to be one tile, with the color being the tile value. If you want each tile to have it's own bitmap, you must use a file set of bitmaps by specifying a `src` value for each tile in the `tiles` array.

Additionally, the `animate` setting behaves differently for bitmaps. Uncertain tiles will be drawn as a color blend of all possible tiles.

#### Tiled

[Tiled](https://www.mapeditor.org/) .tmx files are supported, though Tiled has many features that are ignored. Both square and hex grids can be read. If there are multiple layers in a square grid, then they are taken to be a 3d topology.

You can also load .tsx files (Tiled tilesets). As these do not come with a map to use as sample input, you must set [adjacencies](#adjacency-config) instead.

Tile rotations in Tiled are supported in DeBroglie. They are treated like "generated" tiles as described on the [Rotation page](rotation.md).

#### MagicaVoxel

[MagicaVoxel](http://magicavoxel.net/) .vox files are supported. As a sample, each voxel becomes one tile. If you want each tile to have it's own cube of voxels, you must use a file set of voxels by specifying a `src` value for each tile in the `tiles` array.

You must abide to the limits of the format when using this, specifically there can only be 255 different tiles (plus the empty tile), and the maximum dimension generated is a cube of size 126.
