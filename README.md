DeBroglie
==========

DeBroglie is a C# library implementing the [Wave Function Collapse](https://github.com/mxgmn/WaveFunctionCollapse) algorithm with support for additional non-local constraints, and other useful features.

Wave Function Collapse (WFC) is an constraint-based algorithm for generating new images that are locally similar to a sample bitmap. It can also operate on tilesets, generating tilemaps where the tile 
adjacency fits a specification.

Unlike the original WFC implementation, De Broglie has full backtracking support, so can solve arbitrarily complicated sets of constraints. It is still optimized towards local constraints.

Features
--------

* "Overlapped" model implementation of WFC
* Non-local constraints allow you to specify other desired properties of the result
* Backtracking support - the original WFC implementation immediately give up when a contradiction occurs.
* supports 2d tiles, hexs, and 3d voxels

Usage
-----

See https://boristhebrave.github.io/DeBroglie/

Release Notes
-------------

See docs/articles/release_notes.md

Copyright
---------

Code is covered by the MIT license.