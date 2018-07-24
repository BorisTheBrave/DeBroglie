using DeBroglie.Constraints;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DeBroglie.Console
{
    public abstract class ItemsProcessor
    {
        protected abstract ITopArray<Tile> Load(string filename, DeBroglieConfig config);

        protected abstract void Save(TileModel model, TilePropagator tilePropagator, string filename);

        protected abstract Tile Parse(string s);

        private static TileModel GetModel(DeBroglieConfig config, ITopArray<Tile> sample, TileRotation tileRotation)
        {
            var modelConfig = config.Model;
            if (modelConfig is Overlapping overlapping)
            {
                var symmetries = overlapping.Symmetry;
                var model = new OverlappingModel(overlapping.N);
                model.AddSample(sample, symmetries > 1 ? symmetries / 2 : 1, symmetries > 1, tileRotation);
                return model;
            }
            else if(modelConfig is Adjacent adjacent)
            {
                return new AdjacentModel(sample);
            }
            throw new System.Exception($"Unrecognized model type {modelConfig.GetType()}");
        }

        public void ProcessItem(DeBroglieConfig config, string directory)
        {
            if (config.Src == null)
            {
                throw new System.Exception("src attribute must be set");
            }

            if (config.Dest == null)
            {
                throw new System.Exception("dest attribute must be set");
            }

            var src = Path.Combine(directory, config.Src);
            var dest = Path.Combine(directory, config.Dest);
            var contdest = Path.ChangeExtension(dest, ".contradiction" + Path.GetExtension(dest));
            System.Console.WriteLine($"Reading {src}");

            var topArray = Load(src, config);

            var is3d = topArray.Topology.Directions.Type == DirectionsType.Cartesian3d;
            var topology = new Topology(topArray.Topology.Directions, config.Width, config.Height, is3d ? config.Depth : 1, config.IsPeriodic);

            var tileRotation = GetTileRotation(config.Tiles, topology);

            var model = GetModel(config, topArray, tileRotation);

            // Setup tiles
            if(config.Tiles != null)
            {
                foreach (var tile in config.Tiles)
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
            if (config.Model is Overlapping overlapping && overlapping.Ground != 0)
                waveConstraints.Add(((OverlappingModel)model).GetGroundConstraint());
            var constraints = new List<ITileConstraint>();

            if (config.Constraints != null)
            {
                foreach (var constraint in config.Constraints)
                {
                    if (constraint is PathConfig pathData)
                    {
                        var pathTiles = new HashSet<Tile>(pathData.PathTiles.Select(Parse));
                        var p = new PathConstraint(pathTiles);
                        constraints.Add(p);
                    }
                    else if (constraint is BorderConfig borderData)
                    {
                        var tile = Parse(borderData.Tile);
                        var sides = borderData.Sides == null ? BorderSides.All : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.Sides, true);
                        var excludeSides = borderData.ExcludeSides == null ? BorderSides.None : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.ExcludeSides, true);
                        if (!is3d)
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
            }

            System.Console.WriteLine($"Processing {dest}");
            var propagator = new TilePropagator(model, topology, config.Backtrack,
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

        private static DeBroglieConfig LoadItemsFile(string filename)
        {
            var serializer = new JsonSerializer();
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
            using (var fs = new FileStream(filename, FileMode.Open))
            using(var tr = new StreamReader(fs))
            using (var jsonReader = new JsonTextReader(tr))
            {
                return serializer.Deserialize<DeBroglieConfig>(jsonReader);
            }
        }

        public static void Process(string filename)
        {
            var directory = Path.GetDirectoryName(filename);
            var config = LoadItemsFile(filename);
            if (config.Src.EndsWith(".png"))
            {
                var processor = new BitmapItemsProcessor();
                processor.ProcessItem(config, directory);
            }
            else if (config.Src.EndsWith(".tmx"))
            {
                var processor = new TiledItemsProcessor();
                processor.ProcessItem(config, directory);
            }
            else if (config.Src.EndsWith(".vox"))
            {
                var processor = new VoxItemsProcessor();
                processor.ProcessItem(config, directory);
            }
            else
            {
                throw new System.Exception($"Unrecongized extenion for {config.Src}");
            }
        }
    }
}
