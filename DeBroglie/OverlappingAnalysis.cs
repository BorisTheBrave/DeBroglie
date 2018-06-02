using System;
using System.Collections.Generic;

namespace DeBroglie
{
    public class OverlappingAnalysis
    {
        public static void GetPatterns<T>(ITopArray<T> sample, int n, bool periodic, int rotationalSymmetry, bool reflectionalSymmetry, IEqualityComparer<T> comparer, out List<PatternArray<T>> patternArrays, out List<double> frequencies, out int groundPattern)
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
                        var rotatedSample = HexRotate(sample, i, r > 0);
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
            var lowest = periodic ? height - 1 : width - n;
            PatternArray<T> groundPatternArray;
            TryExtract(sample, n, width / 2, lowest, out groundPatternArray);
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
            var maxx = periodic ? width - 1 : width - n;
            var maxy = periodic ? height - 1 : height - n;

            for (var x = 0; x <= maxx; x++)
            {
                for (var y = 0; y <= maxy; y++)
                {
                    PatternArray<T> patternArray;
                    if (!TryExtract(sample, n, x, y, out patternArray))
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

        private static ITopArray<T> HexRotate<T>(ITopArray<T> original, int rotate, bool reflect)
        {
            if (rotate == 0 && !reflect)
                return original;

            var microRotate = rotate % 3;
            var rotate180 = rotate % 2 == 1;
            var offsetx = 0;
            var offsety = 0;

            // Actually do a reflection/rotation
            void OriginalToNewCoord(int x, int y, out int outX, out int outY)
            {
                if(reflect)
                {
                    x = -x + y;
                }
                var q = x - y;
                var r = -x;
                var s = y;
                var q2 = q;
                switch (microRotate)
                {
                    case 0: break;
                    case 1: q = r;  r = s; s = q2; break;
                    case 2: q = s; s = r; r = q2; break;
                }
                if (rotate180)
                {
                    q = -q;
                    r = -r;
                    s = -s;
                }
                x = -r;
                y = s;
                outX = x + offsetx;
                outY = y + offsety;
            }

            // Find new bounds
            int x1, y1, x2, y2, x3, y3, x4, y4;
            OriginalToNewCoord(0, 0, out x1, out y1);
            OriginalToNewCoord(original.Topology.Width - 1, 0, out x2, out y2);
            OriginalToNewCoord(original.Topology.Width - 1, original.Topology.Height, out x3, out y3);
            OriginalToNewCoord(0, original.Topology.Height, out x4, out y4);

            var minx = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
            var maxx = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));
            var miny = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
            var maxy = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));

            // Arrange so that co-ordinate transfer is into the rect bounced by width, height
            offsetx = -minx;
            offsety = -miny;
            var width = maxx - minx + 1;
            var height = maxy - miny + 1;

            var mask = new bool[width * height];
            var topology = new Topology(Directions.Hexagonal2d, width, height, false, mask);
            var values = new T[width, height];

            // Copy from original to values based on the rotation, setting up the mask as we go.
            for (var x = 0; x < original.Topology.Width; x++)
            {
                for (var y = 0; y < original.Topology.Height; y++)
                {
                    int newX, newY;
                    OriginalToNewCoord(x, y, out newX, out newY);
                    int newIndex = topology.GetIndex(newX, newY);
                    values[newX, newY] = original.Get(x, y);
                    mask[newIndex] = original.Topology.ContainsIndex(original.Topology.GetIndex(x, y));
                }
            }

            return new TopArray2D<T>(values, topology);
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
