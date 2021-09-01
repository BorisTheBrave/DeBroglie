using DeBroglie.Console.Config;
using DeBroglie.Models;
using System.IO;

namespace DeBroglie.Console.Export
{
    public class CsvExporter : IExporter
    {
        public void Export(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            var array = tilePropagator.ToArray(new Tile("?"), new Tile("!"));
            using (var s = File.Open(filename, FileMode.Create))
            using (var tw = new StreamWriter(s))
            {
                for (var z = 0; z < array.Topology.Depth; z++)
                {
                    if (z != 0)
                        tw.WriteLine();
                    for (var y = 0; y < array.Topology.Height; y++)
                    {
                        for (var x = 0; x < array.Topology.Width; x++)
                        {
                            if (x != 0)
                                tw.Write(",");
                            tw.Write(array.Get(x, y, z).Value?.ToString());
                        }
                        tw.WriteLine();
                    }
                }
            }
        }
    }
}
