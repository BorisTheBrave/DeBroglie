using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeBroglie.Console
{
    public abstract class ItemsProcessor
    {
        protected abstract ITopoArray<Tile> Load(string filename, DeBroglieConfig config);

        protected abstract void Save(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config);

        protected abstract Tile Parse(string s);

        private static TileModel GetModel(DeBroglieConfig config, ITopoArray<Tile> sample, TileRotation tileRotation)
        {
            var modelConfig = config.Model ?? new Adjacent();
            if (modelConfig is Overlapping overlapping)
            {
                var model = new OverlappingModel(overlapping.NX, overlapping.NY, overlapping.NZ);
                model.AddSample(sample, config.RotationalSymmetry, config.ReflectionalSymmetry, tileRotation);
                return model;
            }
            else if(modelConfig is Adjacent adjacent)
            {
                var model = new AdjacentModel();
                model.AddSample(sample, config.RotationalSymmetry, config.ReflectionalSymmetry, tileRotation);
                return model;
            }
            throw new System.Exception($"Unrecognized model type {modelConfig.GetType()}");
        }

        public void ProcessItem(DeBroglieConfig config)
        {
            if (config.Src == null)
            {
                throw new System.Exception("src attribute must be set");
            }

            if (config.Dest == null)
            {
                throw new System.Exception("dest attribute must be set");
            }

            var directory = config.BaseDirectory;

            var src = Path.Combine(directory, config.Src);
            var dest = Path.Combine(directory, config.Dest);
            var contdest = Path.ChangeExtension(dest, ".contradiction" + Path.GetExtension(dest));
            System.Console.WriteLine($"Reading {src}");

            var topArray = Load(src, config);

            var is3d = topArray.Topology.Directions.Type == DirectionsType.Cartesian3d;
            var topology = new Topology(topArray.Topology.Directions, config.Width, config.Height, is3d ? config.Depth : 1, config.PeriodicX, config.PeriodicY, config.PeriodicZ);

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
            var constraints = new List<ITileConstraint>();
            if (config.Ground != null)
            {
                var groundTile = Parse(config.Ground);
                constraints.Add(new BorderConstraint
                {
                    Sides = is3d ? BorderSides.ZMin : BorderSides.YMax,
                    Tile = groundTile,
                });
                constraints.Add(new BorderConstraint
                {
                    Sides = is3d ? BorderSides.ZMin : BorderSides.YMax,
                    Tile = groundTile,
                    InvertArea = true,
                    Ban = true,
                });
            }

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
                            InvertArea = borderData.InvertArea,
                            Ban = borderData.Ban,
                        });
                    }
                }
            }

            System.Console.WriteLine($"Processing {dest}");
            var propagator = new TilePropagator(model, topology, config.Backtrack, constraints: constraints.ToArray());

            Resolution status = propagator.Status;

            for (var retry = 0; retry < 5; retry++)
            {
                if (retry != 0)
                {
                    status = propagator.Clear();
                }
                if (status == Resolution.Contradiction)
                {
                    System.Console.WriteLine($"Found contradiction in initial conditions, retrying");
                    continue;
                }
                if (config.Animate)
                {
                    status = RunAnimate(config, model, propagator, dest);
                }
                else
                {
                    status = propagator.Run();
                }
                if (status == Resolution.Contradiction)
                {
                    System.Console.WriteLine($"Found contradiction, retrying");
                    continue;
                }
                break;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            if (status == Resolution.Decided)
            {
                System.Console.WriteLine($"Writing {dest}");
                Save(model, propagator, dest, config);
                File.Delete(contdest);
            }
            else
            {
                System.Console.WriteLine($"Writing {contdest}");
                Save(model, propagator, contdest, config);
                File.Delete(dest);
            }
        }

        private Resolution RunAnimate(DeBroglieConfig config, TileModel model, TilePropagator propagator, string dest)
        {
            if(!config.Animate)
            {
                return propagator.Run();
            }
            // Animate is true - we run the propagator, and save after every step
            Resolution status = Resolution.Undecided;
            var allFiles = new List<string>();
            int i = 0;
            while(true)
            {
                status = propagator.Step();
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                var currentDest = Path.ChangeExtension(dest, i + Path.GetExtension(dest));
                allFiles.Add(currentDest);
                Save(model, propagator, currentDest, config);
                i++;
                if (status != Resolution.Undecided) return status;
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
            config.BaseDirectory = config.BaseDirectory == null ? directory : Path.Combine(directory, config.BaseDirectory);
            if(config.Src == null)
            {
                throw new Exception("src should be provided.");
            }
            else if (config.Src.EndsWith(".png"))
            {
                var processor = new BitmapItemsProcessor();
                processor.ProcessItem(config);
            }
            else if (config.Src.EndsWith(".tmx"))
            {
                var processor = new TiledItemsProcessor();
                processor.ProcessItem(config);
            }
            else if (config.Src.EndsWith(".vox"))
            {
                var processor = new VoxItemsProcessor();
                processor.ProcessItem(config);
            }
            else
            {
                throw new System.Exception($"Unrecongized extenion for {config.Src}");
            }
        }
    }
}
