using System;
using System.Drawing;
using System.Xml.Serialization;
using TiledLib.Layer;
using DeBroglie;
using System.IO;
using DeBroglie.MagicaVoxel;

namespace DeBroglie.Console
{

    class Program
    {
        static void Main(string[] args)
        {

            {
                foreach (var arg in args)
                {
                    SamplesProcessor.Process(arg);
                }
                return;
            }
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
                //for (var i = 0; i < model.Frequencies.Length; i++)
                //    model.Frequencies[i] = 1;
                var width = 21;
                var height = 21;
                var depth = 21;
                var topology = new Topology(Directions.Cartesian3d, width, height, depth, false);
                var propagator = new WavePropagator(model, topology, false, new[] { new BorderConstraint(model) });
                
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
