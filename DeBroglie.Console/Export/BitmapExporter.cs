using DeBroglie.Console.Config;
using DeBroglie.Models;
using DeBroglie.Topo;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Console.Export
{

    public class BitmapExporter : IExporter
    {
        public static Rgba32 WeightedColorAverage(IEnumerable<KeyValuePair<Tile, double>> tiles)
        {
            double alpha = 0;
            double red = 0;
            double green = 0;
            double blue = 0;
            double n = 0;
            foreach (var kv in tiles)
            {
                var color = (Rgba32)kv.Key.Value;
                var frequency = kv.Value;
                alpha += color.A * frequency;
                red += color.R * frequency;
                green += color.G * frequency;
                blue += color.B * frequency;
                n += frequency;
            }
            if (n == 0)
            {
                return Color.Transparent.ToPixel<Rgba32>();
            }
            else
            {
                return new Rgba32(
                    (byte)(red / n),
                    (byte)(green / n),
                    (byte)(blue / n),
                    (byte)(alpha / n));
            }
        }

        public void Export(TileModel model, TilePropagator propagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            if (config.Animate)
            {
                if (exportOptions is BitmapExportOptions)
                {
                    var topoArray = propagator.ToWeightedArraySets().Map(WeightedColorAverage);
                    var bitmap = BitmapUtils.ToBitmap(topoArray.ToArray2d());
                    bitmap.Save(filename);
                }
                else if (exportOptions is BitmapSetExportOptions bseo)
                {

                    var topoArray = propagator.ToWeightedArraySets();
                    var tileTopology = topoArray.Topology.AsGridTopology().WithSize(bseo.TileWidth, bseo.TileHeight, 1);
                    var subTiles = bseo.Bitmaps.ToDictionary(x => x.Key, x => TopoArray.Create(BitmapUtils.ToColorArray(x.Value), tileTopology).Map(c => new Tile(c)));
                    var exploded = MoreTopoArrayUtils.ExplodeWeightedTiles(topoArray, subTiles, bseo.TileWidth, bseo.TileHeight, 1).Map(WeightedColorAverage);
                    var bitmap = BitmapUtils.ToBitmap(exploded.ToArray2d());
                    bitmap.Save(filename);
                }
                else
                {
                    throw new System.Exception($"Cannot export from {exportOptions.TypeDescription} to bitmap.");
                }

            }
            else
            {
                if (exportOptions is BitmapExportOptions)
                {
                    
                    
                    var topoArray = propagator.ToValueArray(Color.Gray.ToPixel<Rgba32>(), Color.Magenta.ToPixel<Rgba32>());
                    var bitmap = BitmapUtils.ToBitmap(topoArray.ToArray2d());
                    bitmap.Save(filename);
                }
                else if (exportOptions is BitmapSetExportOptions bseo)
                {
                    var undecided = new Tile(new object());
                    var contradiction = new Tile(new object());
                    var topoArray = propagator.ToArray(undecided, contradiction);

                    var tileTopology = topoArray.Topology.AsGridTopology().WithSize(bseo.TileWidth, bseo.TileHeight, 1);
                    var subTiles = bseo.Bitmaps.ToDictionary(x => x.Key, x => TopoArray.Create(BitmapUtils.ToColorArray(x.Value), tileTopology));
                    subTiles[undecided] = TopoArray.FromConstant(Color.Gray.ToPixel<Rgba32>(), tileTopology);
                    subTiles[contradiction] = TopoArray.FromConstant(Color.Magenta.ToPixel<Rgba32>(), tileTopology);

                    var exploded = MoreTopoArrayUtils.ExplodeTiles(topoArray, subTiles, bseo.TileWidth, bseo.TileHeight, 1);
                    var bitmap = BitmapUtils.ToBitmap(exploded.ToArray2d());
                    bitmap.Save(filename);
                }
                else
                {
                    throw new System.Exception($"Cannot export from {exportOptions.TypeDescription} to bitmap.");
                }
            }
        }


    }
}
