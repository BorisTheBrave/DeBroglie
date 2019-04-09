using DeBroglie.Console.Config;
using DeBroglie.Models;
using DeBroglie.Topo;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;

namespace DeBroglie.Console.Export
{

    public class BitmapExporter : IExporter
    { 
        public void Export(TileModel model, TilePropagator propagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            if (config.Animate)
            {
                if (exportOptions is BitmapExportOptions)
                {
                    var topoArray = propagator.ToValueSets<Rgba32>().Map(BitmapUtils.ColorAverage);
                    var bitmap = BitmapUtils.ToBitmap(topoArray.ToArray2d());
                    bitmap.Save(filename);
                }
                else if (exportOptions is BitmapSetExportOptions bseo)
                {

                    var topoArray = propagator.ToArraySets();
                    var tileTopology = topoArray.Topology.WithSize(bseo.TileWidth, bseo.TileHeight, 1);
                    var subTiles = bseo.Bitmaps.ToDictionary(x => x.Key, x => TopoArray.Create(BitmapUtils.ToColorArray(x.Value), tileTopology));
                    var exploded = MoreTopoArrayUtils.ExplodeTileSets(topoArray, subTiles, bseo.TileWidth, bseo.TileHeight, 1).Map(BitmapUtils.ColorAverage);
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
                    var topoArray = propagator.ToValueArray(Rgba32.Gray, Rgba32.Magenta);
                    var bitmap = BitmapUtils.ToBitmap(topoArray.ToArray2d());
                    bitmap.Save(filename);
                }
                else if (exportOptions is BitmapSetExportOptions bseo)
                {
                    var undecided = new Tile(new object());
                    var contradiction = new Tile(new object());
                    var topoArray = propagator.ToArray(undecided, contradiction);

                    var tileTopology = topoArray.Topology.WithSize(bseo.TileWidth, bseo.TileHeight, 1);
                    var subTiles = bseo.Bitmaps.ToDictionary(x => x.Key, x => TopoArray.Create(BitmapUtils.ToColorArray(x.Value), tileTopology));
                    subTiles[undecided] = TopoArray.FromConstant(Rgba32.Gray, tileTopology);
                    subTiles[contradiction] = TopoArray.FromConstant(Rgba32.Magenta, tileTopology);

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
