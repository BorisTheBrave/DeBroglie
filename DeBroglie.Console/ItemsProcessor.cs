using DeBroglie.Console.Config;
using DeBroglie.Console.Export;
using DeBroglie.Console.Import;
using DeBroglie.Constraints;
using DeBroglie.MagicaVoxel;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DeBroglie.Console
{

    public class ItemsProcessor
    {
        private readonly ISampleSetImporter loader;
        private readonly DeBroglieConfig config;
        private IDictionary<string, Tile> tilesByName;

        public ItemsProcessor(ISampleSetImporter loader, DeBroglieConfig config)
        {
            this.loader = loader;
            this.config = config;
        }

        private SampleSet LoadSample()
        {
            if (config.Src == null)
                throw new Exception("Src sample should be set");
            var filename = Path.Combine(config.BaseDirectory, config.Src);
            System.Console.WriteLine($"Reading {filename}");

            var sampleSet = loader.Load(filename);
            sampleSet.Samples = sampleSet.Samples
                .Select(x => x.WithPeriodic(
                    config.PeriodicInputX,
                    config.PeriodicInputY,
                    config.PeriodicInputZ))
                    .ToArray();
            return sampleSet;
        }

        private SampleSet LoadFileSet()
        {
            if (config.Tiles == null)
                throw new Exception($"You must specify tile data when using SrcType {config.SrcType}.");

            var filenames = new Dictionary<Tile, string>();
            foreach (var tile in config.Tiles)
            {
                if (tile.Value == null)
                {
                    tile.Value = new Guid().ToString();
                }
                if (tile.Src == null)
                    throw new Exception($"All tiles must have a src set when using SrcType {config.SrcType}.");
                filenames[Parse(tile.Value)] = tile.Src;
            }

            if(filenames.Count == 0)
                throw new Exception($"Must supply at least one tile when using SrcType {config.SrcType}.");

            if (config.SrcType == SrcType.BitmapSet)
            {
                var bitmaps = filenames.ToDictionary(x => x.Key, x => new Bitmap(Path.Combine(config.BaseDirectory, x.Value)));
                var first = bitmaps.First().Value;
                return new SampleSet
                {
                    Directions = DirectionSet.Cartesian2d,
                    Samples = new ITopoArray<Tile>[0],
                    ExportOptions = new BitmapSetExportOptions
                    {
                        Bitmaps = bitmaps,
                        TileWidth = first.Width,
                        TileHeight = first.Height,
                    }
                };
            }
            else if(config.SrcType == SrcType.VoxSet)
            {
                var subtiles = filenames.ToDictionary(x => x.Key, x => VoxUtils.Load(Path.Combine(config.BaseDirectory, x.Value)));
                var first = VoxUtils.ToTopoArray(subtiles.First().Value);
                return new SampleSet
                {
                    Directions = DirectionSet.Cartesian3d,
                    Samples = new ITopoArray<Tile>[0],
                    ExportOptions = new VoxSetExportOptions
                    {
                        Template = subtiles.First().Value,
                        SubTiles = subtiles,
                        TileWidth = first.Topology.Width,
                        TileHeight = first.Topology.Height,
                        TileDepth = first.Topology.Depth,
                    }
                };
            }
            else
            {
                throw new NotImplementedException($"Unrecognized src type {config.SrcType}");
            }
        }

        private Direction ParseDirection(string s)
        {
            switch (s.ToLower())
            {
                case "x+": return Direction.XPlus;
                case "x-": return Direction.XMinus;
                case "y+": return Direction.YPlus;
                case "y-": return Direction.YMinus;
                case "z+": return Direction.ZPlus;
                case "z-": return Direction.ZMinus;
                case "w+": return Direction.WPlus;
                case "w-": return Direction.WMinus;
            }

            if (!Enum.TryParse(s, true, out Direction r))
            {
                throw new Exception($"Unable to parse direction \"{s}\"");
            }
            return r;
        }

        private Axis ParseAxis(string s)
        {
            if(!Enum.TryParse(s, true, out Axis r))
            {
                throw new Exception($"Unable to parse axis \"{s}\"");
            }
            return r;
        }

        private Tile Parse(string s)
        {
            if(s.Contains("!"))
            {
                // TODO: Cleanup and validate
                var a = s.Split('!');
                var b = a[1];
                var refl = false;
                if(b.StartsWith("x"))
                {
                    refl = true;
                    b = b.Substring(1);
                }
                var rotateCw = (int.Parse(b) + 360) % 360;
                return new Tile(new RotatedTile
                {
                    Tile = Parse(a[0]),
                    Rotation = new Rotation(rotateCw, refl),
                });
            }

            if (tilesByName.TryGetValue(s, out var tile))
            {
                return tile;
            }
            if (loader != null)
            {
                return loader.Parse(s);
            }
            else
            {
                return new Tile(s);
            }
        }

        private static TileModel GetModel(DeBroglieConfig config, DirectionSet directions, ITopoArray<Tile>[] samples, TileRotation tileRotation)
        {
            var modelConfig = config.Model ?? new Adjacent();
            if (modelConfig is Overlapping overlapping)
            {
                var model = new OverlappingModel(overlapping.NX, overlapping.NY, overlapping.NZ);
                foreach (var sample in samples)
                {
                    model.AddSample(sample, tileRotation);
                }
                return model;
            }
            else if(modelConfig is Adjacent adjacent)
            {
                var model = new AdjacentModel(directions);
                foreach (var sample in samples)
                {
                    model.AddSample(sample, tileRotation);
                }
                return model;
            }
            throw new System.Exception($"Unrecognized model type {modelConfig.GetType()}");
        }

        private List<ITileConstraint> GetConstraints(bool is3d, TileRotation tileRotation)
        {
            var constraints = new List<ITileConstraint>();
            if (config.Ground != null)
            {
                var groundTile = Parse(config.Ground);
                constraints.Add(new BorderConstraint
                {
                    Sides = is3d ? BorderSides.ZMin : BorderSides.YMax,
                    Tiles = new[] { groundTile },
                });
                constraints.Add(new BorderConstraint
                {
                    Sides = is3d ? BorderSides.ZMin : BorderSides.YMax,
                    Tiles = new[] { groundTile },
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
                        var tiles = new HashSet<Tile>(pathData.Tiles.Select(Parse));
                        var p = new PathConstraint(tiles, pathData.EndPoints);
                        constraints.Add(p);
                    }
                    if (constraint is EdgedPathConfig edgedPathData)
                    {
                        var exits = edgedPathData.Exits.ToDictionary(
                            kv => Parse(kv.Key), x => (ISet<Direction>)new HashSet<Direction>(x.Value.Select(ParseDirection)));
                        var p = new EdgedPathConstraint(exits, edgedPathData.EndPoints, tileRotation);
                        constraints.Add(p);
                    }
                    else if (constraint is BorderConfig borderData)
                    {
                        var tiles = borderData.Tiles.Select(Parse).ToArray();
                        var sides = borderData.Sides == null ? BorderSides.All : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.Sides, true);
                        var excludeSides = borderData.ExcludeSides == null ? BorderSides.None : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.ExcludeSides, true);
                        if (!is3d)
                        {
                            sides = sides & ~BorderSides.ZMin & ~BorderSides.ZMax;
                            excludeSides = excludeSides & ~BorderSides.ZMin & ~BorderSides.ZMax;
                        }
                        constraints.Add(new BorderConstraint
                        {
                            Tiles = tiles,
                            Sides = sides,
                            ExcludeSides = excludeSides,
                            InvertArea = borderData.InvertArea,
                            Ban = borderData.Ban,
                        });
                    }
                    else if (constraint is FixedTileConfig fixedTileConfig)
                    {
                        constraints.Add(new FixedTileConstraint
                        {
                            Tiles = fixedTileConfig.Tiles.Select(Parse).ToArray(),
                            Point = fixedTileConfig.Point,
                        });
                    }
                    else if (constraint is MaxConsecutiveConfig maxConsecutiveConfig)
                    {
                        var axes = maxConsecutiveConfig.Axes?.Select(ParseAxis);
                        constraints.Add(new MaxConsecutiveConstraint
                        {
                            Tiles = new HashSet<Tile>(maxConsecutiveConfig.Tiles.Select(Parse)),
                            MaxCount = maxConsecutiveConfig.MaxCount,
                            Axes = axes == null ? null : new HashSet<Axis>(axes),
                        });
                    }else if (constraint is MirrorConfig mirrorConfig)
                    {
                        constraints.Add(new MirrorConstraint
                        {
                            TileRotation = tileRotation,
                        });
                    }
                }
            }

            return constraints;
        }

        public void ProcessItem()
        {
            if (config.Dest == null)
            {
                throw new System.Exception("dest attribute must be set");
            }

            var directory = config.BaseDirectory;

            var dest = Path.Combine(directory, config.Dest);
            var contdest = Path.ChangeExtension(dest, ".contradiction" + Path.GetExtension(dest));

            // TODO: Neat way to do this without mutability?
            tilesByName = new Dictionary<string, Tile>();

            SampleSet sampleSet;
            if (config.SrcType == SrcType.Sample)
            {
                sampleSet = LoadSample();
                tilesByName = sampleSet.TilesByName ?? tilesByName;
            }
            else
            {
                sampleSet = LoadFileSet();
            }
            var directions = sampleSet.Directions;
            var samples = sampleSet.Samples;

            var is3d = directions.Type == DirectionSetType.Cartesian3d;
            var topology = new Topology(directions, config.Width, config.Height, is3d ? config.Depth : 1, config.PeriodicX, config.PeriodicY, config.PeriodicZ);

            var tileRotation = GetTileRotation(config.Tiles, config.RotationTreatment, topology);

            var model = GetModel(config, directions, samples, tileRotation);

            // Setup adjacencies
            if(config.Adjacencies != null)
            {
                var adjacentModel = model as AdjacentModel;
                if(adjacentModel == null)
                {
                    throw new Exception("Setting adjacencies is only supported for the \"adjacent\" model.");
                }

                foreach(var a in config.Adjacencies)
                {
                    var srcAdj = a.Src.Select(Parse).Select(tileRotation.Canonicalize).ToList();
                    var destAdj = a.Dest.Select(Parse).Select(tileRotation.Canonicalize).ToList();
                    adjacentModel.AddAdjacency(srcAdj, destAdj, a.X, a.Y, a.Z, tileRotation);
                }

                // If there are no samples, set frequency to 1 for everything mentioned in this block
                foreach (var tile in adjacentModel.Tiles)
                {
                    adjacentModel.SetFrequency(tile, 1, tileRotation);
                }
            }

            // Setup tiles
            if(config.Tiles != null)
            {
                foreach (var tile in config.Tiles)
                {
                    var value = Parse(tile.Value);
                    if(tile.MultiplyFrequency != null)
                    {
                        var cf = tile.MultiplyFrequency.Trim();
                        double cfd;
                        if(cf.EndsWith("%"))
                        {
                            cfd = double.Parse(cf.TrimEnd('%')) / 100;
                        }
                        else
                        {
                            cfd = double.Parse(cf);
                        }
                        model.MultiplyFrequency(value, cfd, tileRotation);
                    }
                }
            }

            // Setup constraints
            var constraints = GetConstraints(is3d, tileRotation);

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
                    status = RunAnimate(model, propagator, dest, sampleSet.ExportOptions);
                }
                else
                {
                    status = Run(propagator);
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
                Exporter.Export(model, propagator, dest, config, sampleSet.ExportOptions);
                File.Delete(contdest);
            }
            else
            {
                System.Console.WriteLine($"Writing {contdest}");
                Exporter.Export(model, propagator, contdest, config, sampleSet.ExportOptions);
                File.Delete(dest);
            }
        }

        private Resolution RunAnimate(TileModel model, TilePropagator propagator, string dest, ExportOptions exportOptions)
        {
            if(!config.Animate)
            {
                return Run(propagator);
            }
            // Animate is true - we run the propagator, and export after every step
            Resolution status = Resolution.Undecided;
            var allFiles = new List<string>();
            int i = 0;
            while(true)
            {
                status = propagator.Step();
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                var currentDest = Path.ChangeExtension(dest, i + Path.GetExtension(dest));
                allFiles.Add(currentDest);
                Exporter.Export(model, propagator, currentDest, config, exportOptions);
                i++;
                if (status != Resolution.Undecided) return status;
            }
        }

        private Resolution Run(TilePropagator propagator)
        {
            var next = DateTime.Now + TimeSpan.FromMinutes(1);
            while (true)
            {
                for (var i = 0; i < 100; i++)
                {
                    var status = propagator.Step();
                    if (status != Resolution.Undecided) return status;
                }
                if(DateTime.Now > next)
                {
                    System.Console.WriteLine($"Progress {propagator.GetProgress():p2}");
                    next = DateTime.Now + TimeSpan.FromMinutes(1);
                }

            }
        }

        private TileRotation GetTileRotation(List<TileData> tileData, TileRotationTreatment? rotationTreatment, Topology topology)
        {
            var tileRotationBuilder = new TileRotationBuilder(config.RotationalSymmetry, config.ReflectionalSymmetry, rotationTreatment ?? TileRotationTreatment.Unchanged);
            var rotationGroup = tileRotationBuilder.RotationGroup;

            // Setup tiles
            if (tileData != null)
            {
                foreach (var td in tileData)
                {
                    var tile = Parse(td.Value);
                    if(td.TileSymmetry != null)
                    {
                        var ts = TileSymmetryUtils.Parse(td.TileSymmetry);
                        tileRotationBuilder.AddSymmetry(tile, ts);
                    }
                    if (td.ReflectX != null)
                    {
                        tileRotationBuilder.Add(tile, new Rotation(0, true), Parse(td.ReflectX));
                    }
                    if (td.ReflectY != null)
                    {
                        tileRotationBuilder.Add(tile, new Rotation(180, true), Parse(td.ReflectY));
                    }
                    if (td.RotateCw != null)
                    {
                        tileRotationBuilder.Add(tile, new Rotation(rotationGroup.SmallestAngle, false), Parse(td.RotateCw));
                    }
                    if (td.RotateCcw != null)
                    {
                        tileRotationBuilder.Add(tile, new Rotation(360 - rotationGroup.SmallestAngle, false), Parse(td.RotateCcw));
                    }
                    if(td.RotationTreatment != null)
                    {
                        tileRotationBuilder.SetTreatment(tile, td.RotationTreatment.Value);
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
                var errors = new List<string>();

                serializer.Error += (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args) =>
                {
                    // only log an error once
                    if (args.CurrentObject == args.ErrorContext.OriginalObject)
                    {
                        errors.Add(args.ErrorContext.Error.Message);
                    }
                };
                var result = serializer.Deserialize<DeBroglieConfig>(jsonReader);
                if(errors.Count != 0)
                {
                    // TODO: Better diagnostics
                    throw new Exception(errors[0]);
                }
                if (result == null)
                    throw new Exception($"{filename} is empty.");
                return result;
            }
        }

        public static void Process(string filename)
        {
            var directory = Path.GetDirectoryName(filename);
            if (directory == "")
                directory = ".";
            var config = LoadItemsFile(filename);
            config.BaseDirectory = config.BaseDirectory == null ? directory : Path.Combine(directory, config.BaseDirectory);
            var importer = Importer.GetImporter(config.Src);

            var processor = new ItemsProcessor(importer, config);
            processor.ProcessItem();
        }
    }
}
