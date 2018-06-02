using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{

    public class OverlappingModel<T> : TileModel<T>
    {
        private List<PatternArray> patternArrays;

        private int n;
        private bool periodic;
        private int symmetries;

        private int groundPattern;

        private IReadOnlyDictionary<int, T> patternsToTiles;
        private ILookup<T, int> tilesToPatterns;
        private IEqualityComparer<T> comparer;

        public OverlappingModel(T[,] sample, int n, bool periodic, int symmetries)
            :this(new TopArray2D<T>(sample, periodic), n, symmetries)
        {

        }
        public OverlappingModel(ITopArray<T> sample, int n, int symmetries)
        {
            this.n = n;
            this.periodic = sample.Topology.Periodic;
            this.symmetries = symmetries;

            this.comparer = EqualityComparer<T>.Default;

            List<double> frequencies;

            GetPatterns(sample, n, periodic, symmetries, comparer, out patternArrays, out frequencies, out groundPattern);

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
                        if(Aggrees(patternArrays[p], patternArrays[p2], dx, dy))
                        {
                            l.Add(p2);
                        }
                    }
                    Propagator[p][d] = l.ToArray();
                }
            }

            patternsToTiles = patternArrays
                .Select((x, i) => new KeyValuePair<int, T>(i, x.Values[0, 0]))
                .ToDictionary(x => x.Key, x => x.Value);

            tilesToPatterns = patternsToTiles.ToLookup(x => x.Value, x => x.Key, comparer);
        }

        private static void GetPatterns(ITopArray<T> sample, int n, bool periodic, int symmetries, IEqualityComparer<T> comparer, out List<PatternArray> patternArrays, out List<double> frequencies, out int groundPattern)
        {
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var maxx = periodic ? width - 1 : width - n;
            var maxy = periodic ? height - 1 : height - n;

            var patternIndices = new Dictionary<PatternArray, int>(new PatternArrayComparer(comparer));
            patternArrays = new List<PatternArray>();
            frequencies = new List<double>();

            for (var x = 0; x <= maxx; x++)
            {
                for (var y = 0; y <= maxy; y++)
                {
                    PatternArray patternArray;
                    if(!TryExtract(sample, n, x, y, out patternArray))
                    {
                        continue;
                    }
                    var transformed = new PatternArray[8];
                    transformed[0] = patternArray;
                    transformed[1] = patternArray.Reflected();
                    transformed[2] = transformed[0].Rotated();
                    transformed[3] = transformed[2].Reflected();
                    transformed[4] = transformed[2].Rotated();
                    transformed[5] = transformed[4].Reflected();
                    transformed[6] = transformed[4].Rotated();
                    transformed[7] = transformed[6].Reflected();
                    for (var s = 0; s < symmetries; s++)
                    {
                        int pattern;
                        if (!patternIndices.TryGetValue(transformed[s], out pattern))
                        {
                            pattern = patternIndices[transformed[s]] = patternIndices.Count;
                            patternArrays.Add(transformed[s]);
                            frequencies.Add(1);
                        }
                        else
                        {
                            frequencies[pattern] += 1;
                        }
                    }
                }
            }
            var lowest = periodic ? height - 1 : width - n;
            PatternArray groundPatternArray;
            TryExtract(sample, n, width / 2, lowest, out groundPatternArray);
            groundPattern = patternIndices[groundPatternArray];

        }

        private static bool TryExtract(ITopArray<T> sample, int n, int x, int y, out PatternArray pattern)
        {
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var values = new T[n, n];
            for (int tx = 0; tx < n; tx++)
            {
                var sx = (x + tx) % width;
                for (int ty = 0; ty < n; ty++)
                {
                    var sy = (y + ty) % height;
                    var index = sample.Topology.GetIndex(sx, sy);
                    if (!sample.Topology.ContainsIndex(index))
                    {
                        pattern = default(PatternArray);
                        return false;
                    }
                    values[tx, ty] = sample.Get(sx, sy);
                }
            }
            pattern =  new PatternArray { Values = values };
            return true;
        }

        /**
          * Return true if the pattern1 is compatible with pattern2
          * when pattern2 is at a distance (dy,dx) from pattern1.
          */
        private bool Aggrees(PatternArray a, PatternArray b, int dx, int dy)
        {
            var xmin = dx < 0 ? 0 : dx;
            var xmax = dx < 0 ? dx + b.Width : a.Width;
            var ymin = dy < 0 ? 0 : dy;
            var ymax = dy < 0 ? dy + b.Height : a.Width;
            for (var x = xmin; x < xmax; x++)
            {
                for (var y = ymin; y < ymax; y++)
                {
                    if (!comparer.Equals(a.Values[x, y], b.Values[x - dx, y - dy]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override IReadOnlyDictionary<int, T> PatternsToTiles => patternsToTiles;
        public override ILookup<T, int> TilesToPatterns => tilesToPatterns;
        public override IEqualityComparer<T> Comparer => comparer;

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
                    return patternArrays[pattern].Values[px, py];
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
                    set.Add(patternArrays[pattern].Values[px, py]);
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

        private struct PatternArray
        {
            public T[,] Values;

            public int Width
            {
                get { return Values.GetLength(0); }
            }

            public int Height
            {
                get { return Values.GetLength(1); }
            }

            public PatternArray Reflected()
            {
                var width = Width;
                var height = Height;
                var values = new T[width, height];
                for(var x=0;x<width; x++)
                {
                    for(var y=0;y<height;y++)
                    {
                        values[x, y] = Values[width - 1 - x, y];
                    }
                }
                return new PatternArray { Values = values };
            }

            public PatternArray Rotated()
            {

                var width = Width;
                var height = Height;
                var values = new T[height, width];
                for (var x = 0; x < height; x++)
                {
                    for (var y = 0; y < width; y++)
                    {
                        values[x, y] = Values[width - 1 - y, x];
                    }
                }
                return new PatternArray { Values = values };
            }
        }

        private class PatternArrayComparer : IEqualityComparer<PatternArray>
        {
            IEqualityComparer<T> comparer;

            public PatternArrayComparer(IEqualityComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public bool Equals(PatternArray a, PatternArray b)
            {
                var width = a.Width;
                var height = a.Height;

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        if (!comparer.Equals(a.Values[x, y], b.Values[x, y]))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            public int GetHashCode(PatternArray obj)
            {
                unchecked
                {
                    var width = obj.Width;
                    var height = obj.Height;

                    var hashCode = 13;
                    for (var x = 0; x < width; x++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            hashCode = (hashCode * 397) ^ comparer.GetHashCode(obj.Values[x, y]);
                        }
                    }
                    return hashCode;
                }
            }
        }
    }
}
