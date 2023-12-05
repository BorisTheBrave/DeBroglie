---
uid: trackers
title: Trackers
---

Trackers
========

Trackers are an advanced feature of the DeBroglie API. They are provided for efficiency, and do no provide any additional information.

Each tracker listens for changes to a TilePropagator, and updates internal state to provide some summary information. The trackers available are:

|Tracker|Constructor|Description|
|-------|-----------|-----------|
|<a href="xref:DeBroglie.Trackers.ChangeTracker">ChangeTracker</a> |<a href="xref:DeBroglie.TilePropagator.CreateChangeTracker">CreateChangeTracker</a>|Tracks recently changed indices|
|<a href="xref:DeBroglie.Trackers.SelectedTracker">SelectedTracker</a> |<a href="xref:DeBroglie.TilePropagator.CreateSelectedTracker(DeBroglie.TilePropagatorTileSet)">CreateSelectedTracker</a>|Tracks the banned/selected status of each tile with respect to a tileset.|
|<a href="xref:DeBroglie.Trackers.SelectedChangeTracker">SelectedChangeTracker</a> |<a href="xref:DeBroglie.TilePropagator.CreateSelectedChangeTracker(DeBroglie.TilePropagatorTileSet,DeBroglie.Trackers.IQuadstateChanged)">CreateSelectedChangeTracker</a>|Runs a callback when the banned/selected status of tile changes with respect to a tileset|

Trackers are disabled each time the TilePropagator is Cleared, and need to be recreated from scratch.
