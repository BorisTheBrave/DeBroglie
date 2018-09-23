Introduction
=====================================

DeBroglie is a C# library implementing the [Wave Function Collapse](https://github.com/mxgmn/WaveFunctionCollapse) algorithm with support for additional non-local constraints, and other useful features.

Wave Function Collapse (WFC) is an constraint-based algorithm for which takes a small input image or tilemap
and procedurally generating a larger image in the same style, such as:

<figure>
<a href="https://github.com/BorisTheBrave/DeBroglie/blob/master/samples/mxgmn/city.json">
<img src="../images/city_input.png">
<img src="../images/arrow.png"/>
<img src="../images/city_output.png">
</a>
</figure>
 
DeBroglie is stocked with loads of features to help customize the generation process.

TODO: Motivating examples:



Feature Overview
--------

* ["Overlapped"](features.md#overlapping) model implementation of WFC
* [Non-local constraints](features.md#constraints) allow you to specify other desired properties of the result
* [Backtracking](features.md#backtracking) support - other WFC implementations immediately give up when a contradiction occurs.
* [supports 2d tiles, hex grids, and 3d voxels](features.md#topology) 

What is WFC?
------------

Wave Function Collapse is a constraint based algorithm that generates bitmaps, tilemaps etc one tile at a time, based off a sample image.

<video src="../images/pathway.webm" autoplay loop></video>

The original author of WCF has an excellent [explanation of the core algorithm](https://github.com/mxgmn/WaveFunctionCollapse)

DeBroglie uses the core idea mostly unchanged, though enhanced in various ways explained in [Features](features.md).

Usage
---------------

To use DeBroglie, you start with a simple image or tilemap you want to generalize.

Then, select one of the [models](features.md#models) that controls the generation process. 
There's lot of [features](features.md) that can be applied at this point.

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