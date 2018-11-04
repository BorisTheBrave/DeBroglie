using DeBroglie.Console.Export;
using DeBroglie.Tiled;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using TiledLib;

namespace DeBroglie.Console.Import
{
    public class TiledTilesetImporter : ISampleSetImporter
    {
        public SampleSet Load(string filename)
        {
            var srcFilename = filename;
            // Hack for tsx files. Should handle this more properly in future
            var tileset = TiledUtil.LoadTileset(filename);
            tileset.FirstGid = 1;
            var map = new Map
            {
                CellWidth = tileset.TileWidth,
                CellHeight = tileset.TileHeight,
                Tilesets = new[] { tileset },
                TiledVersion = "1.1.6",
                RenderOrder = RenderOrder.rightdown,
            };
            var tilesByName = new Dictionary<string, Tile>();
            TiledMapImporter.AddTileset(tilesByName, tileset);
            // TODO: Other directions
            var directions = Directions.Cartesian2d;
            map.Orientation = Orientation.orthogonal;
            var samples = new ITopoArray<Tile>[0];
            return new SampleSet
            {
                Directions = directions,
                Samples = samples,
                TilesByName = tilesByName,
                ExportOptions = new TiledExportOptions {
                    Template = map,
                    SrcFileName= srcFilename,
                },
            };
        }

        public Tile Parse(string s)
        {
            int tileId;
            if (int.TryParse(s, out tileId))
            {
                return new Tile(tileId);
            }
            throw new Exception($"Found no tile named {s}, either set the \"name\" property or use tile gids.");
        }
    }
}
