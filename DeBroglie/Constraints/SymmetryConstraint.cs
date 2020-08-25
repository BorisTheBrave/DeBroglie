using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Abstract constraint for any sort of global symmetry where 
    /// a choice of a tile in an index implies a selection for a specific index elsewhere.
    /// </summary>
    public abstract class SymmetryConstraint : ITileConstraint
    {
        protected abstract bool TryMapIndex(TilePropagator propagator, int i, out int i2);

        protected abstract bool TryMapTile(Tile tile, out Tile tile2);

        private ChangeTracker changeTracker;

        private static Tile sentinel = new Tile(new object());

        public virtual void Init(TilePropagator propagator)
        {
            changeTracker = propagator.CreateChangeTracker();

            var p = propagator;
            var topology = propagator.Topology;

            // Ban any tiles which don't have a symmetry
            foreach (var i in topology.GetIndices())
            {
                if (TryMapIndex(p, i, out var i2))
                {
                    topology.GetCoord(i, out var x, out var y, out var z);

                    propagator.Select(x, y, z, propagator.TileModel.Tiles
                        .Where(tile => TryMapTile(tile, out var _)));
                }
            }

            // Ban tiles that interact badly with their own symmetry
            foreach (var i in topology.GetIndices())
            {
                if (TryMapIndex(p, i, out var i2))
                {
                    topology.GetCoord(i, out var x, out var y, out var z);

                    if (i2 == i)
                    {
                        // index maps to itself, so only allow tiles that map to themselves
                        var allowedTiles = propagator.TileModel.Tiles
                           .Where(tile => TryMapTile(tile, out var tile2) && tile == tile2);
                        propagator.Select(x, y, z, allowedTiles);
                        continue;
                    }


                    // TODO: Support overlapped model?
                    if (propagator.TileModel is AdjacentModel adjacentModel)
                    {
                        for (var d = 0; d < topology.DirectionsCount; d++)
                        {
                            if (topology.TryMove(i, (Direction)d, out var dest) && dest == i2)
                            {
                                // index maps adjacent to itself, so only allow tiles that can be placed adjacent to themselves
                                var allowedTiles = propagator.TileModel.Tiles
                                    .Where(tile => TryMapTile(tile, out var tile2) && adjacentModel.IsAdjacent(tile, tile2, (Direction)d))
                                    .ToList();
                                propagator.Select(x, y, z, allowedTiles);
                                continue;
                            }
                        }
                    }
                    if (propagator.TileModel is GraphAdjacentModel graphAdjacentModel)
                    {
                        var sentinel = new Tile(new object());
                        for (var d = 0; d < topology.DirectionsCount; d++)
                        {
                            if (topology.TryMove(i, (Direction)d, out var dest, out var _, out var edgeLabel) && dest == i2)
                            {
                                // index maps adjacent to itself, so only allow tiles that can be placed adjacent to themselves
                                var allowedTiles = propagator.TileModel.Tiles
                                    .Where(tile => TryMapTile(tile, out var tile2) && graphAdjacentModel.IsAdjacent(tile, tile2, edgeLabel));
                                propagator.Select(x, y, z, allowedTiles);
                                continue;
                            }
                        }
                    }

                    topology.GetCoord(i2, out var x2, out var y2, out var z2);
                }
            }
        }

        public void Check(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            foreach (var i in changeTracker.GetChangedIndices())
            {
                if (TryMapIndex(propagator, i, out var i2))
                {
                    topology.GetCoord(i, out var x, out var y, out var z);
                    topology.GetCoord(i2, out var x2, out var y2, out var z2);

                    foreach (var tile in propagator.TileModel.Tiles)
                    {
                        if (TryMapTile(tile, out var tile2))
                        {
                            if (propagator.IsBanned(x, y, z, tile) && !propagator.IsBanned(x2, y2, z2, tile2))
                            {
                                propagator.Ban(x2, y2, z2, tile2);
                            }
                        }
                    }
                }
            }
        }
    }
}
