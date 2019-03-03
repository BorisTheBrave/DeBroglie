using DeBroglie.Models;
using DeBroglie.Rot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    public class MirrorConstraint : ITileConstraint
    {
        private static Rotation reflectX = new Rotation(0, true);

        public TileRotation TileRotation { get; set; }

        public Resolution Init(TilePropagator propagator)
        {
            // Strictly speaking, no initialization is needed.
            // In practise, this is useful to stop WFC from blundering
            // into easy avoided contradictions.

            var topology = propagator.Topology;

            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            if (propagator.TileModel is AdjacentModel adjacentModel)
            {
                TilePropagatorTileSet symetricTileSet;
                if (width % 2 == 1)
                {
                    // Enforce the center strip is symetric
                    symetricTileSet = propagator.CreateTileSet(adjacentModel.Tiles
                        .Where(tile => TileRotation.Rotate(tile, reflectX, out var otherTile) && tile == otherTile));
                }
                else
                {
                    // Enforce column left of center connect to their mirrored selves
                    symetricTileSet = propagator.CreateTileSet(adjacentModel.Tiles
                        .Where(tile => TileRotation.Rotate(tile, reflectX, out var otherTile) && adjacentModel.IsAdjacent(tile, otherTile, Topo.Direction.XPlus)));
                }

                var x = width / 2;
                for (var z = 0; z < depth; z++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        propagator.Select(x, y, z, symetricTileSet);
                    }
                }
            }

            // TODO: Something similar for OverlappingModel

            return Resolution.Undecided;
        }

        public Resolution Check(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            foreach(var i in topology.Indicies)
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
            return Resolution.Undecided;
        }
    }
}
