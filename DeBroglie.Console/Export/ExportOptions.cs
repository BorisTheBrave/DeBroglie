using DeBroglie.MagicaVoxel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
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

        public IDictionary<Tile, Image<Rgba32>> Bitmaps { get; set; }

        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
    }

    public class VoxSetExportOptions : ExportOptions
    {
        public override string TypeDescription => "Bitmap set";

        public Vox Template { get; set; }

        public IDictionary<Tile, Vox> SubTiles { get; set; }

        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int TileDepth { get; set; }
    }

}
