using DeBroglie.Topo;
using System;
using System.IO;
using System.Text;
using TiledLib;
using TiledLib.Layer;

namespace DeBroglie
{
    /// <summary>
    /// Utilities for working with <see href="https://www.mapeditor.org/">Tiled files</see>. 
    /// This class mostly delegates work to <see href="https://github.com/Ragath/TiledLib.Net">TiledLib.Net</see>
    /// </summary>
    public static class TiledUtil
    {
        /// <summary>
        /// Loads a Map
        /// </summary>
        public static Map Load(string filename)
        {
            using (var s = File.OpenRead(filename))
            {
                return Map.FromStream(s, ts => File.OpenRead(Path.Combine(Path.GetDirectoryName(filename), ts.source)));
            }
        }

        /// <summary>
        /// Saves a Map to a file.
        /// </summary>
        public static void Save(string filename, Map map)
        {
            using (var stream = File.OpenWrite(filename))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(), 1024, true))
            {
                writer.WriteTmxMap(map);
                // We truncate the file now rather than at the start as this plays better
                // with Tiled's auto-reloading functionality
                stream.SetLength(stream.Position);
            }
        }

        /// <summary>
        /// Reads a layer of a Map into an <see cref="ITopoArray{T}"/>
        /// </summary>
        public static ITopoArray<Tile> ReadLayer(Map map, TileLayer layer)
        {
            if (map.Orientation == Orientation.orthogonal)
            {
                var layerArray = new Tile[layer.Width, layer.Height];
                var i = 0;
                for (int y = 0; y < layer.Height; y++)
                {
                    for (int x = 0; x < layer.Width; x++)
                    {
                        layerArray[x, y] = GidToTile(layer.Data[i++], map.Orientation);
                    }
                }
                return new TopoArray2D<Tile>(layerArray, false);
            }
            else if(map.Orientation == Orientation.hexagonal)
            {
                // Tiled uses a staggered hex layout, while we use an axial one
                // Convert between them, masking out the dead space
                // For now, only support one mode of staggering
                if (map.StaggerAxis != StaggerAxis.y)
                    throw new NotImplementedException($"Maps staggered on x axis not supported");

                var width = layer.Width + (layer.Height + 1) / 2;
                var height = layer.Height;
                var layerArray = new Tile[width, height];
                var mask = new bool[width * height];
                var topology = new Topology(Directions.Hexagonal2d, width, height, false, false, mask);

                int i = 0;
                var isStaggered = map.StaggerIndex == StaggerIndex.even;
                var xoffset = isStaggered ? -1 : 0;
                for (int y = 0; y < layer.Height; y++)
                {
                    if (isStaggered)
                        xoffset += 1;
                    for (int x = 0; x < layer.Width; x++)
                    {
                        var newY = y;
                        var newX = x + xoffset;
                        layerArray[newX, newY] = GidToTile(layer.Data[i++], Orientation.hexagonal);
                        var index = topology.GetIndex(newX, newY, 0);
                        mask[index] = true;
                    }
                    isStaggered = !isStaggered;
                }
                return new TopoArray2D<Tile>(layerArray, topology);
            }
            else
            {
                throw new NotImplementedException($"{map.Orientation} not supported");
            }
        }

        /// <summary>
        /// Convers a <see cref="ITopoArray{T}"/> to a layer of a Map
        /// If the array is 3d, this reads a place with a given z co-ordinate.
        /// </summary>
        public static TileLayer MakeTileLayer(Map map, ITopoArray<Tile> array, int z = 0)
        {
            if (map.Orientation == Orientation.orthogonal)
            {
                var width = array.Topology.Width;
                var height = array.Topology.Height;
                var data = new int[width * height];
                var i = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        data[i++] = TileToGid(array.Get(x, y, z), Orientation.orthogonal);
                    }
                }
                var layer = new TileLayer();
                layer.Encoding = "base64";
                layer.Data = data;
                layer.Width = width;
                layer.Height = height;
                layer.Visible = true;
                layer.Opacity = 1.0;
                return layer;
            }
            else if (map.Orientation == Orientation.hexagonal)
            {
                // Tiled uses a staggered hex layout, while we use an axial one
                // Convert between them, masking out the dead space
                // For now, only support one mode of staggering
                if (map.StaggerAxis != StaggerAxis.y)
                    throw new NotImplementedException($"Maps staggered on x axis not supported");

                var width = array.Topology.Width;
                var height = array.Topology.Height;
                var newWidth = width + (height + 1) / 2;
                var newHeight = height;
                var data = new int[newWidth * newHeight];

                var isStaggered = map.StaggerIndex == StaggerIndex.even;
                var xoffset = (isStaggered ? 1 : 0) + (height + 1) / 2;
                for (int y = 0; y < height; y++)
                {
                    if (isStaggered)
                        xoffset -= 1;
                    for (int x = 0; x < width; x++)
                    {
                        var newY = y;
                        var newX = x + xoffset;
                        data[newX + newY * newWidth] = TileToGid(array.Get(x, y, 0), Orientation.hexagonal);
                    }
                    isStaggered = !isStaggered;
                }
                var layer = new TileLayer();
                layer.Encoding = "base64";
                layer.Data = data;
                layer.Width = newWidth;
                layer.Height = newHeight;
                layer.Visible = true;
                layer.Opacity = 1.0;
                return layer;
            }
            else
            {
                throw new NotImplementedException($"{map.Orientation} not supported");
            }
        }

        const uint FlippedHorizontally = 0x80000000;
        const uint FlippedVertically = 0x40000000;
        const uint FlippedDiagonally = 0x20000000;

        public static Tile GidToTile(int gid, Orientation orientation = Orientation.orthogonal)
        {
            uint ugid = unchecked((uint)gid);
            if (orientation == Orientation.hexagonal)
            {
                var tileId = (int)(ugid & ~0xF0000000);
                var bits = ugid >> 28;
                // I determined these empirically, the 4th bit is undocumented
                switch (bits)
                {
                    case 0:
                        return new Tile(tileId);
                    case 0b0010:
                        return new Tile(new RotatedTile { RotateCw = 1, ReflectX = false, Tile = new Tile(tileId) });
                    case 0b0001:
                        return new Tile(new RotatedTile { RotateCw = 2, ReflectX = false, Tile = new Tile(tileId) });
                    case 0b1100:
                        return new Tile(new RotatedTile { RotateCw = 3, ReflectX = false, Tile = new Tile(tileId) });
                    case 0b1110:
                        return new Tile(new RotatedTile { RotateCw = 4, ReflectX = false, Tile = new Tile(tileId) });
                    case 0b1101:
                        return new Tile(new RotatedTile { RotateCw = 5, ReflectX = false, Tile = new Tile(tileId) });
                    case 0b1000:
                        return new Tile(new RotatedTile { RotateCw = 0, ReflectX = true, Tile = new Tile(tileId) });
                    case 0b1010:
                        return new Tile(new RotatedTile { RotateCw = 1, ReflectX = true, Tile = new Tile(tileId) });
                    case 0b1001:
                        return new Tile(new RotatedTile { RotateCw = 2, ReflectX = true, Tile = new Tile(tileId) });
                    case 0b0100:
                        return new Tile(new RotatedTile { RotateCw = 3, ReflectX = true, Tile = new Tile(tileId) });
                    case 0b0110:
                        return new Tile(new RotatedTile { RotateCw = 4, ReflectX = true, Tile = new Tile(tileId) });
                    case 0b0101:
                        return new Tile(new RotatedTile { RotateCw = 5, ReflectX = true, Tile = new Tile(tileId) });
                }
            }
            else
            {
                var tileId = (int)(ugid & ~0xE0000000);
                var bits = ugid >> 29;
                switch (bits)
                {
                    case 0:
                        return new Tile(tileId);
                    case 0b101:
                        return new Tile(new RotatedTile { RotateCw = 1, ReflectX = false, Tile = new Tile(tileId) });
                    case 0b110:
                        return new Tile(new RotatedTile { RotateCw = 2, ReflectX = false, Tile = new Tile(tileId) });
                    case 0b011:
                        return new Tile(new RotatedTile { RotateCw = 3, ReflectX = false, Tile = new Tile(tileId) });
                    case 0b100:
                        return new Tile(new RotatedTile { RotateCw = 0, ReflectX = true, Tile = new Tile(tileId) });
                    case 0b111:
                        return new Tile(new RotatedTile { RotateCw = 1, ReflectX = true, Tile = new Tile(tileId) });
                    case 0b010:
                        return new Tile(new RotatedTile { RotateCw = 2, ReflectX = true, Tile = new Tile(tileId) });
                    case 0b001:
                        return new Tile(new RotatedTile { RotateCw = 3, ReflectX = true, Tile = new Tile(tileId) });
                }
            }
            throw new Exception();
        }

        public static int TileToGid(Tile tile, Orientation orientation = Orientation.orthogonal)
        {

            if (tile.Value is RotatedTile rt)
            {
                var tileId = unchecked((long)(uint)(int)rt.Tile.Value);
                if (orientation == Orientation.hexagonal)
                {
                    if(!rt.ReflectX)
                    {
                        switch(rt.RotateCw)
                        {
                            case 0: return (int)(0b0000L << 28 | tileId);
                            case 1: return (int)(0b0010L << 28 | tileId);
                            case 2: return (int)(0b0001L << 28 | tileId);
                            case 3: return (int)(0b1100L << 28 | tileId);
                            case 4: return (int)(0b1110L << 28 | tileId);
                            case 5: return (int)(0b1101L << 28 | tileId);
                        }
                    }
                    else
                    {
                        switch (rt.RotateCw)
                        {
                            case 0: return (int)(0b1000L << 28 | tileId);
                            case 1: return (int)(0b1010L << 28 | tileId);
                            case 2: return (int)(0b1001L << 28 | tileId);
                            case 3: return (int)(0b0100L << 28 | tileId);
                            case 4: return (int)(0b0110L << 28 | tileId);
                            case 5: return (int)(0b0101L << 28 | tileId);
                        }
                    }
                }
                else
                {
                    if (!rt.ReflectX)
                    {
                        switch (rt.RotateCw)
                        {
                            case 0: return (int)(0b000L << 29 | tileId);
                            case 1: return (int)(0b101L << 29 | tileId);
                            case 2: return (int)(0b110L << 29 | tileId);
                            case 3: return (int)(0b011L << 29 | tileId);
                        }
                    }
                    else
                    {
                        switch (rt.RotateCw)
                        {
                            case 0: return (int)(0b100L << 29 | tileId);
                            case 1: return (int)(0b111L << 29 | tileId);
                            case 2: return (int)(0b010L << 29 | tileId);
                            case 3: return (int)(0b001L << 29 | tileId);
                        }
                    }
                }
            }
            else
            {
                return (int)tile.Value;
            }
            throw new Exception();
        }
    }
}
