using DeBroglie.Rot;
using DeBroglie.Topo;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Models
{
    public class GraphAdjacentModel : TileModel
    {
        private readonly int directionsCount;
        private readonly int edgeLabelCount;
        private Dictionary<Tile, int> tilesToPatterns;
        private List<double> frequencies;
        // By Pattern, then edge-label
        private List<HashSet<int>[]> propagator;

        public GraphAdjacentModel(int directionsCount, int edgeLabelCount)
        {
            this.directionsCount = directionsCount;
            this.edgeLabelCount = edgeLabelCount;
            // Tiles map 1:1 with patterns
            tilesToPatterns = new Dictionary<Tile, int>();
            frequencies = new List<double>();
            propagator = new List<HashSet<int>[]>();
        }

        internal override TileModelMapping GetTileModelMapping(ITopology topology)
        {
            if (frequencies.Sum() == 0.0)
            {
                throw new Exception("No tiles have assigned frequences.");
            }

            var patternModel = new PatternModel
            {
                Propagator = propagator.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray(),
                Frequencies = frequencies.ToArray(),
            };
            var tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>>()
                {
                    {0, tilesToPatterns.ToLookup(x=>x.Key, x=>x.Value).ToDictionary(g=>g.Key, g=>(ISet<int>)new HashSet<int>(g)) }
                };
            var patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>>
                {
                    {0, tilesToPatterns.ToDictionary(x => x.Value, x => x.Key)},
                };
            return new TileModelMapping
            {
                PatternTopology = topology,
                PatternModel = patternModel,
                PatternsToTilesByOffset = patternsToTilesByOffset,
                TilesToPatternsByOffset = tilesToPatternsByOffset,
                TileCoordToPatternCoordIndexAndOffset = null,
            };
        }

        public override IEnumerable<Tile> Tiles => tilesToPatterns.Keys;

        public override void MultiplyFrequency(Tile tile, double multiplier)
        {
            var pattern = tilesToPatterns[tile];
            frequencies[pattern] *= multiplier;
        }

        /// <summary>
        /// Finds a tile and all its rotations, and sets their total frequency.
        /// </summary>
        public void SetFrequency(Tile tile, double frequency, TileRotation tileRotation)
        {
            var rotatedTiles = tileRotation.RotateAll(tile).ToList();
            foreach (var rt in rotatedTiles)
            {
                int pattern = GetPattern(rt);
                frequencies[pattern] = 0.0;
            }
            var incrementalFrequency = frequency / rotatedTiles.Count;
            foreach (var rt in rotatedTiles)
            {
                int pattern = GetPattern(rt);
                frequencies[pattern] += incrementalFrequency;
            }
        }

        /// <summary>
        /// Sets the frequency of a given tile.
        /// </summary>
        public void SetFrequency(Tile tile, double frequency)
        {
            int pattern = GetPattern(tile);
            frequencies[pattern] = frequency;
        }

        /// <summary>
        /// Sets all tiles as equally likely to be picked
        /// </summary>
        public void SetUniformFrequency()
        {
            foreach (var tile in Tiles)
            {
                SetFrequency(tile, 1.0);
            }
        }

        private int GetPattern(Tile tile)
        {
            int pattern;
            if (!tilesToPatterns.TryGetValue(tile, out pattern))
            {
                pattern = tilesToPatterns[tile] = tilesToPatterns.Count;
                frequencies.Add(0);
                propagator.Add(new HashSet<int>[edgeLabelCount]);
                for (var el = 0; el < edgeLabelCount; el++)
                {
                    propagator[pattern][el] = new HashSet<int>();
                }
            }
            return pattern;
        }

        public void AddAdjacency(Tile src, Tile dest, EdgeLabel edgeLabel)
        {
            var s = GetPattern(src);
            var d = GetPattern(dest);
            propagator[s][(int)edgeLabel].Add(d);
        }
    }
}
