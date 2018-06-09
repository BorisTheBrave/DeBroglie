De Broglie
==========

De Broglie is a C# library implementing the [Wave Function Collapse](https://github.com/mxgmn/WaveFunctionCollapse) algorithm with support for additional non-local constraints, and other useful features.

Wave Function Collapse (WFC) is an constraint-based algorithm for generating new images that are locally similar to a sample bitmap. It can also operate on tilesets, generating tilemaps where the tile 
adjacency fits a specification.

Unlike other WFC implementations, De Broglie has full backtracking support, so can solve arbitrarily complicated sets of constraints. It is still optimized towards local constraints.

Features
--------

* "Overlapped" model implementation of WFC
* Non-local constraints allow you to specify other desired properties of the result
* Backtracking support - other WFC implementations immediately give up when a contradiction occurs.
* supports 2d tiles, hexs, and 3d voxels

Usage
-----

TODO


Copyright
---------

Code is covered by the MIT license.


FAQ
---

Q: What does the Wave Function Collapse Algorithm have to do with the quantum physics concept of Wave Function Collapse

A: Very little really. The original idea was that you have a probability space of possible tile choices similar to how a wave function is a possibility space for particle properties. 
But the comparison is only really skin deep - it's just a cool name.

Q: How do you pronounce De Broglie

A: "duh broy". [Wikipedia has a pronounciation guide](https://en.wikipedia.org/wiki/Louis_de_Broglie)

Q: How do I...

A: I plan to write some articles on the best way to use the library. Contact me if you have something in mind, and I can prioritize writing it up.