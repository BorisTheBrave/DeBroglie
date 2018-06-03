using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{

    public class OverlappingModel<T> : TileModel<T>
    {
        private List<PatternArray<T>> patternArrays;

        private int n;
        private bool periodic;
        int rotationalSymmetry;
        bool reflectionalSymmetry;

        private int groundPattern;

        private IReadOnlyDictionary<int, T> patternsToTiles;
        private ILookup<T, int> tilesToPatterns;
        private IEqualityComparer<T> comparer;

        public OverlappingModel(T[,] sample, int n, bool periodic, int symmetries)
            :this(new TopArray2D<T>(sample, periodic), n, symmetries > 1 ? symmetries / 2 : 1, symmetries > 1)
        {

        }

        public OverlappingModel(ITopArray<T> sample, int n, int rotationalSymmetry, bool reflectionalSymmetry)
        {
            this.n = n;
            this.periodic = sample.Topology.Periodic;
            this.rotationalSymmetry = rotationalSymmetry;
            this.reflectionalSymmetry = reflectionalSymmetry;

            this.comparer = EqualityComparer<T>.Default;

            List<double> frequencies;

            OverlappingAnalysis.GetPatterns(sample, n, periodic, rotationalSymmetry, reflectionalSymmetry, comparer, out patternArrays, out frequencies, out groundPattern);

            this.Frequencies = frequencies.ToArray();

            var directions = sample.Topology.Directions;

            Propagator = new int[patternArrays.Count][][];

            for (var p = 0; p < patternArrays.Count; p++)
            {
                Propagator[p] = new int[directions.Count][];
                for (var d = 0; d < directions.Count; d++)
                {
                    var l = new List<int>();
                    for (var p2 = 0; p2 < patternArrays.Count; p2++)
                    {
                        var dx = directions.DX[d];
                        var dy = directions.DY[d];
                        var dz = directions.DZ[d];
                        if (Aggrees(patternArrays[p], patternArrays[p2], dx, dy, dz))
                        {
                            l.Add(p2);
                        }
                    }
                    Propagator[p][d] = l.ToArray();
                }
            }

            patternsToTiles = patternArrays
                .Select((x, i) => new KeyValuePair<int, T>(i, x.Values[0, 0, 0]))
                .ToDictionary(x => x.Key, x => x.Value);

            tilesToPatterns = patternsToTiles.ToLookup(x => x.Value, x => x.Key, comparer);
        }

        public override IReadOnlyDictionary<int, T> PatternsToTiles => patternsToTiles;
        public override ILookup<T, int> TilesToPatterns => tilesToPatterns;
        public override IEqualityComparer<T> Comparer => comparer;

        /**
          * Return true if the pattern1 is compatible with pattern2
          * when pattern2 is at a distance (dy,dx) from pattern1.
          */
        private bool Aggrees(PatternArray<T> a, PatternArray<T> b, int dx, int dy, int dz)
        {
            var xmin = dx < 0 ? 0 : dx;
            var xmax = dx < 0 ? dx + b.Width : a.Width;
            var ymin = dy < 0 ? 0 : dy;
            var ymax = dy < 0 ? dy + b.Height : a.Height;
            var zmin = dz < 0 ? 0 : dz;
            var zmax = dz < 0 ? dz + b.Depth : a.Depth;
            for (var x = xmin; x < xmax; x++)
            {
                for (var y = ymin; y < ymax; y++)
                {
                    for (var z = zmin; z < zmax; z++)
                    {
                        if (!comparer.Equals(a.Values[x, y, z], b.Values[x - dx, y - dy, z - dz]))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public GroundConstraint GetGroundConstraint()
        {
            return new GroundConstraint(groundPattern);
        }

        public override T[,] ToArray(WavePropagator wavePropagator, T undecided = default(T), T contradiction = default(T))
        {
            T MapPatternOrStatus(int pattern, int px, int py)
            {
                if(pattern == (int)CellStatus.Contradiction)
                {
                    return contradiction;
                }
                else if(pattern == (int)CellStatus.Undecided)
                {
                    return undecided;
                } 
                else
                {
                    return patternArrays[pattern].Values[px, py, 0];
                }
            }

            var a = wavePropagator.ToArray();
            if (wavePropagator.Periodic)
            {
                var width = a.GetLength(0);
                var height = a.GetLength(1);
                var results = new T[width, height];
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        results[x, y] = MapPatternOrStatus(a[x,y], 0, 0);
                    }
                }
                return results;
            }
            else
            {
                var width = a.GetLength(0);
                var height = a.GetLength(1);
                var results = new T[width + n - 1, height + n - 1];
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        results[x, y] = MapPatternOrStatus(a[x, y], 0, 0);
                    }
                }
                for (var x = 0; x < width; x++)
                {
                    for (var y = 1; y < n; y++)
                    {
                        results[x, height - 1 + y] = MapPatternOrStatus(a[x, height - 1], 0, y);
                    }
                }
                for (var y = 0; y < height; y++)
                {
                    for (var x = 1; x < n; x++)
                    {
                        results[width - 1 + x, y] = MapPatternOrStatus(a[width - 1, y], x, 0);
                    }
                }
                for (var x = 1; x < n; x++)
                {
                    for (var y = 1; y < n; y++)
                    {
                        results[width - 1 + x, height - 1 + y] = MapPatternOrStatus(a[width - 1, height - 1], x, y);
                    }
                }
                return results;
            }
        }

        public override List<T>[,] ToArraySets(WavePropagator wavePropagator)
        {
            List<T> Map(List<int> patterns, int px, int py)
            {
                HashSet<T> set = new HashSet<T>(comparer);
                foreach(var pattern in patterns)
                {
                    set.Add(patternArrays[pattern].Values[px, py, 0]);
                }
                return set.ToList();
            }

            var a = wavePropagator.ToArraySets();
            if (periodic)
            {
                var width = a.GetLength(0);
                var height = a.GetLength(1);
                var results = new List<T>[width, height];
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        results[x, y] = Map(a[x, y], 0, 0);
                    }
                }
                return results;
            }
            else
            {
                var width = a.GetLength(0);
                var height = a.GetLength(1);
                var results = new List<T>[width + n - 1, height + n - 1];
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        results[x, y] = Map(a[x, y], 0, 0);
                    }
                }
                for (var x = 0; x < width; x++)
                {
                    for (var y = 1; y < n; y++)
                    {
                        results[x, height - 1 + y] = Map(a[x, height - 1], 0, y);
                    }
                }
                for (var y = 0; y < height; y++)
                {
                    for (var x = 1; x < n; x++)
                    {
                        results[width - 1 + x, y] = Map(a[width - 1, y], x, 0);
                    }
                }
                for (var x = 1; x < n; x++)
                {
                    for (var y = 1; y < n; y++)
                    {
                        results[width - 1 + x, height - 1 + y] = Map(a[width - 1, height - 1], x, y);
                    }
                }
                return results;
            }
        }
    }

}
