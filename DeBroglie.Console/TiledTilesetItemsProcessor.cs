using DeBroglie.Topo;
using System.Collections.Generic;
using TiledLib;

namespace DeBroglie.Console
{
    public class TiledTilesetItemsProcessor : TiledItemsProcessor
    {
        protected override void LoadSamples(string src, DeBroglieConfig config, out Directions directions, out ITopoArray<Tile>[] samples)
        {
            srcFilename = src;
            // Hack for tsx files. Should handle this more properly in future
            var tileset = TiledUtil.LoadTileset(src);
            tileset.FirstGid = 1;
            map = new Map
            {
                CellWidth = tileset.TileWidth,
                CellHeight = tileset.TileHeight,
                Tilesets = new[] { tileset },
                TiledVersion = "1.1.6",
                RenderOrder = RenderOrder.rightdown,
            };
            tilesByName = new Dictionary<string, int>();
            AddTileset(tileset);
            // TODO: Other directions
            directions = Directions.Cartesian2d;
            map.Orientation = Orientation.orthogonal;
            samples = new ITopoArray<Tile>[0];
        }
    }
}
