---
uid: rotation_article
title: Rotation
---

Rotation Motivation
-------------------

When you supply an input sample, you can optinoally specify the `rotationalSymmetry` and `reflectionalSymmetry`. If you do, extra copies of the sample will be generated, by rotating and reflecting. This can be very handy, as it means you don't need as large an input sample, and you are guaranteed that there will be no bias towards a particular direction. 

 Specifing rotations means even small samples can provide a lot of output variety:

<figure>
<img src="../images/rotation_input.png"/>
<img src="../images/arrow.png"/>
<img src="../images/rotation_output.png"/>
<figcaption>Extra rotation(<a href="../images/rotation.webm">animated</a>)</figcaption>
</figure>

The core WCF algorithm used has no notion of rotation. DeBroglie handles rotation entirely as a pre-processing effect on input samples. So it's not necessary to specify rotations as described below, you can always just add more tiles and more samples. But it's much more convenient to let DeBroglie do it.

DeBroglie works on tile samples, and you cannot just naively rotate the sample image but otherwise leave the tiles unchanged. The tiles will stop joining up with each other:

<canvas id="myCanvas" width="300" height="300"></canvas>
<script src="animation.js"></script>

So when rotating a sample, it's necessary to rotate the tiles as well, and that requires extra information to be passed to DeBroglie.

Quick Start
-----------

There are three common use cases in DeBroglie.

If you are doing pixel based, or voxel based 

TODO

Handling rotating a tile
------------------------

Starting with a given tile, and for a given rotation / reflection, there are 4 choices for what we might want to do:

 1) Re-use the same tile **unchanged**. This is typically the case when the tile is symmetric.
 2) **Replace** the tile with another tile. This is what you want when a tileset comes with rotated variants of the tile in question.
 3) **Generate** a rotated variant of the tile by rotating the actual bitmap / voxels that make up the tile. This is what you want when you don't already have a rotated copy, and it makes sense to rotate the inner details.
 4) **Fail** to rotate. This leaves a hole in the rotated sample, and is typically used when it only makes sense to display a tile a certain way around.
 
 For example, suppose we wanted to rotate the following sample clockwise:

 <img src="../images/rotation_example_scene.png" />
 <img src="../images/rotate_arrow.png"/>
 <img src="../images/rotation_example_scene_rotated.png" />

The we had to treat the different tiles in different ways.

The crossroad tile <img src="../images/grass_crossroad.png" /> is **unchanged** as it is fully symetric.

The bottom left cliff corner <img src="../images/grass_corner1.png" /> gets **replaced** with the top left corner <img src="../images/grass_corner2.png" />. These two images don't look similar due to the change in perspective, but you can easily see that a curve in the cliff would change as you rotate the view by 90 degrees.

The path tiles have no prespective in them, so we are free to take a path tile <img src="../images/grass_corner3.png" /> and **generate** a rotation from it <img src="../images/grass_corner4.png" /> by simply rotating the image.

The steps <img src="../images/grass_steps.png" /> cannot be generated due to perspective and the tileset doesn't have an appropriate replacement, so it **fails** and just leaves a hole in the rotated sample. Holes are ok, though! We're just using the rotated sample as more input examples for the selected model, and the model will know to ignore holes.
 
Specifying Rotations
--------------------

The choice of unchanged, replacement, generate or fail is stored in an <xref:DeBroglie.TileRotation> object. To create one of these, you need a <xref:DeBroglie.TileRotationBuilder>.