using DeBroglie.Models;
using System.IO;

namespace DeBroglie.Console
{
    public class CsvSaver : ISampleSetSaver
    {
        public void Save(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, object template)
        {
            var array = tilePropagator.ToArray(new Tile("?"), new Tile("!"));
            using (var s = File.OpenWrite(filename))
            using (var tw = new StreamWriter(s))
            {
                for(var z=0;z<array.Topology.Depth;z++)
                {
                    if (z != 0)
                        tw.WriteLine();
                    for(var y = 0;y<array.Topology.Height;y++)
                    {
                        for(var x =0;x<array.Topology.Width;x++)
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
