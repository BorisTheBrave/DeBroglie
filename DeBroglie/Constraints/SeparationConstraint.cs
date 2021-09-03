using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// This constriant forces particular tiles to not be placed near each other.
    /// It's useful for giving a more even distribution of tiles, similar to a Poisson disk sampling.
    /// </summary>
    public class SeparationConstraint : ITileConstraint
    {
        private TilePropagatorTileSet tileset;
        private SelectedChangeTracker changeTracker;
        private NearbyTracker nearbyTracker;

        /// <summary>
        /// Set of tiles, all of which should be separated from each other.
        /// </summary>
        public ISet<Tile> Tiles { get; set; }

        /// <summary>
        /// The minimum distance between two points.
        /// Measured using manhattan distance.
        /// </summary>
        public int MinDistance { get; set; }


        public void Init(TilePropagator propagator)
        {
            tileset = propagator.CreateTileSet(Tiles);
            nearbyTracker = new NearbyTracker { MinDistance = MinDistance, Topology = propagator.Topology };
            changeTracker = propagator.CreateSelectedChangeTracker(tileset, nearbyTracker);

            // Review the initial state
            foreach(var index in propagator.Topology.GetIndices())
            {
                if (changeTracker.GetQuadstate(index).IsYes())
                {
                    nearbyTracker.VisitNearby(index, false);
                }
            }

            Check(propagator);
        }

        public void Check(TilePropagator propagator)
        {
            if (nearbyTracker.NewlyVisited.Count == 0)
                return;

            var newlyVisited = nearbyTracker.NewlyVisited;
            nearbyTracker.NewlyVisited = new HashSet<int>();

            foreach (var index in newlyVisited)
            {
                propagator.Topology.GetCoord(index, out var x, out var y, out var z);
                propagator.Ban(x, y, z, tileset);
            }
        }


        internal class NearbyTracker : IQuadstateChanged
        {
            public ITopology Topology;

            public ISet<int> NewlyVisited = new HashSet<int>();

            public int MinDistance;

            public void VisitNearby(int index, bool undo)
            {
                // Dijkstra's with fixed weights is just a queue
                var queue = new Queue<(int, int)>();
                var visited = new HashSet<int>();

                void Visit(int i2, int dist)
                {
                    if (visited.Add(i2))
                    {
                        queue.Enqueue((i2, dist));

                        if (undo)
                        {
                            NewlyVisited.Remove(i2);
                        }
                        else
                        {
                            if (dist != 0)
                            {
                                NewlyVisited.Add(i2);
                            }
                        }
                    }
                }

                Visit(index, 0);

                while (queue.Count > 0)
                {
                    var (i, dist) = queue.Dequeue();
                    if (dist < MinDistance - 1)
                    {
                        for (var dir = 0; dir < Topology.DirectionsCount; dir++)
                        {
                            if (Topology.TryMove(i, (Direction)dir, out var i2))
                            {
                                Visit(i2, dist + 1);
                            }
                        }
                    }
                }
            }

            public void Reset(SelectedChangeTracker tracker)
            {
            }

            public void Notify(int index, Quadstate before, Quadstate after)
            {
                var a = after == Quadstate.Yes || after == Quadstate.Contradiction;
                var b = before == Quadstate.Yes || before == Quadstate.Contradiction;
                if (a && !b)
                {
                    VisitNearby(index, false);
                }
                if(b && !a)
                {
                    // Must be backtracking. 
                    // The main backtrack mechanism will handle undoing bans, and 
                    // undos are always in order, so we just need to reverse VisitNearby
                    VisitNearby(index, true);
                }
            }
        }

    }
}
