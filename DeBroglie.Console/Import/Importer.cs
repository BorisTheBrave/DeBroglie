using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeBroglie.Console.Import
{
    public static class Importer
    {
        public static ISampleSetImporter GetImporter(string filename)
        {
            if (filename == null)
            {
                return null;
            }
            else if (filename.EndsWith(".png"))
            {
                return new BitmapImporter();
            }
            else if (filename.EndsWith(".tmx"))
            {
                return new TiledMapImporter();
            }
            else if (filename.EndsWith(".tsx"))
            {
                return new TiledTilesetImporter();
            }
            else if (filename.EndsWith(".vox"))
            {
                return new MagicaVoxelImporter();
            }
            else
            {
                throw new System.Exception($"Loading {Path.GetExtension(filename)} files not supported.");
            }
        }
    }
}
