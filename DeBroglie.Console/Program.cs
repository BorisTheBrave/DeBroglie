using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;
using TiledLib.Layer;
using DeBroglie;
using System.IO;
using DeBroglie.MagicaVoxel;
using System.Linq;

namespace DeBroglie.Console
{

    class Program
    {
        static void Main(string[] args)
        {
            {
                var filename = "columns.vox";
                Vox vox;
                using (var stream = File.OpenRead(filename))
                {
                    var br = new BinaryReader(stream);
                    vox = VoxSerializer.Read(br);
                }
                var array = VoxUtils.Load(vox);
                //var model = new OverlappingModel<byte>(array, 5, 4, true);
                var model = new AdjacentModel<byte>(array);
                var width = 21;
                var height = 21;
                var depth = 21;
                var propagator = new WavePropagator(model, new Topology(Directions.Cartesian3d, width, height, depth, false), false);
                var groundPatterns = new HashSet<int>(model.TilesToPatterns[255]);
                var nonGroundPatterns = new HashSet<int>(Enumerable.Range(0, model.PatternCount).Except(groundPatterns));
                var airPatterns = new HashSet<int>(model.TilesToPatterns[0]);
                var nonAirPatterns = new HashSet<int>(Enumerable.Range(0, model.PatternCount).Except(airPatterns));
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        for (var z = 0; z < depth; z++)
                        {
                            var isBoundary = x == 0 || x == width - 1 ||
                                y == 0 || y == height- 1 ||
                                z == 0 || z == depth - 1;
                            var patternsToBan = z == 0 ? nonGroundPatterns : isBoundary ? nonAirPatterns : groundPatterns;
                            foreach(var pattern in patternsToBan)
                            {
                                propagator.Ban(x, y, z, pattern);
                            }
                        }
                    }
                }
                //model.Frequencies[0] = 1;
                var status = propagator.Run();
                array = model.ToTopArray(propagator);
                VoxUtils.Save(vox, array);

                using (var stream = new FileStream("columns2.vox", FileMode.Create))
                {
                    var br = new BinaryWriter(stream);
                    VoxSerializer.Write(br, vox);
                }
                return;
            }
            {
                var filename = @"hexagonal-mini2.tmx";
                var map = TiledUtil.Load(filename);


                var layer = (TileLayer)map.Layers[0];
                var layerArray = TiledUtil.ReadLayer(map, layer);


                var model = new OverlappingModel<int>(layerArray, 2, 6, false);
                //var model = new AdjacentModel<int>(layerArray);

                var propagator = new WavePropagator(model, new Topology(Directions.Hexagonal2d, 20, 20, false), false);

                var status = propagator.Run();

                layerArray = new TopArray2D<int>(model.ToArray(propagator), propagator.Topology);
                layer = TiledUtil.WriteLayer(map, layerArray);

                map.Layers = new[] { layer };
                map.Width = layer.Width;
                map.Height = layer.Height;

                TiledUtil.Save("new_hexmini.tmx", map);

                return;
            }
            {
                SamplesProcessor.Process();
                return;
            }

            {
                int[,] sample =
                {
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 1, 0, 0, 0 },
                { 0, 0, 1, 1, 1, 1 },
                { 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0 },
            };

                var model = new OverlappingModel<int>(sample, 3, false, 8);

                var pathConstraint = PathConstraint.Create(model, new[] { 1 }, new[]{
                new Point(0, 0),
                new Point(9, 9),
            });

                var propagator = new WavePropagator(model, 10, 10, false, true, new[] { pathConstraint });

                var status = ConsoleUtils.Run(propagator, 1);
                ConsoleUtils.Write(model, propagator);

                System.Console.WriteLine("Backtrack count {0}", propagator.BacktrackCount);
            }
        }
    }
}
