using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TiledLib.Layer;

namespace DeBroglie.Console
{
    public class TiledItemsProcessor : ItemsProcessor
    {
        private TiledLib.Map map;
        private string srcFilename;
        private IDictionary<string, int> tilesByName;

        protected override ITopArray<Tile> Load(string filename, DeBroglieConfig config)
        {
            srcFilename = filename;
            map = TiledUtil.Load(filename);
            // Scan tilesets for tiles with a custom property "name"
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

            // Read all the layers into a 3d array.
            var tileLayers = map.Layers
                .Where(x => x is TileLayer)
                .Cast<TileLayer>()
                .ToList();
            var results = new Tile[map.Width, map.Height, tileLayers.Count];
            Topology topology = null;
            for(var z = 0; z < tileLayers.Count;z++)
            {
                var layer = tileLayers[z];
                var layerArray = TiledUtil.ReadLayer(map, layer).ToTiles();
                topology = layerArray.Topology;
                for (var y = 0; y < layer.Height; y++)
                {
                    for (var x = 0; x < layer.Width; x++)
                    {
                        results[x, y, z] = layerArray.Get(x, y);
                    }
                }
            }
            if(tileLayers.Count > 1 && topology.Directions.Type == DirectionsType.Cartesian2d)
            {
                topology = new Topology(Directions.Cartesian3d, map.Width, map.Height, tileLayers.Count, false);
            }
            else
            {
                topology = new Topology(topology.Directions, topology.Width, topology.Height, tileLayers.Count, false);
            }
            return new TopArray3D<Tile>(results, topology);
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
            map.Layers = new BaseLayer[layerArray.Topology.Depth];
            for(var z=0;z<layerArray.Topology.Depth;z++)
            {
                map.Layers[z] = TiledUtil.MakeTileLayer(map, layerArray, z);
            }
            map.Width = layerArray.Topology.Width;
            map.Height = layerArray.Topology.Height;
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
