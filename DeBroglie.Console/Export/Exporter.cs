using DeBroglie.Console.Config;
using DeBroglie.Models;
using System;
using System.Collections.Generic;
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

    }
}
