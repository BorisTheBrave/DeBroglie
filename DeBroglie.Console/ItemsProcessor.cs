using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DeBroglie.Console
{
    public abstract class ItemsProcessor
    {
        protected abstract ITopArray<Tile> Load(string filename, Item item);

        protected abstract void Save(TileModel model, TilePropagator tilePropagator, string filename);

        protected abstract Tile Parse(string s);

        private static TileModel GetModel(Item item, ITopArray<Tile> sample, TileRotation tileRotation)
        {
            if (item is Overlapping overlapping)
            {
                var symmetries = overlapping.Symmetry;
                var model = new OverlappingModel(overlapping.N);
                model.AddSample(sample, symmetries > 1 ? symmetries / 2 : 1, symmetries > 1, tileRotation);
                return model;
            }
            else if(item is Adjacent adjacent)
            {
                return new AdjacentModel(sample);
            }
            throw new System.Exception($"Unrecognized model type {item.GetType()}");
        }

        public void ProcessItem(Item item, string directory)
        {
            if(item is SimpleTiled)
            {
                // Not supported atm
                return;
            }
            if (item.Src == null)
            {
                throw new System.Exception("src attribute must be set");
            }

            if (item.Dest == null)
            {
                throw new System.Exception("dest attribute must be set");
            }

            var src = Path.Combine(directory, item.Src);
            var dest = Path.Combine(directory, item.Dest);
            var contdest = Path.ChangeExtension(dest, ".contradiction" + Path.GetExtension(dest));
            System.Console.WriteLine($"Reading {src}");

            var topArray = Load(src, item);

            var is3d = topArray.Topology.Directions.Type == DirectionsType.Cartesian3d;
            var topology = new Topology(topArray.Topology.Directions, item.Width, item.Height, is3d ? item.Depth : 1, item.IsPeriodic);

            var tileRotation = GetTileRotation(item.Tiles, topology);

            var model = GetModel(item, topArray, tileRotation);

            // Setup tiles
            if(item.Tiles != null)
            {
                foreach (var tile in item.Tiles)
                {
                    var value = Parse(tile.Value);
                    if(tile.ChangeFrequency != null)
                    {
                        var cf = tile.ChangeFrequency.Trim();
                        double cfd;
                        if(cf.EndsWith("%"))
                        {
                            cfd = double.Parse(cf.TrimEnd('%')) / 100;
                        }
                        else
                        {
                            cfd = double.Parse(cf);
                        }
                        model.ChangeFrequency(value, cfd);
                    }
                }
            }

            // Setup constraints
            var waveConstraints = new List<IWaveConstraint>();
            if (item is Overlapping overlapping && overlapping.Ground != 0)
                waveConstraints.Add(((OverlappingModel)model).GetGroundConstraint());
            var constraints = new List<ITileConstraint>();
            if (is3d)
            {
                constraints.Add(new BorderConstraint
                {
                    Sides=BorderSides.ZMin,
                    Tile=new Tile(255),
                });
                constraints.Add(new BorderConstraint
                {
                    Sides = BorderSides.All,
                    ExcludeSides = BorderSides.ZMin,
                    Tile = new Tile(0),
                });
            }

            foreach (var constraint in item.Constraints)
            {
                if(constraint is PathData pathData)
                {
                    var pathTiles = new HashSet<Tile>(pathData.PathTiles.Select(Parse));
                    var p = new PathConstraint(pathTiles);
                    constraints.Add(p);
                }
                else if(constraint is BorderData borderData)
                {
                    var tile = Parse(borderData.Tile);
                    var sides = borderData.Sides == null ? BorderSides.All : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.Sides, true);
                    var excludeSides = borderData.ExcludeSides == null ? BorderSides.None : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.ExcludeSides, true);
                    if(!is3d)
                    {
                        sides = sides & ~BorderSides.ZMin & ~BorderSides.ZMax;
                        excludeSides = excludeSides & ~BorderSides.ZMin & ~BorderSides.ZMax;
                    }
                    constraints.Add(new BorderConstraint
                    {
                        Tile = tile,
                        Sides = sides,
                        ExcludeSides = excludeSides,
                    });
                }
            }


            System.Console.WriteLine($"Processing {dest}");
            var propagator = new TilePropagator(model, topology, item.Backtrack,
                constraints: constraints.ToArray(),
                waveConstraints: waveConstraints.ToArray());
            CellStatus status = CellStatus.Contradiction;
            for (var retry = 0; retry < 5; retry++)
            {
                if (retry != 0)
                    propagator.Clear();
                status = propagator.Run();
                if (status == CellStatus.Decided)
                {
                    break;
                }
                System.Console.WriteLine($"Found contradiction, retrying");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            if (status == CellStatus.Decided)
            {
                System.Console.WriteLine($"Writing {dest}");
                Save(model, propagator, dest);
                File.Delete(contdest);
            }
            else
            {
                System.Console.WriteLine($"Writing {contdest}");
                Save(model, propagator, contdest);
                File.Delete(dest);
            }
        }

        private TileRotation GetTileRotation(List<TileData> tileData, Topology topology)
        {

            var tileRotationBuilder = new TileRotationBuilder();

            // Setup tiles
            if (tileData != null)
            {
                foreach (var tile in tileData)
                {
                    var value = Parse(tile.Value);
                    if (tile.ReflectX != null)
                    {
                        tileRotationBuilder.Add(value, 0, true, Parse(tile.ReflectX));
                    }
                    if (tile.ReflectY != null)
                    {
                        tileRotationBuilder.Add(value, topology.Directions.Count / 2, true, Parse(tile.ReflectY));
                    }
                    if (tile.RotateCw != null)
                    {
                        tileRotationBuilder.Add(value, 1, false, Parse(tile.RotateCw));
                    }
                    if (tile.RotateCcw != null)
                    {
                        tileRotationBuilder.Add(value, -1, false, Parse(tile.RotateCcw));
                    }
                    if(tile.NoRotate)
                    {
                        tileRotationBuilder.NoRotate(value);
                    }
                }
            }

            return tileRotationBuilder.Build();
        }

        private static Items LoadItemsFile(string filename)
        {
            var serializer = new XmlSerializer(typeof(Items));
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                return (Items)serializer.Deserialize(fs);
            }
        }

        public static void Process(string filename)
        {
            var directory = Path.GetDirectoryName(filename);
            var items = LoadItemsFile(filename);
            foreach (var item in items.AllItems)
            {
                if (item is SimpleTiled)
                {
                    // Not supported atm
                    continue;
                }
                if (item.Src.EndsWith(".png"))
                {
                    var processor = new BitmapItemsProcessor();
                    processor.ProcessItem(item, directory);
                }
                else if (item.Src.EndsWith(".tmx"))
                {
                    var processor = new TiledItemsProcessor();
                    processor.ProcessItem(item, directory);
                }
                else if (item.Src.EndsWith(".vox"))
                {
                    var processor = new VoxItemsProcessor();
                    processor.ProcessItem(item, directory);
                }
                else
                {
                    throw new System.Exception($"Unrecongized extenion for {item.Src}");
                }
            }
        }
    }
}
