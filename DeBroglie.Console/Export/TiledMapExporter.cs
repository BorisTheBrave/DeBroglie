using DeBroglie.Console.Config;
using DeBroglie.Console.Export;
using DeBroglie.Models;
using DeBroglie.Tiled;
using System.IO;
using TiledLib;
using TiledLib.Layer;

namespace DeBroglie.Console.Export
{

    public class TiledMapExporter : IExporter
    {
        public void Export(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            var tiledExportOptions = exportOptions as TiledExportOptions;
            if(tiledExportOptions == null)
            {
                throw new System.Exception($"Cannot export from {exportOptions.TypeDescription} to .tmx");
            }

            var map = tiledExportOptions.Template;
            var srcFilename = tiledExportOptions.SrcFileName;

            var layerArray = tilePropagator.ToArray();
            map.Layers = new BaseLayer[layerArray.Topology.Depth];
            for(var z = 0; z < layerArray.Topology.Depth; z++)
            {
                map.Layers[z] = TiledUtil.MakeTileLayer(map, layerArray, z);
            }
            map.Width = map.Layers[0].Width;
            map.Height = map.Layers[0].Height;
            TiledUtil.Save(filename, map);

            // Check for any external files that may also need copying
            foreach(var tileset in map.Tilesets)
            {
                if(tileset is ExternalTileset e)
                {
                    var srcPath = Path.Combine(Path.GetDirectoryName(srcFilename), e.source);
                    var destPath = Path.Combine(Path.GetDirectoryName(filename), e.source);
                    if (File.Exists(srcPath) && !File.Exists(destPath))
                    {
                        File.Copy(srcPath, destPath);
                    }
                }
                if(tileset.ImagePath != null)
                {
                    var srcImagePath = Path.Combine(Path.GetDirectoryName(srcFilename), tileset.ImagePath);
                    var destImagePath = Path.Combine(Path.GetDirectoryName(filename), tileset.ImagePath);
                    if(File.Exists(srcImagePath) && !File.Exists(destImagePath))
                    {
                        File.Copy(srcImagePath, destImagePath);
                    }
                }
            }
        }
    }
}
