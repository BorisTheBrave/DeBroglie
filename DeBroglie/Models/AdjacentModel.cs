using DeBroglie.Topo;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{

    public class AdjacentModel : TileModel
    {
        private IReadOnlyDictionary<int, Tile> patternsToTiles;
        private Dictionary<Tile, int> tilesToPatterns;
        private List<double> frequencies;
        private List<HashSet<int>[]> propagator;

        public static AdjacentModel Create<T>(T[,] sample, bool periodic)
        {
            return Create(new TopoArray2D<T>(sample, periodic));
        }

        public static AdjacentModel Create<T>(ITopoArray<T> sample)
        {
            return new AdjacentModel(sample.ToTiles());
        }

        public AdjacentModel()
        {
            // Tiles map 1:1 with patterns
            tilesToPatterns = new Dictionary<Tile, int>();
            frequencies = new List<double>();

            propagator = new List<HashSet<int>[]>();
        }

        public AdjacentModel(ITopoArray<Tile> sample)
        {
            AddSample(sample);
        }

        public void AddSample(ITopoArray<Tile> sample, int rotationalSymmetry, bool reflectionalSymmetry, TileRotation tileRotation = null)
        {
            foreach (var s in OverlappingAnalysis.GetRotatedSamples(sample, rotationalSymmetry, reflectionalSymmetry, tileRotation))
            {
                AddSample(s);
            }
        }

        public void AddSample(ITopoArray<Tile> sample)
        {
            var topology = sample.Topology;
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;
            var directionCount = topology.Directions.Count;

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

            // Update the model based on the collected data
            this.Frequencies = frequencies.ToArray();
            this.Propagator = propagator.Select(x => x.Select(y => y.ToArray()).ToArray()).ToArray();
            this.patternsToTiles = tilesToPatterns.ToDictionary(x => x.Value, x => x.Key);
        }

        public override IReadOnlyDictionary<int, Tile> PatternsToTiles => patternsToTiles;
        public override ILookup<Tile, int> TilesToPatterns  => tilesToPatterns.ToLookup(x=>x.Key, x=>x.Value);

        public override void ChangeFrequency(Tile tile, double relativeChange)
        {
            var multiplier = (1 + relativeChange);
            var patterns = TilesToPatterns[tile];
            foreach (var pattern in patterns)
            {
                frequencies[pattern] *= multiplier;
            }
        }
    }
}
