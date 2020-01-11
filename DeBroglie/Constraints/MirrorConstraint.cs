using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    public class MirrorConstraint : ITileConstraint
    {
        private readonly static Rotation reflectX = new Rotation(0, true);

        private ChangeTracker changeTracker;

        public TileRotation TileRotation { get; set; }

        public void Init(TilePropagator propagator)
        {
            changeTracker = propagator.CreateChangeTracker();

            // Strictly speaking, no initialization is needed.
            // In practise, this is useful to stop WFC from blundering
            // into easy avoided contradictions.

            var topology = propagator.Topology;

            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            // Ban any tiles which don't have a reflection
            // Note we don't require the topology mask to be symmetric
            // So there may be some spots they are ok
            var reflectableTileSet = propagator.CreateTileSet(propagator.TileModel.Tiles
                .Where(tile => TileRotation.Rotate(tile, reflectX, out var _)));
            foreach (var i in topology.Indicies)
            {
                topology.GetCoord(i, out var x, out var y, out var z);
                var x2 = topology.Width - 1 - x;
                var i2 = topology.GetIndex(x2, y, z);
                if(topology.ContainsIndex(i2))
                {
                    propagator.Select(x, y, z, reflectableTileSet);
                }
            }

            // Ensure we don't pick a central tile that interacts badly
            // with its own reflection
            if (propagator.TileModel is AdjacentModel adjacentModel)
            {
                TilePropagatorTileSet symmetricTileSet;
                if (width % 2 == 1)
                {
                    // Enforce the center strip is symetric
                    symmetricTileSet = propagator.CreateTileSet(adjacentModel.Tiles
                        .Where(tile => TileRotation.Rotate(tile, reflectX, out var otherTile) && tile == otherTile));

                    var x = width / 2;
                    for (var z = 0; z < depth; z++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            var i = topology.GetIndex(x, y, z);
                            if (topology.ContainsIndex(i))
                            {
                                propagator.Select(x, y, z, symmetricTileSet);
                            }
                        }
                    }
                }
                else
                {
                    // Enforce column left of center connect to their mirrored selves
                    symmetricTileSet = propagator.CreateTileSet(adjacentModel.Tiles
                        .Where(tile => TileRotation.Rotate(tile, reflectX, out var otherTile) && adjacentModel.IsAdjacent(tile, otherTile, Topo.Direction.XPlus)));

                    var x = width / 2 - 1;
                    var x2 = width / 2;
                    for (var z = 0; z < depth; z++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            var i = topology.GetIndex(x, y, z);
                            var i2 = topology.GetIndex(x2, y, z);
                            if (topology.ContainsIndex(i) && topology.ContainsIndex(i2))
                            {
                                propagator.Select(x, y, z, symmetricTileSet);
                            }
                        }
                    }
                }

            }

            // TODO: Something similar for OverlappingModel
        }

        public void Check(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            foreach(var i in changeTracker.GetChangedIndices())
            {
                topology.GetCoord(i, out var x, out var y, out var z);
                var x2 = topology.Width - 1 - x;

                foreach(var tile in propagator.TileModel.Tiles)
                {
                    if (TileRotation.Rotate(tile, reflectX, out var tile2))
                    {
                        if (propagator.IsBanned(x, y, z, tile) && !propagator.IsBanned(x2, y, z, tile2))
                        {
                            propagator.Ban(x2, y, z, tile2);
                        }
                    }
                }
            }
        }
    }
}
