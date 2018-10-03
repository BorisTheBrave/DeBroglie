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
| `src`                | string | The file to load as the initial sample.  See [file formats](#file-formats) for what is supported. Required. |
| `dest`               | string |   The file to write the result to. The file has the same format as `src`. Required. |
| `baseDirectory`      | string |   The directory that `src` and `dest` are relative to. <br/>`baseDirectory` itself is relative to the directory of the config file, and defaults to that directory.|
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
|`ground`              |[Tile](#tile-references)|Shorthand for adding a pair of [border constraints](features.md#border). <br/> The first one constrains the bottom of the output to be the specified tile.<br/> The second bans the tile from all other locations.<br/> The bottom is taken to be ymax for 2d generation, zmin for 3d.|
|`symmetry`            |int|Shorthand for setting `reflectionalSymmetry` and `rotationalSymmetry`. <br/> If even, reflections are on, and rotations is half `symmetry`. <br/>Otherwise reflections are off and rotations are equal to `symmetry`|
|`reflectionalSymmetry`|bool|If set, extra copies of the `src` are used as samples, as described in [Rotation](xref:rotation_article)|
|`rotationalSymmetry`  |int|If set, extra copies of the `src` are used as samples, as described in [Rotation](xref:rotation_article)|
|`backtrack`           |bool|Specifies if [backtracking](features.md#backtracking) is enabled.|
|`animate`             |bool|Dumps snapshots of the output while the generation process is running. Experimental.|
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

For constructing a [path constraint](features.md#path)

| Field                | Type           | Description  |
| -------------------- |---------------|-------|
|`type`|string| `"path"`|
|`pathTiles`|array of [Tile](#tile-references)| The set of tiles that are considered "on the path".|

For constructing a [border constraint](features.md#border)

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

### Adjacency Config

Adjacency config is a way of configuring the [adjacent model](features.md#adjacent) without using sample inputs. You set an array of adjacency entries,
where each entry adds extra permissible neighbours for some tiles in some directions. 

Each adjacency entry it composed of two named lists of [Tile references](#tile-references). Specifically, they take one of three forms:

```javascript
{"left": [...], "right": [...] }
{"up": [...], "down": [...] }
{"above": [...], "below": [...] }
```

For `left`/`right`, this means that it is legal to place any tile found on the `left` list to the left of any tile in the `right` list. 
Similarly `up`/`down` are along the y-axis and `above`/`below` are on the z-axis.

A tile must be listed at least once in every relevant direction, or else it'll have no possible neighbours in some direction, and the generation never include it.

As documented in [here](xref:DeBroglie.Models.AdjacentModel.AddAdjacency(System.Collections.Generic.IList{DeBroglie.Tile},System.Collections.Generic.IList{DeBroglie.Tile},System.Int32,System.Int32,System.Int32,System.Int32,System.Boolean,DeBroglie.TileRotation), if there are rotations specified for tiles, then adjacencies are added for the rotated pairs as appropriate.


File formats
------------

When specifying the `src` property, you can load files from a number of different sources. `dest` is written to in the same format. In each case DeBroglie tries to handle the format sensibly, which means there are some notes particular to each format.

### Bitmap

When loading bitmaps (or pngs), each pixel is assumed to be one tile, with the color desribing which tile.

Additionally, the `animate` setting behaves differently for bitmaps. Uncertain tiles will be drawn as a color blend of all possible tiles.

### Tiled

[Tiled](https://www.mapeditor.org/) .tmx files are supported, though Tiled has many features that are ignored. Both square and hex grids can be read. If there are multiple layers in a square grid, then they are taken to be a 3d topology.

You can also load .tsx files (Tiled tilesets). As these do not come with a map to use as sample input, you must set [adjacencies](#adjacency-config) instead. The output is a .tmx file referencing the .tsx.

### MagicaVoxel

[MagicaVoxel](http://magicavoxel.net/) .vox files are supported. Each voxel becomes one tile. You must abide to the limits of the format when using this, specifically there can only be 255 different tiles (plus the empty tile), and the maximum dimension generated is a cube of size 126.
