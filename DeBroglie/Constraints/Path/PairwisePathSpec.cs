using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Two adjacent tiles are considered to form a path only if their specific pair combination is listed in 
    /// Note that this can be really inefficient compared with other path specs.
    /// </summary>
    internal class PairwisePathSpec : IPathSpec
    {
        public (Tile, Tile)[] Pairs { get; set; }

        public IPathView MakeView(TilePropagator tilePropagator)
        {
            return new PairwisePathView(this, tilePropagator);
        }
    }

    /// <summary>
    /// The implementation has one graph node for every cell, and a graph node for every pair of adjacent cells.
    /// The latter nodes are used to constrain the pair of cells to tiles from the list Pairs.
    /// </summary>
    internal class PairwisePathView : IPathView
    {
        private HashSet<Tile> tiles;
        private (Tile, Tile)[] pairs;
        private TilePropagator tilePropagator;
        private ILookup<Tile, Tile> others;
        PathConstraintUtils.SimpleGraph graph;
        Dictionary<int, int> indexToNode;
        Dictionary<(int, int), int> indexPairToNode;
        Dictionary<int, int> nodeToIndex;
        Dictionary<int, (int, int)> nodeToIndexPair;
        Dictionary<Tile, TilePropagatorTileSet> soloTileSets;
        Dictionary<Tile, SelectedTracker> soloTrackers;
        TilePropagatorTileSet pathTileSet;
        SelectedTracker pathTracker;

        public PairwisePathView(PairwisePathSpec pairwisePathSpec, TilePropagator tilePropagator)
        {
            this.pairs = pairwisePathSpec.Pairs;
            this.tilePropagator = tilePropagator;

            others = pairwisePathSpec.Pairs.ToLookup(x => x.Item1, x => x.Item2);
            tiles = new HashSet<Tile>(others.Select(g => g.Key));
            
            (graph, indexToNode, indexPairToNode) = CreateGraph(tilePropagator.Topology);
            nodeToIndex = indexToNode.ToDictionary(kv => kv.Value, kv => kv.Key);
            nodeToIndexPair = indexPairToNode.Where(x=>x.Key.Item1 < x.Key.Item2).ToDictionary(kv => kv.Value, kv => kv.Key);

            CouldBePath = new bool[graph.NodeCount];
            MustBePath = new bool[graph.NodeCount];

            soloTileSets = tiles.ToDictionary(x => x, x => tilePropagator.CreateTileSet(new[] { x }));
            soloTrackers = soloTileSets.ToDictionary(kv => kv.Key, kv => tilePropagator.CreateSelectedTracker(kv.Value));
            pathTileSet = tilePropagator.CreateTileSet(tiles);
            pathTracker = tilePropagator.CreateSelectedTracker(pathTileSet);
        }

        public PathConstraintUtils.SimpleGraph Graph => graph;

        public bool[] CouldBePath { get; }

        public bool[] MustBePath { get; }

        // For now, doesn't support Relevant
        public bool[] CouldBeRelevant => CouldBePath;

        public bool[] MustBeRelevant => MustBePath;

        public void Update()
        {
            foreach(var kv in indexToNode)
            {
                var index = kv.Key;
                var node = kv.Value;
                var qs = pathTracker.GetQuadstate(index);
                CouldBePath[node] = qs.Possible();
                MustBePath[node] = !qs.PossiblyNot();
            }
            foreach(var kv in indexPairToNode)
            {
                var (index1, index2) = kv.Key;
                var node = kv.Value;

                // Skip half the pairs, as everything is listed twice
                if (index2 < index1)
                    continue;

                var couldBePath = false;
                var mustBePath = true;

                foreach (var tile1 in tiles)
                {
                    var qs1 = soloTrackers[tile1].GetQuadstate(index1);
                    foreach(var tile2 in others[tile1])
                    {
                        var qs2 = soloTrackers[tile2].GetQuadstate(index2);
                        couldBePath |= qs1.Possible() && qs2.Possible();
                        mustBePath &= !(qs2.PossiblyNot() && qs2.PossiblyNot());
                    }
                }
                CouldBePath[node] = couldBePath;
                MustBePath[node] = mustBePath;
            }
        }

        public void BanPath(int index)
        {
            throw new NotImplementedException();
        }

        public void BanRelevant(int index)
        {
            BanPath(index);
        }

        public void SelectPath(int node)
        {
            if(nodeToIndex.TryGetValue(node, out var index))
            {
                tilePropagator.Topology.GetCoord(index, out var x, out var y, out var z);
                tilePropagator.Select(x, y, z, pathTileSet);
            }
            if(nodeToIndexPair.TryGetValue(node, out var indexPair))
            {
                var (index1, index2) = indexPair;
                tilePropagator.Topology.GetCoord(index1, out var x1, out var y1, out var z1);
                tilePropagator.Topology.GetCoord(index2, out var x2, out var y2, out var z2);
                // Classic AC3
                var support1 = new HashSet<Tile>(tiles.Where(t => soloTrackers[t].GetQuadstate(index2).Possible()).SelectMany(t => others[t]));
                var support2 = new HashSet<Tile>(tiles.Where(t => soloTrackers[t].GetQuadstate(index1).Possible()).SelectMany(t => others[t]));
                tilePropagator.Select(x1, y1, z1, support1);
                tilePropagator.Select(x2, y2, z2, support2);
            }
        }
        (PathConstraintUtils.SimpleGraph graph, Dictionary<int, int> indexToNode, Dictionary<(int, int), int> indexPairToNode) CreateGraph(ITopology topology)
        {
            var indexToNode = new Dictionary<int, int>();
            var indexPairToNode = new Dictionary<(int, int), int>();
            int nodeCount = 0;
            var neighbours = new List<List<int>>();
            foreach(var i in topology.GetIndices())
            {
                // Add node
                indexToNode[i] = nodeCount;
                neighbours.Add(new List<int>());
                nodeCount++;
            }

            foreach (var i in topology.GetIndices())
            {
                for(var d=0;d<topology.DirectionsCount;d++)
                {
                    if (!topology.TryMove(i, (Direction)d, out var i2))
                        continue;

                    if (indexPairToNode.ContainsKey((i, i2)))
                        continue;

                    // Add index pair
                    indexPairToNode[(i, i2)] = indexPairToNode[(i2, i)] = nodeCount;
                    neighbours.Add(new List<int> { indexToNode[i], indexToNode[i2] });
                    neighbours[indexToNode[i]].Add(nodeCount);
                    neighbours[indexToNode[i2]].Add(nodeCount);
                    nodeCount++;
                }
            }

            var graph = new PathConstraintUtils.SimpleGraph
            {
                NodeCount = nodeCount,
                Neighbours = neighbours.Select(x => x.ToArray()).ToArray(),
            };
            return (graph, indexToNode, indexPairToNode);
        }
    }
}
