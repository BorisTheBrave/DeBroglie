using System.Collections.Generic;

namespace DeBroglie
{
    public static class OverlappingAnalysis
    {
        public static void GetPatterns<T>(ITopArray<T> sample, int n, bool periodic, int symmetries, IEqualityComparer<T> comparer, out List<PatternArray<T>> patternArrays, out List<double> frequencies, out int groundPattern)
        {
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var maxx = periodic ? width - 1 : width - n;
            var maxy = periodic ? height - 1 : height - n;

            var patternIndices = new Dictionary<PatternArray<T>, int>(new PatternArrayComparer<T>(comparer));
            patternArrays = new List<PatternArray<T>>();
            frequencies = new List<double>();

            for (var x = 0; x <= maxx; x++)
            {
                for (var y = 0; y <= maxy; y++)
                {
                    PatternArray<T> patternArray;
                    if (!TryExtract(sample, n, x, y, out patternArray))
                    {
                        continue;
                    }
                    var transformed = new PatternArray<T>[8];
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
            PatternArray<T> groundPatternArray;
            TryExtract(sample, n, width / 2, lowest, out groundPatternArray);
            groundPattern = patternIndices[groundPatternArray];

        }

        private static bool TryExtract<T>(ITopArray<T> sample, int n, int x, int y, out PatternArray<T> pattern)
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
                        pattern = default(PatternArray<T>);
                        return false;
                    }
                    values[tx, ty] = sample.Get(sx, sy);
                }
            }
            pattern = new PatternArray<T> { Values = values };
            return true;
        }

        private class PatternArrayComparer<T> : IEqualityComparer<PatternArray<T>>
        {
            IEqualityComparer<T> comparer;

            public PatternArrayComparer(IEqualityComparer<T> comparer)
            {
                this.comparer = comparer;
            }

            public bool Equals(PatternArray<T> a, PatternArray<T> b)
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

            public int GetHashCode(PatternArray<T> obj)
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
