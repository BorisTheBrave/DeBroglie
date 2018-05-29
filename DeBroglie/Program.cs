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
                var filename = @"desert.tmx";
                var map = TiledUtil.Load(filename);


                var layer = (TileLayer)map.Layers[0];
                var layerArray = TiledUtil.AsIntArray(layer);
                

                var model = new OverlappingModel<int>(layerArray, 2, false, 1);

                var propagator = new WavePropagator(model, 100, 100, false);

                var status = propagator.Run();

                layerArray = model.ToArray(propagator);
                
                map.Width = layerArray.GetLength(0);
                map.Height = layerArray.GetLength(0);
                map.Layers = new[] { TiledUtil.AsLayer(layerArray) };

                TiledUtil.Save("new_desert.tmx", map);

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
