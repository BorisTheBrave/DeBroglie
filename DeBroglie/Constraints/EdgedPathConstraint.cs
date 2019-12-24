using DeBroglie.Rot;
using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    public class EdgedPathConstraint : ITileConstraint
    {
        private TilePropagatorTileSet pathTileSet;

        private SelectedTracker pathSelectedTracker;

        private TilePropagatorTileSet endPointTileSet;

        private SelectedTracker endPointSelectedTracker;

        private PathConstraintUtils.SimpleGraph graph;

        private IDictionary<Direction, TilePropagatorTileSet> tilesByExit;
        private IDictionary<Direction, SelectedTracker> trackerByExit;
        private IDictionary<Tile, ISet<Direction>> actualExits { get; set; }


        /// <summary>
        /// For each tile on the path, the set of direction values that paths exit out of this tile.
        /// </summary>
        public IDictionary<Tile, ISet<Direction>> Exits { get; set; }

        /// <summary>
        /// Set of points that must be connected by paths.
        /// If EndPoints and EndPointTiles are null, then EdgedPathConstraint ensures that all path cells
        /// are connected.
        /// </summary>
        public Point[] EndPoints { get; set; }

        /// <summary>
        /// Set of tiles that must be connected by paths.
        /// If EndPoints and EndPointTiles are null, then EdgedPathConstraint ensures that all path cells
        /// are connected.
        /// </summary>
        public ISet<Tile> EndPointTiles { get; set; }

        /// <summary>
        /// If set, Exits is augmented with extra copies as dictated by the tile rotations
        /// </summary>
        public TileRotation TileRotation { get; set; }

        public EdgedPathConstraint(IDictionary<Tile, ISet<Direction>> exits, Point[] endPoints = null, TileRotation tileRotation = null)
        {
            this.Exits = exits;
            this.EndPoints = endPoints;
            this.TileRotation = tileRotation;
        }


        public void Init(TilePropagator propagator)
        {
            pathTileSet = propagator.CreateTileSet(Exits.Keys);
            pathSelectedTracker = propagator.CreateSelectedTracker(pathTileSet);
            endPointTileSet = EndPointTiles != null ? propagator.CreateTileSet(EndPointTiles) : null;
            endPointSelectedTracker = EndPointTiles != null ? propagator.CreateSelectedTracker(endPointTileSet) : null;
            graph = CreateEdgedGraph(propagator.Topology);

            var tileRotation = TileRotation ?? new TileRotation();

            actualExits = new Dictionary<Tile, ISet<Direction>>();
            foreach(var kv in Exits)
            {
                foreach(var rot in tileRotation.RotationGroup)
                {
                    if(tileRotation.Rotate(kv.Key, rot, out var rtile))
                    {
                        Direction Rotate(Direction d)
                        {
                            return TopoArrayUtils.RotateDirection(propagator.Topology.Directions, d, rot);
                        }
                        var rexits = new HashSet<Direction>(kv.Value.Select(Rotate));
                        actualExits[rtile] = rexits;
                    }
                }
            }

            tilesByExit = actualExits
                .SelectMany(kv => kv.Value.Select(e => Tuple.Create(kv.Key, e)))
                .GroupBy(x => x.Item2, x => x.Item1)
                .ToDictionary(g => g.Key, propagator.CreateTileSet);

            trackerByExit = tilesByExit
                .ToDictionary(kv => kv.Key, kv => propagator.CreateSelectedTracker(kv.Value));
        }

        public void Check(TilePropagator propagator)
        {

            var topology = propagator.Topology;
            var indices = topology.Width * topology.Height * topology.Depth;

            var nodesPerIndex = topology.Directions.Count + 1;

            // Initialize couldBePath and mustBePath based on wave possibilities
            var couldBePath = new bool[indices * nodesPerIndex];
            var mustBePath = new bool[indices * nodesPerIndex];
            var exitMustBePath = new bool[indices * nodesPerIndex];
            for (int i = 0; i < indices; i++)
            {
                foreach(var kv in trackerByExit)
                {
                    var exit = kv.Key;
                    var tracker = kv.Value;
                    var ts = tracker.GetTristate(i);
                    couldBePath[i * nodesPerIndex + 1 + (int)exit] = ts.Possible();
                    // Cannot put this in mustBePath these points can be disconnected, depending on topology mask
                    exitMustBePath[i * nodesPerIndex + 1 + (int)exit] = ts.IsYes();
                }
                var pathTs = pathSelectedTracker.GetTristate(i);
                couldBePath[i * nodesPerIndex] = pathTs.Possible();
                mustBePath[i * nodesPerIndex] = pathTs.IsYes();
            }
            // Select relevant cells, i.e. those that must be connected.
            bool[] relevant;
            if (EndPoints == null && EndPointTiles == null)
            {
                relevant = mustBePath;
            }
            else
            {
                relevant = new bool[indices * nodesPerIndex];

                var relevantCount = 0;
                if (EndPoints != null)
                {
                    foreach (var endPoint in EndPoints)
                    {
                        var index = topology.GetIndex(endPoint.X, endPoint.Y, endPoint.Z);
                        relevant[index * nodesPerIndex] = true;
                        relevantCount++;
                    }
                }
                if (EndPointTiles != null)
                {
                    for (int i = 0; i < indices; i++)
                    {
                        if (endPointSelectedTracker.IsSelected(i))
                        {
                            relevant[i * nodesPerIndex] = true;
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
                topology.GetCoord(i, out var x, out var y, out var z);
                if (isArticulation[i * nodesPerIndex] && !mustBePath[i * nodesPerIndex])
                {
                    propagator.Select(x, y, z, pathTileSet);
                }
                for (var d = 0; d < topology.Directions.Count; d++)
                {
                    if(isArticulation[i * nodesPerIndex + 1 + d] && !exitMustBePath[i * nodesPerIndex + 1 + d])
                    {
                        propagator.Select(x, y, z, tilesByExit[(Direction)d]);
                    }
                }
            }

            // Any EndPointTiles not in the connected component aren't safe to add
            if (EndPointTiles != null)
            {
                for (int i = 0; i < indices; i++)
                {
                    if (!component[i * nodesPerIndex])
                    {
                        topology.GetCoord(i, out var x, out var y, out var z);
                        propagator.Ban(x, y, z, endPointTileSet);
                    }
                }
            }
        }

        private static readonly int[] Empty = { };

        /// <summary>
        /// Creates a grpah where each index in the original topology
        /// has 1+n nodes in the graph - one for the initial index
        /// and one for each direction leading out of it.
        /// </summary>
        private static PathConstraintUtils.SimpleGraph CreateEdgedGraph(Topology topology)
        {
            var nodesPerIndex = topology.Directions.Count + 1;

            var nodeCount = topology.IndexCount * nodesPerIndex;

            var neighbours = new int[nodeCount][];

            int GetNodeId(int index) => index * nodesPerIndex;

            int GetDirNodeId(int index, Direction direction) => index * nodesPerIndex + 1 + (int)direction;

            foreach (var i in topology.Indicies)
            {
                var n = new List<int>();
                foreach(var d in topology.Directions)
                {
                    if (topology.TryMove(i, d, out var dest))
                    {
                        // The central node connects to the direction node
                        n.Add(GetDirNodeId(i, d));
                        var inverseDir = topology.Directions.Inverse(d);
                        // The diction node connects to the central node
                        // and the opposing direction node
                        neighbours[GetDirNodeId(i, d)] =
                            new[] { GetNodeId(i), GetDirNodeId(dest, inverseDir) };
                    }
                    else
                    {
                        neighbours[GetDirNodeId(i, d)] = Empty;
                    }
                }
                neighbours[GetNodeId(i)] = n.ToArray();
            }

            return new PathConstraintUtils.SimpleGraph
            {
                NodeCount = nodeCount,
                Neighbours = neighbours,
            };
        }
    }
}
