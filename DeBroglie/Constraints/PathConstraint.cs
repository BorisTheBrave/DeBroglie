using DeBroglie.Rot;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// The PathConstraint checks that it is possible to connect several locations together via a continuous path of adjacent tiles. 
    /// It does this by banning any tile placement that would make such a path impossible.
    /// </summary>
    [Obsolete("Use ConnectedConstraint instead")]
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

        /// <summary>
        /// If set, Tiles is augmented with extra copies as dictated by the tile rotations
        /// </summary>
        public TileRotation TileRotation { get; set; }


        public PathConstraint(ISet<Tile> tiles, Point[] endPoints = null, TileRotation tileRotation = null)
        {
            this.Tiles = tiles;
            this.EndPoints = endPoints;
            this.TileRotation = tileRotation;
        }

        public void Init(TilePropagator propagator)
        {
            ISet<Tile> actualTiles;
            ISet<Tile> actualEndPointTiles;
            if (TileRotation != null)
            {
                actualTiles = new HashSet<Tile>(TileRotation.RotateAll(Tiles));
                actualEndPointTiles = EndPointTiles == null ? null : new HashSet<Tile>(TileRotation.RotateAll(EndPointTiles));
            }
            else
            {
                actualTiles = Tiles;
                actualEndPointTiles = EndPointTiles;
            }

            tileSet = propagator.CreateTileSet(actualTiles);
            selectedTracker = propagator.CreateSelectedTracker(tileSet);
            endPointTileSet = EndPointTiles != null ? propagator.CreateTileSet(actualEndPointTiles) : null;
            endPointSelectedTracker = EndPointTiles != null ? propagator.CreateSelectedTracker(endPointTileSet) : null;
            graph = PathConstraintUtils.CreateGraph(propagator.Topology);

            Check(propagator, true);
        }

        public void Check(TilePropagator propagator)
        {
            Check(propagator, false);
        }

        private void Check(TilePropagator propagator, bool init)
        {
            var topology = propagator.Topology;
            var indices = topology.IndexCount;
            // Initialize couldBePath and mustBePath based on wave possibilities
            var couldBePath = new bool[indices];
            var mustBePath = new bool[indices];
            for (int i = 0; i < indices; i++)
            {
                var ts = selectedTracker.GetQuadstate(i);
                couldBePath[i] = ts.Possible();
                mustBePath[i] = ts.IsYes();
            }

            // Select relevant cells, i.e. those that must be connected.
            var hasEndPoints = EndPoints != null || EndPointTiles != null;
            bool[] relevant;
            if (!hasEndPoints)
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

            if(init)
            {
                for (int i = 0; i < indices; i++)
                {
                    if(relevant[i])
                    {
                        topology.GetCoord(i, out var x, out var y, out var z);
                        propagator.Select(x, y, z, tileSet);
                    }
                }
            }

            var walkable = couldBePath;

            var info = PathConstraintUtils.GetArticulationPoints(graph, walkable, relevant);
            var isArticulation = info.IsArticulation;

            if (info.ComponentCount > 1)
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

            // Any path tiles / EndPointTiles not in the connected component aren't safe to add.
            if (info.ComponentCount > 0)
            {
                var component = info.Component;
                var actualEndPointTileSet = hasEndPoints ? endPointTileSet : tileSet;
                if (actualEndPointTileSet != null)
                {
                    for (int i = 0; i < indices; i++)
                    {
                        if (component[i] == null)
                        {
                            topology.GetCoord(i, out var x, out var y, out var z);
                            propagator.Ban(x, y, z, actualEndPointTileSet);
                        }
                    }
                }
            }
        }
    }
}
