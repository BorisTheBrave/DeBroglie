using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// This constraint forces one set of tiles to not be placed near another set.
    /// </summary>
    public class PairSeparationConstraint : ITileConstraint
    {
        private TilePropagatorTileSet tileset1;
        private TilePropagatorTileSet tileset2;
        private SelectedChangeTracker changeTracker1;
        private SelectedChangeTracker changeTracker2;
        private SeparationConstraint.NearbyTracker nearbyTracker1;
        private SeparationConstraint.NearbyTracker nearbyTracker2;

        public ISet<Tile> Tiles1 { get; set; }
        public ISet<Tile> Tiles2 { get; set; }

        /// <summary>
        /// The minimum distance between two points.
        /// Measured using manhattan distance.
        /// </summary>
        public int MinDistance { get; set; }


        public void Init(TilePropagator propagator)
        {
            tileset1 = propagator.CreateTileSet(Tiles1);
            tileset2 = propagator.CreateTileSet(Tiles2);
            nearbyTracker1 = new SeparationConstraint.NearbyTracker { MinDistance = MinDistance, Topology = propagator.Topology };
            nearbyTracker2 = new SeparationConstraint.NearbyTracker { MinDistance = MinDistance, Topology = propagator.Topology };
            changeTracker1 = propagator.CreateSelectedChangeTracker(tileset1, nearbyTracker1);
            changeTracker2 = propagator.CreateSelectedChangeTracker(tileset2, nearbyTracker2);

            // Review the initial state
            foreach(var index in propagator.Topology.GetIndices())
            {
                if (changeTracker1.GetQuadstate(index).IsYes())
                {
                    nearbyTracker1.VisitNearby(index, false);
                }
                if (changeTracker2.GetQuadstate(index).IsYes())
                {
                    nearbyTracker2.VisitNearby(index, false);
                }
            }

            Check(propagator);
        }

        public void Check(TilePropagator propagator)
        {
            if (nearbyTracker1.NewlyVisited.Count != 0)
            {
                var newlyVisited = nearbyTracker1.NewlyVisited;
                nearbyTracker1.NewlyVisited = new HashSet<int>();

                foreach (var index in newlyVisited)
                {
                    propagator.Topology.GetCoord(index, out var x, out var y, out var z);
                    propagator.Ban(x, y, z, tileset2);
                }
            }
            if (nearbyTracker2.NewlyVisited.Count != 0)
            {
                var newlyVisited = nearbyTracker2.NewlyVisited;
                nearbyTracker2.NewlyVisited = new HashSet<int>();

                foreach (var index in newlyVisited)
                {
                    propagator.Topology.GetCoord(index, out var x, out var y, out var z);
                    propagator.Ban(x, y, z, tileset1);
                }
            }
        }
    }
}
