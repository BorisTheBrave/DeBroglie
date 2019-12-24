using DeBroglie.Trackers;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// The PathConstraint checks that it is possible to connect several locations together via a continuous path of adjacent tiles. 
    /// It does this by banning any tile placement that would make such a path impossible.
    /// </summary>
    public class PathConstraint : ITileConstraint
    {
        private TilePropagatorTileSet tileSet;

        private SelectedTracker selectedTracker;

        private TilePropagatorTileSet endPointTileSet;

        private SelectedTracker endPointSelectedTracker;

        private PathConstraintUtils.SimpleGraph graph;

        /// <summary>
        /// Set of patterns that are considered on the path
        /// </summary>
        public ISet<Tile> Tiles { get; set; }

        /// <summary>
        /// Set of points that must be connected by paths.
        /// If EndPoints and EndPointTiles are null, then PathConstraint ensures that all path cells
        /// are connected.
        /// </summary>
        public Point[] EndPoints { get; set; }

        /// <summary>
        /// Set of tiles that must be connected by paths.
        /// If EndPoints and EndPointTiles are null, then PathConstraint ensures that all path cells
        /// are connected.
        /// </summary>
        public ISet<Tile> EndPointTiles { get; set; }

        public PathConstraint(ISet<Tile> tiles, Point[] endPoints = null)
        {
            this.Tiles = tiles;
            this.EndPoints = endPoints;
        }

        public void Init(TilePropagator propagator)
        {
            tileSet = propagator.CreateTileSet(Tiles);
            selectedTracker = propagator.CreateSelectedTracker(tileSet);
            endPointTileSet = EndPointTiles != null ? propagator.CreateTileSet(EndPointTiles) : null;
            endPointSelectedTracker = EndPointTiles != null ? propagator.CreateSelectedTracker(endPointTileSet) : null;
            graph = PathConstraintUtils.CreateGraph(propagator.Topology);
        }

        public void Check(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            var indices = topology.Width * topology.Height * topology.Depth;
            // Initialize couldBePath and mustBePath based on wave possibilities
            var couldBePath = new bool[indices];
            var mustBePath = new bool[indices];
            for (int i = 0; i < indices; i++)
            {
                var ts = selectedTracker.GetTristate(i);
                couldBePath[i] = ts.Possible();
                mustBePath[i] = ts.IsYes();
            }

            // Select relevant cells, i.e. those that must be connected.
            bool[] relevant;
            if (EndPoints == null && EndPointTiles == null)
            {
                relevant = mustBePath;
            }
            else
            {
                relevant = new bool[indices];
                var relevantCount = 0;
                if (EndPoints != null)
                {
                    foreach (var endPoint in EndPoints)
                    {
                        var index = topology.GetIndex(endPoint.X, endPoint.Y, endPoint.Z);
                        relevant[index] = true;
                        relevantCount++;
                    }
                }
                if (EndPointTiles != null)
                {
                    for (int i = 0; i < indices; i++)
                    {
                        if (endPointSelectedTracker.IsSelected(i))
                        {
                            relevant[i] = true;
                            relevantCount++;
                        }
                    }
                }
                if (relevantCount == 0)
                {
                    // Nothing to do.
                    return;
                }
            }
            var walkable = couldBePath;

            var component = EndPointTiles != null ? new bool[indices] : null;

            var isArticulation = PathConstraintUtils.GetArticulationPoints(graph, walkable, relevant, component);

            if (isArticulation == null)
            {
                propagator.SetContradiction();
                return;
            }

            // All articulation points must be paths,
            // So ban any other possibilities
            for (var i = 0; i < indices; i++)
            {
                if (isArticulation[i] && !mustBePath[i])
                {
                    topology.GetCoord(i, out var x, out var y, out var z);
                    propagator.Select(x, y, z, tileSet);
                }
            }

            // Any EndPointTiles not in the connected component aren't safe to add
            if (EndPointTiles != null)
            {
                for (int i = 0; i < indices; i++)
                {
                    if (!component[i])
                    {
                        topology.GetCoord(i, out var x, out var y, out var z);
                        propagator.Ban(x, y, z, endPointTileSet);
                    }
                }
            }
        }
    }
}
