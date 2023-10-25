using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints.Path
{
    internal class HeightmapPathSpec : IPathSpec
    {
        public Dictionary<Tile, int> Heights { get; set; }

        public int MaxDiff { get; set; } = 1;

        public IPathView MakeView(TilePropagator tilePropagator)
        {
            return new HeightmapPathView(this, tilePropagator);
        }

        public PairwisePathSpec ToPairwise()
        {
            var tilesByHeight = Heights.ToLookup(x => x.Value, x => x.Key);
            var pairs = tilesByHeight.SelectMany(g1 =>
                tilesByHeight
                    .Where(g2 => Math.Abs(g1.Key - g2.Key) <= MaxDiff)
                    .SelectMany(g2 => g1.SelectMany(x1 => g2.Select(x2 => (x1, x2)))))
                .ToArray();
            return new PairwisePathSpec
            {
                Pairs = pairs,
            };
        }
    }

    internal class HeightmapPathView : IPathView
    {
        PathConstraintUtils.SimpleGraph graph;
        Dictionary<(int index, int height), int> indexHeightToNode;
        Dictionary<int, (int index, int height)> nodeToIndexHeight;
        ILookup<int, Tile> tilesByHeight;
        Dictionary<int, TilePropagatorTileSet> tileSetByHeight;
        Dictionary<int, SelectedTracker> trackerByHeight;
        private readonly TilePropagator tilePropagator;

        public HeightmapPathView(HeightmapPathSpec spec, TilePropagator tilePropagator)
        {
            tilesByHeight = spec.Heights.ToLookup(x => x.Value, x => x.Key);
            (graph, indexHeightToNode) = CreateGraph(tilePropagator.Topology, tilesByHeight.Select(x => x.Key).ToList(), spec.MaxDiff);
            nodeToIndexHeight = indexHeightToNode.ToDictionary(x => x.Value, x => x.Key);
            tileSetByHeight = tilesByHeight.ToDictionary(g => g.Key, g => tilePropagator.CreateTileSet(g));
            trackerByHeight = tileSetByHeight.ToDictionary(kv => kv.Key, kv => tilePropagator.CreateSelectedTracker(kv.Value));
            this.tilePropagator = tilePropagator;
            CouldBePath = new bool[graph.NodeCount];
            MustBePath = new bool[graph.NodeCount];
        }

        (PathConstraintUtils.SimpleGraph graph, Dictionary<(int index, int height), int> indexHeightToNode) CreateGraph(ITopology topology, List<int> heights, int maxDiff)
        {
            var indexCount = topology.IndexCount;
            var nodeCount = 0;
            var neighbours = new List<int[]>();
            var indexHeightToNode = new Dictionary<(int index, int height), int>();
            var dirCount = topology.DirectionsCount;
            for (var i = 0; i < indexCount; i++)
            {
                foreach (var h in heights)
                {
                    indexHeightToNode[(i, h)] = nodeCount++;
                }
            }
            for (var i = 0; i < indexCount; i++)
            {
                foreach (var h in heights)
                {
                    var nearbyHeights = heights.Where(h2 => Math.Abs(h - h2) <= maxDiff);
                    var ns = new List<int>();
                    for(var d = 0; d < dirCount; d++)
                    {
                        if(topology.TryMove(i, (Direction)d, out var i2))
                        {
                            ns.AddRange(nearbyHeights.Select(h2 => indexHeightToNode[(i2, h2)]));
                        }
                    }
                    neighbours.Add(ns.ToArray());
                }
            }
            var graph = new PathConstraintUtils.SimpleGraph
            {
                Neighbours = neighbours.ToArray(),
                NodeCount = nodeCount,
            };
            return (graph, indexHeightToNode);
        }

        public PathConstraintUtils.SimpleGraph Graph => graph;

        public bool[] CouldBePath { get; }

        public bool[] MustBePath { get; }

        public bool[] CouldBeRelevant => CouldBePath;

        public bool[] MustBeRelevant => MustBePath;

        public void BanPath(int node)
        {
            var (index, height) = nodeToIndexHeight[node];
            tilePropagator.Topology.GetCoord(index, out var x, out var y, out var z);
            tilePropagator.Ban(x, y, z, tileSetByHeight[height]);
        }

        public void BanRelevant(int node)
        {
            BanPath(node);
        }

        public void SelectPath(int node)
        {
            var (index, height) = nodeToIndexHeight[node];
            tilePropagator.Topology.GetCoord(index, out var x, out var y, out var z);
            tilePropagator.Select(x, y, z, tileSetByHeight[height]);
        }

        public void Update()
        {
            foreach (var kv in nodeToIndexHeight)
            {
                var node = kv.Key;
                var (index, height) = kv.Value;
                var qs = trackerByHeight[height].GetQuadstate(index);
                CouldBePath[node] = qs.Possible();
                MustBePath[node] = !qs.PossiblyNot();
            }
        }
    }
}
