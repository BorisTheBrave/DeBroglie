using DeBroglie.Console.Export;
using DeBroglie.Console.Import;
using DeBroglie.MagicaVoxel;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Console.Config
{
    public static class AdjacencyUtils
    {
        #region Auto Adjacency
        private static IDictionary<Tile, ITopoArray<Rgba32>> GetSubTiles(BitmapSetExportOptions bseo, out Topology subTileTopology)
        {
            var t = subTileTopology = new Topology(bseo.TileWidth, bseo.TileHeight, false);
            return bseo.Bitmaps.ToDictionary(x => x.Key, x => TopoArray.Create(BitmapUtils.ToColorArray(x.Value), t));
        }

        private static double DiffColor(Rgba32 a, Rgba32 b)
        {
            return (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) + Math.Abs(a.A - b.A)) / 4.0 / 255.0;
        }

        private static IDictionary<Tile, ITopoArray<byte>> GetSubTiles(VoxSetExportOptions vseo, out Topology subTileTopology)
        {
            subTileTopology = new Topology(vseo.TileWidth, vseo.TileHeight, vseo.TileDepth, false);
            return vseo.SubTiles.ToDictionary(x => x.Key, x => VoxUtils.ToTopoArray(x.Value));
        }

        private static double DiffIndex(byte a, byte b)
        {
            return a == b ? 0 : 1;
        }

        public static IList<AdjacentModel.Adjacency> GetAutoAdjacencies(SampleSet sampleSet, TileRotation tileRotations, double tolerance)
        {
            if (sampleSet.ExportOptions is BitmapSetExportOptions bseo)
            {
                var subTiles = GetSubTiles(bseo, out var subTileTopology);
                return GetAutoAdjacencies(subTiles, subTileTopology, tileRotations, DiffColor, tolerance);
            }
            if (sampleSet.ExportOptions is VoxSetExportOptions vseo)
            {
                var subTiles = GetSubTiles(vseo, out var subTileTopology);
                return GetAutoAdjacencies(subTiles, subTileTopology, tileRotations, DiffIndex, tolerance);
            }
            throw new Exception("AutoAdjacency not supported for this type of tile set");
        }

        private static List<T> SliceX<T>(ITopoArray<T> topoArray, int x)
        {
            var l = new List<T>();
            var topology = topoArray.Topology;
            for (var z = 0; z < topology.Depth; z++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    var i = topology.GetIndex(x, y, z);
                    l.Add(topology.ContainsIndex(i) ? topoArray.Get(i) : default(T));
                }
            }
            return l;
        }

        private static List<T> SliceY<T>(ITopoArray<T> topoArray, int y)
        {
            var l = new List<T>();
            var topology = topoArray.Topology;
            for (var z = 0; z < topology.Depth; z++)
            {
                for (var x = 0; x < topology.Width; x++)
                {
                    var i = topology.GetIndex(x, y, z);
                    l.Add(topology.ContainsIndex(i) ? topoArray.Get(i) : default(T));
                }
            }
            return l;
        }

        private static List<T> SliceZ<T>(ITopoArray<T> topoArray, int z)
        {
            var l = new List<T>();
            var topology = topoArray.Topology;
            for (var y = 0; y < topology.Height; y++)
            {
                for (var x = 0; x < topology.Width; x++)
                {
                    var i = topology.GetIndex(x, y, z);
                    l.Add(topology.ContainsIndex(i) ? topoArray.Get(i) : default(T));
                }
            }
            return l;
        }

        private static double DiffSlice<T>(List<T> a, List<T> b, Func<T, T, double> diff)
        {
            var t = 0.0;
            for (var i = 0; i < a.Count; i++)
            {
                t += diff(a[i], b[i]);
            }
            return t / a.Count;
        }

        private static IList<AdjacentModel.Adjacency> GetAutoAdjacencies<T>(
            IDictionary<Tile, ITopoArray<T>> subTiles,
            Topology subTileTopology,
            TileRotation tileRotations,
            Func<T, T, double> diff,
            double tolerance)
        {
            // Pre-process for rotations
            var allSubTiles = subTiles;
            if(subTileTopology.Width == subTileTopology.Height)
            {
                allSubTiles = new Dictionary<Tile, ITopoArray<T>>();
                foreach(var kv in subTiles)
                {
                    foreach(var rot in tileRotations.RotationGroup)
                    {
                        if(tileRotations.Rotate(kv.Key, rot, out var rt) && !allSubTiles.ContainsKey(rt))
                        {
                            allSubTiles[rt] = TopoArrayUtils.Rotate(kv.Value, rot);
                        }
                    }
                }
            }


            var output = new List<AdjacentModel.Adjacency>();

            // Left-right
            {
                var leftSlices = allSubTiles.ToDictionary(x => x.Key, x => SliceX(x.Value, 0));
                var rightSlices = allSubTiles.ToDictionary(x => x.Key, x => SliceX(x.Value, subTileTopology.Width - 1));

                foreach (var kv1 in leftSlices)
                {
                    foreach (var kv2 in rightSlices)
                    {
                        if (DiffSlice(kv1.Value, kv2.Value, diff) <= tolerance)
                        {
                            output.Add(new AdjacentModel.Adjacency
                            {
                                Src = new[] { kv2.Key },
                                Dest = new[] { kv1.Key },
                                Direction = Direction.XPlus
                            });
                        }
                    }
                }
            }

            //
            {
                var upSlices = allSubTiles.ToDictionary(x => x.Key, x => SliceY(x.Value, 0));
                var downSlices = allSubTiles.ToDictionary(x => x.Key, x => SliceY(x.Value, subTileTopology.Height - 1));

                foreach (var kv1 in upSlices)
                {
                    foreach (var kv2 in downSlices)
                    {
                        if (DiffSlice(kv1.Value, kv2.Value, diff) <= tolerance)
                        {
                            output.Add(new AdjacentModel.Adjacency
                            {
                                Src = new[] { kv2.Key },
                                Dest = new[] { kv1.Key },
                                Direction = Direction.YPlus
                            });
                        }
                    }
                }
            }

            //
            if(subTileTopology.Directions.Type == DirectionSetType.Cartesian3d)
            {
                var aboveSlices = allSubTiles.ToDictionary(x => x.Key, x => SliceZ(x.Value, 0));
                var belowSlices = allSubTiles.ToDictionary(x => x.Key, x => SliceZ(x.Value, subTileTopology.Depth - 1));

                foreach (var kv1 in aboveSlices)
                {
                    foreach (var kv2 in belowSlices)
                    {
                        if (DiffSlice(kv1.Value, kv2.Value, diff) <= tolerance)
                        {
                            output.Add(new AdjacentModel.Adjacency
                            {
                                Src = new[] { kv2.Key },
                                Dest = new[] { kv1.Key },
                                Direction = Direction.ZPlus
                            });
                        }
                    }
                }
            }

            return output;
        }
        #endregion

        // Experimental. Remove adjacencies such that
        // Anything that can can connect to tile, must connect to tile.
        // This is hard to express otherwise.
        public static IList<AdjacentModel.Adjacency> ForcePadding(IList<AdjacentModel.Adjacency> adjacencies, Tile tile)
        {
            var results = new List<AdjacentModel.Adjacency>();
            var tileSet = new[] { tile };
            foreach (var a in adjacencies)
            {
                if (a.Src.Contains(tile) || a.Dest.Contains(tile))
                {
                    results.Add(new AdjacentModel.Adjacency
                    {
                        Src = a.Src.Except(tileSet).ToArray(),
                        Dest = a.Dest.Intersect(tileSet).ToArray(),
                        Direction = a.Direction,
                    });
                    results.Add(new AdjacentModel.Adjacency
                    {
                        Src = a.Src.Intersect(tileSet).ToArray(),
                        Dest = a.Dest.Except(tileSet).ToArray(),
                        Direction = a.Direction,
                    });
                    results.Add(new AdjacentModel.Adjacency
                    {
                        Src = a.Src.Intersect(tileSet).ToArray(),
                        Dest = a.Dest.Intersect(tileSet).ToArray(),
                        Direction = a.Direction,
                    });
                }
                else
                {
                    results.Add(a);
                }
            }
            return results;
        }

        public static AdjacentModel.Adjacency Rotate(AdjacentModel.Adjacency adjacency, Rotation rotation, DirectionSet directions, TileRotation tileRotation)
        {
            return new AdjacentModel.Adjacency
            {
                Src = tileRotation.Rotate(adjacency.Src, rotation).ToArray(),
                Dest = tileRotation.Rotate(adjacency.Dest, rotation).ToArray(),
                Direction = TopoArrayUtils.RotateDirection(directions, adjacency.Direction, rotation),
            };
        }

        public static IList<AdjacentModel.Adjacency> Rotate(IList<AdjacentModel.Adjacency> adjacencies, Rotation rotation, DirectionSet directions, TileRotation tileRotation)
        {
            return adjacencies
                .Select(x => Rotate(x, rotation, directions, tileRotation))
                .Where(x => x.Src.Length > 0 && x.Dest.Length > 0)
                .ToList();
        }

        public static IList<AdjacentModel.Adjacency> Rotate(IList<AdjacentModel.Adjacency> adjacencies, RotationGroup rg, DirectionSet directions, TileRotation tileRotation)
        {
            return rg.SelectMany(r => Rotate(adjacencies, r, directions, tileRotation)).ToList();
        }

    }
}
