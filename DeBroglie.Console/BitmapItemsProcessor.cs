using DeBroglie.Models;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace DeBroglie.Console
{
    public class BitmapItemsProcessor : ItemsProcessor
    {
        protected override bool ShouldGenerateTileRotations => false;

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

        private static Color ColorAverage(ISet<Tile> tiles)
        {
            int alpha = 0;
            int red = 0;
            int green = 0;
            int blue = 0;
            int n = 0;
            foreach(var tile in tiles)
            {
                var color = (Color)tile.Value;
                alpha += color.A;
                red += color.R;
                green += color.G;
                blue += color.B;
                n += 1;
            }
            return Color.FromArgb(alpha / n, red / n, green / n, blue / n);
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
            if (config.Animate)
            {

                var topoArray = propagator.ToArraySets().Map(ColorAverage);
                var bitmap = ToBitmap(topoArray.ToArray2d());
                bitmap.Save(filename);
            }
            else
            {
                var topoArray = propagator.ToValueArray(Color.Gray, Color.Magenta);
                var bitmap = ToBitmap(topoArray.ToArray2d());
                bitmap.Save(filename);
            }
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
