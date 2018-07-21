using System;
using System.Collections.Generic;
using System.IO;
using TiledLib.Layer;

namespace DeBroglie.Console
{
    public class TiledItemsProcessor : ItemsProcessor
    {
        private TiledLib.Map map;
        private string srcFilename;
        private IDictionary<string, int> tilesByName;

        protected override ITopArray<Tile> Load(string filename, Item item)
        {
            srcFilename = filename;
            map = TiledUtil.Load(filename);
            var layer = (TileLayer)map.Layers[0];
            tilesByName = new Dictionary<string, int>();
            foreach(var tileset in map.Tilesets)
            {
                if (tileset.TileProperties == null)
                    continue;
                foreach (var kv in tileset.TileProperties)
                {
                    var localTileId = kv.Key;
                    var properties = kv.Value;
                    if(properties.TryGetValue("name", out var name))
                    {
                        tilesByName[name] = tileset.FirstGid + localTileId;
                    }
                }
            }
            return TiledUtil.ReadLayer(map, layer).ToTiles();
        }

        protected override Tile Parse(string s)
        {
            int tileId;
            if(tilesByName.TryGetValue(s, out tileId))
            {
                return new Tile(tileId);
            }
            if(int.TryParse(s, out tileId))
            {
                return new Tile(tileId);
            }
            throw new Exception($"Found no tile named {s}, either set the \"name\" property or use tile gids.");
        }

        protected override void Save(TileModel model, TilePropagator tilePropagator, string filename)
        {
            var layerArray = tilePropagator.ToValueArray<int>();
            var layer = TiledUtil.WriteLayer(map, layerArray);
            map.Layers = new[] { layer };
            map.Width = layer.Width;
            map.Height = layer.Height;
            TiledUtil.Save(filename, map);

            // Check for any external files that may also need copying
            foreach(var tileset in map.Tilesets)
            {
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
