using DeBroglie.Console.Config;
using DeBroglie.Console.Export;
using DeBroglie.Console.Import;
using DeBroglie.MagicaVoxel;
using DeBroglie.Models;
using DeBroglie.Topo;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeBroglie.Console
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message):base(message)
        {

        }
    }


    public class ItemsProcessor
    {
        private readonly ISampleSetImporter loader;
        private readonly DeBroglieConfig config;
        private readonly Factory factory;

        public ItemsProcessor(ISampleSetImporter loader, DeBroglieConfig config)
        {
            this.loader = loader;
            this.config = config;
            factory = new Factory();
            factory.Config = config;
            if (loader != null)
            {
                factory.TileParser = loader.Parse;
            }
        }

        private SampleSet LoadSample()
        {
            if (config.Src == null)
                throw new ConfigurationException("Src sample should be set");
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

        private Tile Parse(string s)
        {
            return factory.Parse(s);
        }

        private SampleSet LoadFileSet()
        {
            if (config.Tiles == null)
                throw new ConfigurationException($"You must specify tile data when using SrcType {config.SrcType}.");

            var filenames = new Dictionary<Tile, string>();
            foreach (var tile in config.Tiles)
            {
                if (tile.Value == null)
                {
                    tile.Value = new Guid().ToString();
                }
                if (tile.Src == null)
                    throw new ConfigurationException($"All tiles must have a src set when using SrcType {config.SrcType}.");
                filenames[Parse(tile.Value)] = tile.Src;
            }

            if(filenames.Count == 0)
                throw new ConfigurationException($"Must supply at least one tile when using SrcType {config.SrcType}.");

            if (config.SrcType == SrcType.BitmapSet)
            {
                var bitmaps = filenames.ToDictionary(x => x.Key, x => Image.Load(Path.Combine(config.BaseDirectory, x.Value)));
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

        public void ProcessItem()
        {
            if (config.Dest == null)
            {
                throw new ConfigurationException("Dest attribute must be set");
            }

            var directory = config.BaseDirectory;

            var dest = Path.Combine(directory, config.Dest);
            var contdest = Path.ChangeExtension(dest, ".contradiction" + Path.GetExtension(dest));

            // TODO: Neat way to do this without mutability?
            factory.TilesByName = new Dictionary<string, Tile>();

            SampleSet sampleSet;
            if (config.SrcType == SrcType.Sample)
            {
                sampleSet = LoadSample();
                factory.TilesByName = sampleSet.TilesByName ?? factory.TilesByName;
            }
            else
            {
                sampleSet = LoadFileSet();
            }
            var directions = sampleSet.Directions;

            var topology = factory.GetOutputTopology(directions); 

            var tileRotation = factory.GetTileRotation(config.RotationTreatment, topology);

            var model = factory.GetModel(directions, sampleSet, tileRotation);

            var constraints = factory.GetConstraints(directions, tileRotation);

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
                    throw new ConfigurationException($"{filename} is empty.");
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
