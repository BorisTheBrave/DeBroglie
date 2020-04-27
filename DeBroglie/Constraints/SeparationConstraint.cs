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
                if (changeTracker.GetTristate(index).IsYes())
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


        private class NearbyTracker : ITristateChanged
        {
            public ITopology Topology;

            public ISet<int> Visited = new HashSet<int>();
            public ISet<int> NewlyVisited = new HashSet<int>();

            public int MinDistance;

            public void VisitNearby(int index, bool undo)
            {
                // Dijkstra's with fixed weights is just a queue
                var queue = new Queue<(int, int)>();
                queue.Enqueue((index, 0));

                while (queue.Count > 0)
                {
                    var (i, dist) = queue.Dequeue();
                    if (dist < MinDistance - 1)
                    {
                        for (var dir = 0; dir < Topology.DirectionsCount; dir++)
                        {
                            if (Topology.TryMove(i, (Direction)dir, out var i2))
                            {
                                if (!Visited.Contains(i2))
                                {
                                    queue.Enqueue((i2, dist + 1));
                                }
                            }
                        }
                    }

                    if (undo)
                    {
                        Visited.Remove(i);
                        NewlyVisited.Remove(i);
                    }
                    else
                    {
                        Visited.Add(i);
                        if (dist > 0)
                        {
                            NewlyVisited.Add(i);
                        }

                    }
                }
            }

            public void Reset(SelectedChangeTracker tracker)
            {
            }

            public void Notify(int index, Tristate before, Tristate after)
            {
                if(after.IsYes())
                {
                    VisitNearby(index, false);
                }
                if(before.IsYes())
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
