using DeBroglie.Console.Config;
using DeBroglie.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeBroglie.Console.Export
{
    public static class Exporter
    {
        public static void Export(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            var exporter = GetExporter(filename);
            // Handle conversions
            if(exporter is BitmapExporter && exportOptions is TiledExportOptions)
            {
                if (tilePropagator.Topology.Directions.Type != Topo.DirectionSetType.Cartesian2d)
                    throw new NotSupportedException("Converting from Tiled format to bitmaps only supported for square grids.");

                exportOptions = ConvertToBitmaps(exportOptions as TiledExportOptions);
            }

            exporter.Export(model, tilePropagator, filename, config, exportOptions);
        }

        private static IExporter GetExporter(string filename)
        {
            if (filename == null)
            {
                throw new Exception("dest should be provided.");
            }
            else if (filename.EndsWith(".png"))
            {
                return new BitmapExporter();
            }
            else if (filename.EndsWith(".tmx"))
            {
                return new TiledMapExporter();
            }
            else if (filename.EndsWith(".tsx"))
            {
                return new TiledMapExporter();
            }
            else if (filename.EndsWith(".vox"))
            {
                return new MagicaVoxelExporter();
            }
            else if (filename.EndsWith(".csv"))
            {
                return new CsvExporter();
            }
            else
            {
                throw new System.Exception($"Saving {Path.GetExtension(filename)} files not supported.");
            }
        }

        private static BitmapSetExportOptions ConvertToBitmaps(TiledExportOptions teo)
        {
            var bseo = new BitmapSetExportOptions();
            bseo.TileWidth = teo.Template.CellWidth;
            bseo.TileHeight = teo.Template.CellHeight;
            bseo.Bitmaps = new Dictionary<Tile, Image<Rgba32>>();
            var basePath = Path.GetDirectoryName(teo.SrcFileName);
            foreach (var tileset in teo.Template.Tilesets)
            {
                var tilesetBitmap = Image.Load(Path.Combine(basePath, tileset.ImagePath));
                for(var i=0;i<tileset.TileCount;i++)
                {
                    var gid = i + tileset.FirstGid;
                    var tile = tileset[gid];
                    var tileBitmap = BitmapUtils.Slice(tilesetBitmap, tile.Left, tile.Top, tile.Width, tile.Height);
                    bseo.Bitmaps[new Tile(gid)] = tileBitmap;
                }
            }
            return bseo;
        }

    }
}
