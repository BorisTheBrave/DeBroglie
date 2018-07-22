using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{

    public class AdjacentModel : TileModel
    {

        private IReadOnlyDictionary<int, Tile> patternsToTiles;
        private ILookup<Tile, int> tilesToPatterns;

        public static AdjacentModel Create<T>(T[,] sample, bool periodic)
        {
            return Create(new TopArray2D<T>(sample, periodic));
        }

        public static AdjacentModel Create<T>(ITopArray<T> sample)
        {
            return new AdjacentModel(sample.ToTiles());
        }

        public AdjacentModel(ITopArray<Tile> sample)
        {
            var topology = sample.Topology;
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;
            var directionCount = topology.Directions.Count;

            // Tiles map 1:1 with patterns
            var tilesToPatterns = new Dictionary<Tile, int>();
            var frequencies = new List<double>();

            List<HashSet<int>[]> propagator = new List<HashSet<int>[]>();

            int GetPattern(Tile tile)
            {
                int pattern;
                if (!tilesToPatterns.TryGetValue(tile, out pattern))
                {
                    pattern = tilesToPatterns[tile] = tilesToPatterns.Count;
                    frequencies.Add(0);
                    propagator.Add(new HashSet<int>[directionCount]);
                    for (var d = 0; d < directionCount; d++)
                    {
                        propagator[pattern][d] = new HashSet<int>();
                    }
                }
                return pattern;
            }

            for (var z = 0; z < depth; z++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var index = topology.GetIndex(x, y, z);
                        if (!topology.ContainsIndex(index))
                            continue;

                        // Find the pattern and update the frequency
                        var pattern = GetPattern(sample.Get(x, y, z));

                        frequencies[pattern] += 1;

                        // Update propogator
                        for (var d = 0; d < directionCount; d++)
                        {
                            int x2, y2, z2;
                            if (topology.TryMove(x, y, z, d, out x2, out y2, out z2))
                            {
                                var pattern2 = GetPattern(sample.Get(x2, y2, z2));
                                propagator[pattern][d].Add(pattern2);
                            }
                        }
                    }
                }
            }

            this.Frequencies = frequencies.ToArray();
            this.Propagator = propagator.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray();
            this.patternsToTiles = tilesToPatterns.ToDictionary(x => x.Value, x => x.Key);
            this.tilesToPatterns = tilesToPatterns.ToLookup(x => x.Key, x => x.Value);
        }

        public override IReadOnlyDictionary<int, Tile> PatternsToTiles => patternsToTiles;
        public override ILookup<Tile, int> TilesToPatterns => tilesToPatterns;

        public override void ChangeFrequency(Tile tile, double relativeChange)
        {
            var multiplier = (1 + relativeChange);
            var patterns = TilesToPatterns[tile];
            foreach (var pattern in patterns)
            {
                Frequencies[pattern] *= multiplier;
            }
        }
    }
}
