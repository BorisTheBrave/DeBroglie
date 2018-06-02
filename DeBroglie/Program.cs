using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;
using TiledLib.Layer;

namespace DeBroglie
{

    class Program
    {
        static void Main(string[] args)
        {
            {
                var filename = @"hexagonal-mini2.tmx";
                var map = TiledUtil.Load(filename);


                var layer = (TileLayer)map.Layers[0];
                var layerArray = TiledUtil.ReadLayer(map, layer);


                var model = new OverlappingModel<int>(layerArray, 2, 1);
                //var model = new AdjacentModel<int>(layerArray);

                var propagator = new WavePropagator(model, new Topology(Directions.Hexagonal2d, 10, 10, false), false);

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

                Console.WriteLine("Backtrack count {0}", propagator.BacktrackCount);
            }
        }
    }
}
