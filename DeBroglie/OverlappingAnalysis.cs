using System.Collections.Generic;

namespace DeBroglie
{

    public static class OverlappingAnalysis
    {
        public static void GetPatterns<T>(
            ITopArray<T> sample, 
            int n,
            bool periodic,
            int rotationalSymmetry,
            bool reflectionalSymmetry,
            IEqualityComparer<T> comparer,
            out List<PatternArray<T>> patternArrays,
            out List<double> frequencies, 
            out int groundPattern)
        {
            var patternIndices = new Dictionary<PatternArray<T>, int>(new PatternArrayComparer<T>(comparer));
            patternArrays = new List<PatternArray<T>>();
            frequencies = new List<double>();

            if (sample.Topology.Directions.Type == DirectionsType.Hexagonal2d)
            {
                // GetPatternsInternal doesn't understand how to rotate hexagonally.
                // It's easier just to rotate the entire map and extract patterns from each copy
                var reflections = reflectionalSymmetry ? 2 : 1;
                for (var r = 0; r < reflections; r++)
                {
                    for (var i = 0; i < rotationalSymmetry; i += (6 / rotationalSymmetry))
                    {
                        var rotatedSample = TopArrayUtils.HexRotate(sample, i, r > 0);
                        GetPatternsInternal(rotatedSample, n, periodic, 1, false, comparer, patternIndices, patternArrays, frequencies);
                    }
                }
            }
            else
            {
                GetPatternsInternal(sample, n, periodic, rotationalSymmetry, reflectionalSymmetry, comparer, patternIndices, patternArrays, frequencies);
            }

            // Find the "ground" pattern, i.e. the patter in the bottom center
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var lowest = periodic ? height - 1 : height - n;
            PatternArray<T> groundPatternArray;
            TryExtract(sample, n, width / 2, lowest, 0, out groundPatternArray);
            groundPattern = patternIndices[groundPatternArray];
        }

        private static void GetPatternsInternal<T>(
            ITopArray<T> sample, 
            int n,
            bool periodic,
            int rotationalSymmetry, 
            bool reflectionalSymmetry,
            IEqualityComparer<T> comparer,
            Dictionary<PatternArray<T>, int> patternIndices,
            List<PatternArray<T>> patternArrays,
            List<double> frequencies)
        {
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var depth = sample.Topology.Depth;
            var maxx = periodic ? width - 1 : width - n;
            var maxy = periodic ? height - 1 : height - n;
            var maxz = depth == 1 ? 1 : periodic ? depth - 1 : depth - n;

            for (var x = 0; x <= maxx; x++)
            {
                for (var y = 0; y <= maxy; y++)
                {
                    for (var z = 0; z <= maxz; z++)
                    {
                        PatternArray<T> patternArray;
                        if (!TryExtract(sample, n, x, y, z, out patternArray))
                        {
                            continue;
                        }
                        var reflections = reflectionalSymmetry ? 2 : 1;
                        var transformed = new PatternArray<T>[rotationalSymmetry * reflections];
                        for (var r = 0; r < reflections; r++)
                        {
                            var current = r > 0 ? patternArray.Reflected() : patternArray;
                            for (var i = 0; i < rotationalSymmetry; i += (6 / rotationalSymmetry))
                            {
                                transformed[r * rotationalSymmetry + i] = current;
                                current = current.Rotated();
                            }
                        }
                        for (var s = 0; s < transformed.Length; s++)
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
            }
        }

        private static bool TryExtract<T>(ITopArray<T> sample, int n, int x, int y, int z, out PatternArray<T> pattern)
        {
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var depth = sample.Topology.Depth;
            var nz = depth == 1 ? 1 : n;
            var values = new T[n, n, nz];
            for (int tx = 0; tx < n; tx++)
            {
                var sx = (x + tx) % width;
                for (int ty = 0; ty < n; ty++)
                {
                    var sy = (y + ty) % height;
                    for (int tz = 0; tz < nz; tz++)
                    {
                        var sz = (z + tz) % depth;
                        var index = sample.Topology.GetIndex(sx, sy, sz);
                        if (!sample.Topology.ContainsIndex(index))
                        {
                            pattern = default(PatternArray<T>);
                            return false;
                        }
                        values[tx, ty, tz] = sample.Get(sx, sy, sz);
                    }
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
                var depth = a.Depth;

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        for (var z = 0; z < depth; z++)
                        {
                            if (!comparer.Equals(a.Values[x, y, z], b.Values[x, y, z]))
                            {
                                return false;
                            }
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
                    var depth = obj.Depth;

                    var hashCode = 13;
                    for (var x = 0; x < width; x++)
                    {
                        for (var y = 0; y < height; y++)
                        {
                            for (var z = 0; z < depth; z++)
                            {
                                hashCode = (hashCode * 397) ^ comparer.GetHashCode(obj.Values[x, y, z]);
                            }
                        }
                    }
                    return hashCode;
                }
            }
        }
    }

}
