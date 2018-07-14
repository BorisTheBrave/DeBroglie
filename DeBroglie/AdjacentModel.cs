using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{

    public class AdjacentModel<T> : TileModel<T>
    {

        private IReadOnlyDictionary<int, T> patternsToTiles;
        private ILookup<T, int> tilesToPatterns;
        private IEqualityComparer<T> comparer;

        public AdjacentModel(T[,] sample, bool periodic)
            :this(new TopArray2D<T>(sample, periodic))
        {

        }
        public AdjacentModel(ITopArray<T> sample)
        {
            this.comparer = EqualityComparer<T>.Default;

            var topology = sample.Topology;
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;
            var directionCount = topology.Directions.Count;

            // Tiles map 1:1 with patterns
            var tilesToPatterns = new Dictionary<T, int>(comparer);
            var frequencies = new List<double>();

            List<HashSet<int>[]> propagator = new List<HashSet<int>[]>();

            int GetPattern(T tile)
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

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
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
            this.tilesToPatterns = tilesToPatterns.ToLookup(x => x.Key, x => x.Value, comparer);
        }

        public override IReadOnlyDictionary<int, T> PatternsToTiles => patternsToTiles;
        public override ILookup<T, int> TilesToPatterns => tilesToPatterns;
        public override IEqualityComparer<T> Comparer => comparer;

        public override void ChangeFrequency(T tile, double relativeChange)
        {
            var multiplier = (1 + relativeChange);
            Frequencies[TilesToPatterns[tile].First()] *= multiplier;
        }
    }
}
