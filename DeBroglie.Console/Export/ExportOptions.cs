using DeBroglie.MagicaVoxel;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiledLib;

namespace DeBroglie.Console.Export
{
    public abstract class ExportOptions
    {
        public abstract string TypeDescription { get; }
    }

    public class TiledExportOptions : ExportOptions
    {
        public Map Template { get; set; }
        public string SrcFileName { get; set; }

        public override string TypeDescription => "Tiled";
    }

    public class VoxExportOptions : ExportOptions
    {
        public Vox Template { get; set; }

        public override string TypeDescription => "Vox";
    }

    public class BitmapExportOptions : ExportOptions
    {
        public override string TypeDescription => "Bitmap";
    }

    public class BitmapSetExportOptions : ExportOptions
    {
        public override string TypeDescription => "Bitmap set";

        public IDictionary<Tile, Bitmap> Bitmaps { get; set; }

        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
    }

    public class VoxSetExportOptions : ExportOptions
    {
        public override string TypeDescription => "Bitmap set";

        public IDictionary<Tile, Vox> SubTiles { get; set; }

        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int TileDepth { get; set; }
    }

}
