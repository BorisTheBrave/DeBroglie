using DeBroglie.Models;
using DeBroglie.Topo;
using System;
using System.Drawing;

namespace DeBroglie.Console
{
    public class BitmapItemsProcessor : ItemsProcessor
    {
        private static Color[,] ToColorArray(Bitmap bitmap)
        {
            Color[,] sample = new Color[bitmap.Width, bitmap.Height];
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    sample[x, y] = bitmap.GetPixel(x, y);
                }
            }
            return sample;
        }

        private static Bitmap ToBitmap(Color[,] colorArray)
        {
            var bitmap = new Bitmap(colorArray.GetLength(0), colorArray.GetLength(1));
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    bitmap.SetPixel(x, y, colorArray[x, y]);
                }
            }
            return bitmap;
        }

        protected override ITopoArray<Tile> Load(string filename, DeBroglieConfig config)
        {
            var bitmap = new Bitmap(filename);
            var colorArray = ToColorArray(bitmap);
            var topology = new Topology(Directions.Cartesian2d, colorArray.GetLength(0), colorArray.GetLength(1), config.PeriodicInputX, config.PeriodicInputY);
            return TopoArray.Create(colorArray, topology).ToTiles();
        }

        protected override void Save(TileModel model, TilePropagator propagator, string filename, DeBroglieConfig config)
        {
            var array = propagator.ToValueArray(Color.Gray, Color.Magenta);
            var bitmap = ToBitmap(array.ToArray2d());
            bitmap.Save(filename);
        }

        protected override Tile Parse(string s)
        {
            return new Tile(ColorTranslator.FromHtml(s));
        }

        private static ITopoArray<T> Scale<T>(ITopoArray<T> topoArray, int scale)
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
    }
}
