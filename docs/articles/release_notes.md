---
uid: release_notes
title: Release Notes
---
# v2.0.0
   * [Added more path based constraints](path_constraints.md). Note, these replace the old PathConstraint class.
   * Support multiple [ModelConstraintAlgorithms](xref:DeBroglie.Wfc.ModelConstraintAlgorithm)
   * Configurable pickers (see [TilePickerType](xref:DeBroglie.TilePickerType), [IndexPickerType](xref:DeBroglie.IndexPickerType)) (for [Editable WFC](https://www.boristhebrave.com/2022/04/25/editable-wfc/))
   * Add [Backjumping](xref:DeBroglie.BacktrackType.Backjump) ([e.g.](https://twitter.com/boris_brave/status/1485006264119799811))
   * Improved behaviour/performance of backtracking
   * Other performance improvements, particularly for large topologies.
   * SeparationConstraint fixes
   * Fix to Symmetry constraint
   * Fix to Count constraint
   * Path constraints more consistent over use of TileRotation ([#14](https://github.com/BorisTheBrave/DeBroglie/issues/14))
   * Fix path constraints "priority" to work with masking
   * Fixed [#12](https://github.com/BorisTheBrave/DeBroglie/issues/12)
   * Fix parsing bug on systems where "," is the decimal separator ([#15](https://github.com/BorisTheBrave/DeBroglie/issues/15))(thanks grapefrukt)

# v1.0.0
 * Improved performance of path constraints
 * Now can use mirror constraint on both X and Y axes
 * Added [GraphTopology](xref:DeBroglie.Topo.GraphTopology) and [GraphAdjacentModel](xref:DeBroglie.Models.GraphAdjacentModel) to allow generation on irregular topologies
 * Added [MeshTopologyBuilder](xref:DeBroglie.Topo.MeshTopologyBuilder) to aid with generating on the surfaces of mesh.
 * Added pick heuristics and prioritized weights (undocumented)
 * Added [SeparationConstraint](xref:DeBroglie.Constraints.SeparationConstraint)
 * Fixed color blending for image output
 * Fixed ToTopoArraySets()
 * Fixed tile rotation bug


# v0.6.0
 * Added DeBroglie.Benchmark
 * Improved performance of most constraints significantly
 * Improved performance of generation - changed seed output [breaking]

# v0.5.0

* Improved memory and performance of backtracking significantly
* Command line program now runs Linux and Mac.
* Added [auto adjacency detection](adjacency.md#auto-adjacency) support.
* Added [CountConstraint](xref:DeBroglie.Constraints.CountConstraint)
* Added EndPointTiles to PathConstraint and EdgedPathConstraint.

# v0.4.0

* Added [EdgedPathConstraint](xref:DeBroglie.Constraints.EdgedPathConstraint)
* Use Direction enum instead of int [breaking]
* Path contraint can now specify end points in JSON
* Added [MirrorConstraint](xref:DeBroglie.Constraints.MirrorConstraint)
* Several constraints now support using an array of tiles instead of a single tile [breaking]
* Fixed several subtle bugs in the core WFC+constraint system.
* Constraint methods now return void [breaking]

# v0.3.0

* Support setting a mask on the output topology of TilePropagator. Doesn't work perfectly with Overlapping, so undocumented feature for now.
* Rotation is now specified in degrees [breaking]
* Core library no longer depends on TiledLib [breaking]
* Added [MaxConsecutiveConstraint](xref:DeBroglie.Constraints.MaxConsecutiveConstraint)

# v0.2.1

* Reduced memory usage of WFC
* Fixed [#5](https://github.com/BorisTheBrave/DeBroglie/issues/5)

# v0.2.0

* Support more input formats:
  * .tsx
  * list of bitmaps
  * list of .vox
* Support for saving .csv files.
* Direct specification of adjacencies.
* Generated tile rotations now working.
* Default tile rotation treatment is now Unchanged. [breaking]
* Added [FixedTileConstraint](xref:DeBroglie.Constraints.FixedTileConstraint).

# v0.1.0

* Initial release