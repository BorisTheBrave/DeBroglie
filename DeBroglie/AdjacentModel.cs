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
        {
            this.comparer = EqualityComparer<T>.Default;

            var width = sample.GetLength(0);
            var height = sample.GetLength(1);

            // Tiles map 1:1 with patterns
            var tilesToPatterns = new Dictionary<T, int>(comparer);
            var frequencies = new List<double>();


            var topology = new Topology
            {
                Directions = Directions.Cartesian2dDirections,
                Width = width,
                Height = height,
                Periodic = periodic,
            };
            var directionCount = topology.Directions.Count;

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
                    // Find the pattern and update the frequency
                    var pattern = GetPattern(sample[x, y]);
                    
                    frequencies[pattern] += 1;

                    // Update propogator
                    for (var d = 0; d < directionCount; d++)
                    {
                        int x2, y2;
                        if(topology.TryMove(x, y, d, out x2, out y2))
                        {
                            var pattern2 = GetPattern(sample[x2, y2]);
                            propagator[pattern][d].Add(pattern2);
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


    }
}
