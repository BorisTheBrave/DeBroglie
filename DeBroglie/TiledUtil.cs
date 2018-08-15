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
        public static ITopoArray<int> ReadLayer(Map map, TileLayer layer)
        {
            if (map.Orientation == Orientation.orthogonal)
            {
                var layerArray = new int[layer.Width, layer.Height];
                var i = 0;
                for (int y = 0; y < layer.Height; y++)
                {
                    for (int x = 0; x < layer.Width; x++)
                    {
                        layerArray[x, y] = layer.Data[i++];
                    }
                }
                return new TopoArray2D<int>(layerArray, false);
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
                var layerArray = new int[width, height];
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
                        layerArray[newX, newY] = layer.Data[i++];
                        var index = topology.GetIndex(newX, newY, 0);
                        mask[index] = true;
                    }
                    isStaggered = !isStaggered;
                }
                return new TopoArray2D<int>(layerArray, topology);
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
        public static TileLayer MakeTileLayer(Map map, ITopoArray<int> array, int z = 0)
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
                        data[i++] = array.Get(x, y, z);
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
                        data[newX + newY * newWidth] = array.Get(x, y, 0);
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
    }
}
