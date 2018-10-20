using DeBroglie.Console.Export;
using DeBroglie.Topo;
using System.Collections.Generic;

namespace DeBroglie.Console.Import
{
    public class SampleSet
    {
        public Directions Directions { get; set; }
        public ITopoArray<Tile>[] Samples { get; set; }
        public IDictionary<string, Tile> TilesByName { get; set; }
        public ExportOptions ExportOptions { get; set; }

    }
}
