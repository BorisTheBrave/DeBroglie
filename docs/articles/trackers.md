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
|(ChangeTracker)[xref:DeBroglie.Trackers.ChangeTracker] |(CreateChangeTracker)[xref:DeBroglie.TilePropagator.CreateChangeTracker]|Tracks recently changed indices|
|(SelectedTracker)[xref:DeBroglie.Trackers.SelectedTracker] |(CreateSelectedTracker)[DeBroglie.TilePropagator.CreateSelectedTracker(DeBroglie.TilePropagatorTileSet)]|Tracks the banned/selected status of each tile with respect to a tileset.|
|(SelectedChangeTracker)[xref:DeBroglie.Trackers.SelectedChangeTracker] |(CreateSelectedChangeTracker)[DeBroglie.TilePropagator.CreateSelectedChangeTracker(DeBroglie.TilePropagatorTileSet,DeBroglie.Trackers.IQuadstateChanged)]|Runs a callback when the banned/selected status of tile changes with respect to a tileset|

Trackers are disabled each time the TilePropagator is Cleared, and need to be recreated from scratch.