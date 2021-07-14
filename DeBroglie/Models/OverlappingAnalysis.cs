using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;

namespace DeBroglie.Models
{

    /// <summary>
    /// Contains utilities relevant to <see cref="OverlappingModel"/>
    /// </summary>
    internal static class OverlappingAnalysis
    {
        public static IEnumerable<ITopoArray<Tile>> GetRotatedSamples(
            ITopoArray<Tile> sample,
            TileRotation tileRotation = null)
        {
            tileRotation = tileRotation ?? new TileRotation();

            foreach (var rotation in tileRotation.RotationGroup)
            {
                yield return TopoArrayUtils.Rotate(sample, rotation, tileRotation);
            }
        }

        public static void GetPatterns(
            ITopoArray<Tile> sample, 
            int nx,
            int ny,
            int nz,
            bool periodicX,
            bool periodicY,
            bool periodicZ,
            Dictionary<PatternArray, int> patternIndices,
            List<PatternArray> patternArrays,
            List<double> frequencies)
        {
            var width = sample.Topology.Width;
            var height = sample.Topology.Height;
            var depth = sample.Topology.Depth;
            var maxx = periodicX ? width - 1 : width - nx;
            var maxy = periodicY ? height - 1 : height - ny;
            var maxz = periodicZ ? depth - 1 : depth - nz;

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

        public static PatternArray PatternEdge(PatternArray patternArray, int dx, int dy, int dz)
        {
            var a = patternArray;
            var edgeWidth = a.Width - Math.Abs(dx);
            var ix = Math.Max(0, dx);
            var edgeHeight = a.Height - Math.Abs(dy);
            var iy = Math.Max(0, dy);
            var edgeDepth = a.Depth - Math.Abs(dz);
            var iz = Math.Max(0, dz);
            var edge = new PatternArray
            {
                Values = new Tile[edgeWidth, edgeHeight, edgeDepth]
            };
            for (var x = 0; x < edgeWidth; x++)
            {
                for (var y = 0; y < edgeHeight; y++)
                {
                    for (var z = 0; z < edgeDepth; z++)
                    {
                        edge.Values[x, y, z] = patternArray.Values[x + ix, y + iy, z + iz];
                    }
                }
            }
            return edge;
        }

        private static bool TryExtract(ITopoArray<Tile> sample, int nx, int ny, int nz, int x, int y, int z, out PatternArray pattern)
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
