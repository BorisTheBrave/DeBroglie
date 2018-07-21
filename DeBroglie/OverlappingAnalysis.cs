using System.Collections.Generic;

namespace DeBroglie
{

    public static class OverlappingAnalysis
    {
        public static void GetPatterns(
            ITopArray<Tile> sample, 
            int nx,
            int ny,
            int nz,
            bool periodic,
            int rotationalSymmetry,
            bool reflectionalSymmetry,
            TileRotation tileRotation,
            Dictionary<PatternArray, int> patternIndices,
            List<PatternArray> patternArrays,
            List<double> frequencies, 
            out int groundPattern)
        {
            tileRotation = tileRotation ?? new TileRotation();

            if (sample.Topology.Directions.Type == DirectionsType.Hexagonal2d)
            {
                var reflections = reflectionalSymmetry ? 2 : 1;
                for (var r = 0; r < reflections; r++)
                {
                    for (var i = 0; i < rotationalSymmetry; i += (6 / rotationalSymmetry))
                    {
                        var rotatedSample = TopArrayUtils.HexRotate(sample, i, r > 0, tileRotation);
                        GetPatternsInternal(rotatedSample, nx, ny, nz, periodic, patternIndices, patternArrays, frequencies);
                    }
                }
            }
            else
            {
                var reflections = reflectionalSymmetry ? 2 : 1;
                for (var r = 0; r < reflections; r++)
                {
                    for (var i = 0; i < rotationalSymmetry; i += (6 / rotationalSymmetry))
                    {
                        var rotatedSample = TopArrayUtils.Rotate(sample, i, r > 0, tileRotation);
                        GetPatternsInternal(rotatedSample, nx, ny, nz, periodic, patternIndices, patternArrays, frequencies);
                    }
                }
            }

            // Find the "ground" pattern, i.e. the patter in the bottom center
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var lowest = periodic ? height - 1 : height - ny;
            PatternArray groundPatternArray;
            TryExtract(sample, nx, ny, nz, width / 2, lowest, 0, out groundPatternArray);
            groundPattern = patternIndices[groundPatternArray];
        }

        private static void GetPatternsInternal(
            ITopArray<Tile> sample, 
            int nx,
            int ny,
            int nz,
            bool periodic,
            Dictionary<PatternArray, int> patternIndices,
            List<PatternArray> patternArrays,
            List<double> frequencies)
        {
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var depth = sample.Topology.Depth;
            var maxx = periodic ? width - 1 : width - nx;
            var maxy = periodic ? height - 1 : height - ny;
            var maxz = periodic ? depth - 1 : depth - nz;

            for (var x = 0; x <= maxx; x++)
            {
                for (var y = 0; y <= maxy; y++)
                {
                    for (var z = 0; z <= maxz; z++)
                    {
                        PatternArray patternArray;
                        if (!TryExtract(sample, nx, ny, nz, x, y, z, out patternArray))
                        {
                            continue;
                        }
                        int pattern;
                        if (!patternIndices.TryGetValue(patternArray, out pattern))
                        {
                            pattern = patternIndices[patternArray] = patternIndices.Count;
                            patternArrays.Add(patternArray);
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

        private static bool TryExtract(ITopArray<Tile> sample, int nx, int ny, int nz, int x, int y, int z, out PatternArray pattern)
        {
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var depth = sample.Topology.Depth;
            var values = new Tile[nx, ny, nz];
            for (int tx = 0; tx < nx; tx++)
            {
                var sx = (x + tx) % width;
                for (int ty = 0; ty < ny; ty++)
                {
                    var sy = (y + ty) % height;
                    for (int tz = 0; tz < nz; tz++)
                    {
                        var sz = (z + tz) % depth;
                        var index = sample.Topology.GetIndex(sx, sy, sz);
                        if (!sample.Topology.ContainsIndex(index))
                        {
                            pattern = default(PatternArray);
                            return false;
                        }
                        values[tx, ty, tz] = sample.Get(sx, sy, sz);
                    }
                }
            }
            pattern = new PatternArray { Values = values };
            return true;
        }

    }

    internal class PatternArrayComparer : IEqualityComparer<PatternArray>
    {
        public bool Equals(PatternArray a, PatternArray b)
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
                        if (a.Values[x, y, z] != b.Values[x, y, z])
                        {
                            return false;
                        }
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
                var depth = obj.Depth;

                var hashCode = 13;
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        for (var z = 0; z < depth; z++)
                        {
                            hashCode = (hashCode * 397) ^ obj.Values[x, y, z].GetHashCode();
                        }
                    }
                }
                return hashCode;
            }
        }
    }

}
