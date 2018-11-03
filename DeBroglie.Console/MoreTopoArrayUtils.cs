using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeBroglie.Console
{
    public static class MoreTopoArrayUtils
    {
        public static ITopoArray<T> Scale<T>(ITopoArray<T> topoArray, int scale)
        {
            var topology = topoArray.Topology;
            if (topology.Mask != null)
                throw new NotSupportedException();
            var result = new T[topology.Width * scale, topology.Height * scale, topology.Depth * scale];
            for (var z = 0; z < topology.Depth; z++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var x = 0; x < topology.Width; x++)
                    {
                        var value = topoArray.Get(x, y, z);
                        for (var dz = 0; dz < scale; dz++)
                        {
                            for (var dy = 0; dy < scale; dy++)
                            {
                                for (var dx = 0; dx < scale; dx++)
                                {
                                    result[x * scale + dx, y * scale + dy, z * scale + dz] = value;
                                }
                            }
                        }
                    }
                }
            }
            var resultTopology = topology.WithSize(topology.Width * scale, topology.Height * scale, topology.Depth * scale);
            return TopoArray.Create(result, resultTopology);
        }

        public static ITopoArray<V> ExplodeTiles<V>(ITopoArray<Tile> topoArray, IDictionary<Tile, ITopoArray<V>> subTiles, int tileWidth, int tileHeight, int tileDepth)
        {
            var subTilesCopy = subTiles.ToDictionary(x => x.Key, x => x.Value);
            return Explode(topoArray, tile => GetSubTile(tile, subTilesCopy), tileWidth, tileHeight, tileDepth);
        }

        public static ITopoArray<IEnumerable<V>> ExplodeTileSets<V>(ITopoArray<ISet<Tile>> topoArray, IDictionary<Tile, ITopoArray<V>> subTiles, int tileWidth, int tileHeight, int tileDepth)
        {
            var subTilesCopy = subTiles.ToDictionary(x => x.Key, x => x.Value);
            return ExplodeSets(topoArray, tile => GetSubTile(tile, subTilesCopy), tileWidth, tileHeight, tileDepth);

        }

        private static ITopoArray<V> GetSubTile<V>(Tile tile, IDictionary<Tile, ITopoArray<V>> subTiles)
        {
            if (subTiles.TryGetValue(tile, out var result))
                return result;

            if (tile.Value is RotatedTile rt)
            {
                if (!subTiles.TryGetValue(rt.Tile, out var subTile))
                    return null;
                result = TopoArrayUtils.Rotate(subTile, rt.Rotation);
                return subTiles[tile] = result;
            }

            return null;
        }

        public static ITopoArray<V> Explode<U, V>(ITopoArray<U> topoArray, Func<U, ITopoArray<V>> getSubTile, int tileWidth, int tileHeight, int tileDepth)
        {
            if (topoArray.Topology.Directions.Type != DirectionsType.Cartesian2d && topoArray.Topology.Directions.Type != DirectionsType.Cartesian3d)
                throw new NotImplementedException();

            var inTopology = topoArray.Topology;
            var inWidth = inTopology.Width;
            var inHeight = inTopology.Height;
            var inDepth = inTopology.Depth;

            var resultTopology = inTopology.WithSize(
                inWidth * tileWidth,
                inHeight * tileHeight,
                inDepth * tileDepth
                );
            var result = new V[resultTopology.Width, resultTopology.Height, resultTopology.Depth];
            var mask = new bool[resultTopology.Width * resultTopology.Height * resultTopology.Depth];

            for (var z = 0; z < inDepth; z++)
            {
                for (var y = 0; y < inHeight; y++)
                {
                    for (var x = 0; x < inWidth; x++)
                    {
                        if (inTopology.Mask != null)
                        {
                            var index = inTopology.GetIndex(x, y, z);
                            if (!inTopology.Mask[index])
                                continue;
                        }
                        var inTile = topoArray.Get(x, y, z);
                        var subTile = getSubTile(inTile);
                        if (subTile == null)
                            continue;
                        for (var tz = 0; tz < tileDepth; tz++)
                        {
                            for (var ty = 0; ty < tileHeight; ty++)
                            {
                                for (var tx = 0; tx < tileWidth; tx++)
                                {
                                    if (subTile.Topology.Mask != null)
                                    {
                                        var index = subTile.Topology.GetIndex(tx, ty, tz);
                                        if (!subTile.Topology.Mask[index])
                                            continue;
                                    }
                                    result[x * tileWidth + tx, y * tileHeight + ty, z * tileDepth + tz]
                                        = subTile.Get(tx, ty, tz);
                                    mask[resultTopology.GetIndex(x * tileWidth + tx, y * tileHeight + ty, z * tileDepth + tz)] = true;
                                }
                            }
                        }
                    }
                }
            }

            return TopoArray.Create(result, resultTopology);
        }

        public static ITopoArray<IEnumerable<V>> ExplodeSets<U, V>(ITopoArray<ISet<U>> topoArray, Func<U, ITopoArray<V>> getSubTile, int tileWidth, int tileHeight, int tileDepth)
        {
            if (topoArray.Topology.Directions.Type != DirectionsType.Cartesian2d && topoArray.Topology.Directions.Type != DirectionsType.Cartesian3d)
                throw new NotImplementedException();

            var inTopology = topoArray.Topology;
            var inWidth = inTopology.Width;
            var inHeight = inTopology.Height;
            var inDepth = inTopology.Depth;

            var resultTopology = inTopology.WithSize(
                inWidth * tileWidth,
                inHeight * tileHeight,
                inDepth * tileDepth
                );
            var result = new IEnumerable<V>[resultTopology.Width, resultTopology.Height, resultTopology.Depth];
            var mask = new bool[resultTopology.Width * resultTopology.Height * resultTopology.Depth];

            for (var z = 0; z < inDepth; z++)
            {
                for (var y = 0; y < inHeight; y++)
                {
                    for (var x = 0; x < inWidth; x++)
                    {
                        if (inTopology.Mask != null)
                        {
                            var index = inTopology.GetIndex(x, y, z);
                            if (!inTopology.Mask[index])
                                continue;
                        }
                        var inTileSet = topoArray.Get(x, y, z);
                        if (inTileSet.Count == 0)
                            continue;
                        for (var tz = 0; tz < tileDepth; tz++)
                        {
                            for (var ty = 0; ty < tileHeight; ty++)
                            {
                                for (var tx = 0; tx < tileWidth; tx++)
                                {
                                    var outSet = new List<V>();
                                    foreach (var inTile in inTileSet)
                                    {
                                        var subTile = getSubTile(inTile);
                                        if (subTile == null)
                                            continue;

                                        if (subTile.Topology.Mask != null)
                                        {
                                            var index = subTile.Topology.GetIndex(tx, ty, tz);
                                            if (!subTile.Topology.Mask[index])
                                                continue;
                                        }
                                        outSet.Add(subTile.Get(tx, ty, tz));
                                        mask[resultTopology.GetIndex(x * tileWidth + tx, y * tileHeight + ty, z * tileDepth + tz)] = true;
                                    }
                                    result[x * tileWidth + tx, y * tileHeight + ty, z * tileDepth + tz] = outSet;
                                }
                            }
                        }
                    }
                }
            }

            return TopoArray.Create(result, resultTopology);
        }
    }
}
