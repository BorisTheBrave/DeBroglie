using DeBroglie.Models;
using DeBroglie.Topo;
using System;
using System.Drawing;

namespace DeBroglie.Console
{

    public class BitmapSaver : ISampleSetSaver
    { 
        public void Save(TileModel model, TilePropagator propagator, string filename, DeBroglieConfig config, object template)
        {
            if (config.Animate)
            {

                var topoArray = propagator.ToArraySets().Map(BitmapUtils.ColorAverage);
                var bitmap = BitmapUtils.ToBitmap(topoArray.ToArray2d());
                bitmap.Save(filename);
            }
            else
            {
                var topoArray = propagator.ToValueArray(Color.Gray, Color.Magenta);
                var bitmap = BitmapUtils.ToBitmap(topoArray.ToArray2d());
                bitmap.Save(filename);
            }
        }

        private static ITopoArray<T> Scale<T>(ITopoArray<T> topoArray, int scale)
        {
            var topology = topoArray.Topology;
            if (topology.Mask != null)
                throw new NotSupportedException();
            var result = new T[topology.Width * scale, topology.Height * scale, topology.Depth * scale];
            for (var z = 0; z < topology.Depth; z++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var x = 0; x < topology.Width; x++)
                    {
                        var value = topoArray.Get(x, y, z);
                        for (var dz = 0; dz < scale; dz++)
                        {
                            for (var dy = 0; dy < scale; dy++)
                            {
                                for (var dx = 0; dx < scale; dx++)
                                {
                                    result[x * scale + dx, y * scale + dy, z * scale + dz] = value;
                                }
                            }
                        }
                    }
                }
            }
            var resultTopology = topology.WithSize(topology.Width * scale, topology.Height * scale, topology.Depth * scale);
            return TopoArray.Create(result, resultTopology);
        }
    }
}
