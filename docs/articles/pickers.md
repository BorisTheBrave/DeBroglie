# Pickers

WaveFunctionCollapse is an iterative process. In each loop, a tile is assigned to a cell and then the constraint propagator runs.
*Pickers* are what DeBroglie uses to decide what cells and tiles to use in this loop. Configuring these can turn DeBroglie from doing WaveFunctionCollapse to doing a variety of other effects.

There are two sorts of pickers. Index pickers select which cell to fill next. Tile pickers choose which tile to assign to that cell. In some cases, you need to set both at once.

Pickers can only be set via the the C# API. They are configured by setting fields on [TilePropagatorOptions](xref:DeBroglie.TilePropagatorOptions). We'll go through all available pickers now.

## Index Pickers

Index pickers are set setting the [TilePropagatorOptions.IndexPickerType](xref:DeBroglie.TilePropagatorOptions.IndexPickerType) option to a value from [IndexPickerType](xref:DeBroglie.IndexPickerType)

### Default

The default index picker is currently the same as the HeapMinEntropy picker, though it may change in later versions.

### Ordered

This picker walks through the cells in a predetermined order. You can optionally specify the order using [TilePropagatorOptions.IndexOrder](xref:DeBroglie.TilePropagatorOptions.IndexOrder).

The Ordered picker can be used to recreate [Model Synthesis](https://paulmerrell.org/model-synthesis/) - a constraint based generator older than WaveFunctionCollapse.

### MinEntropy

This picker picks the cell with the least "entropy", which roughly means the cell with the fewest possible tiles remaining.

### HeapMinEntropy

This picker works exactly like MinEntropy, but it has better performance at large generations.

### ArrayPriorityMinEntropy

This picker is intended to be used with TilePicker.ArrayPriority, and automatically selects that option. It is a minentropy picker that understands about the 
per-cell probabilities.

### Dirty

This picker works the same as the Ordered picker, but it only picks "dirty" cells. To use it, you must set [TilePropagatorOptions.CleanTiles](xref:DeBroglie.TilePropagatorOptions.CleanTiles) which is an input tilemap. A cell is considered dirty if the clean tile for that cell is not possible in the current generation. As all tiles start clean, you typically must [Select](xref:DeBroglie.TilePropagator.Select(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) or [Ban](xref:DeBroglie.TilePropagator.Ban(System.Int32,System.Int32,System.Int32,DeBroglie.Tile)) some tiles first.

If it cannot find a dirty tile, it finishes the generation early. The dirty picker is described in in more detail on [my blog](https://www.boristhebrave.com/2022/04/25/editable-wfc/).

## Tile Pickers

Tile pickers are set setting the [TilePropagatorOptions.TilePickerType](xref:DeBroglie.TilePropagatorOptions.TilePickerType) option to a value from [TilePickerType](xref:DeBroglie.TilePickerType)

### Default

The default tile picker is currently the same as the Weighted picker, though it may change in later versions.

### Weighted

Randomly picks between the possible tiles for the cell. The model supplies the weights used for the random choice. The weights are typically chosen to match the frequency of the tile in the input samples, or set uniformly to 1.

### Ordered

Picks the first possible tile. This corresponds, e.g. to how [Townscaper](https://store.steampowered.com/app/1291340/Townscaper/) works.

The order of tiles is the order they were added to the tile model.

### ArrayPriority

This picker works a bit like Weighted, but it has two extra features:
 * The weights can be set on a per-cell basis, allowing you to vary the generation over the space of the output.
 * In addition to weights, you can also set priorities. A tile with a higher priority value will always be chosen over a lower priority.
   So it's a bit like `actual_weight = HUGE_NUMBER * priority + weight`.

Setting array priority is a bit complex. You must set both WeightSets and WeightSetByIndex.
Each weight set associates a weight and priority to every tile. The weightsets themselves are numbered, then WeightSetByIndex selects which weight set is used for which cell.