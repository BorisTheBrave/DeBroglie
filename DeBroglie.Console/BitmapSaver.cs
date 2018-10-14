using DeBroglie.Console.Export;
using DeBroglie.Models;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DeBroglie.Console
{

    public class BitmapSaver : ISampleSetSaver
    { 
        public void Save(TileModel model, TilePropagator propagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            if (config.Animate)
            {
                if (exportOptions is BitmapExportOptions)
                {
                    var topoArray = propagator.ToValueSets<Color>().Map(BitmapUtils.ColorAverage);
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
                    var topoArray = propagator.ToValueArray(Color.Gray, Color.Magenta);
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
                    subTiles[undecided] = TopoArray.FromConstant(Color.Gray, tileTopology);
                    subTiles[contradiction] = TopoArray.FromConstant(Color.Magenta, tileTopology);

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
