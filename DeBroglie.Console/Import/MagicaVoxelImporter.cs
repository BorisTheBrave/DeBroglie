using DeBroglie.Console.Export;
using DeBroglie.MagicaVoxel;
using DeBroglie.Topo;
using System.IO;

namespace DeBroglie.Console.Import
{
    public class MagicaVoxelImporter : ISampleSetImporter
    {
        public SampleSet Load(string filename)
        {
            var vox = VoxUtils.Load(filename);
            var sample = VoxUtils.ToTopoArray(vox).ToTiles();
            return new SampleSet
            {
                Directions = sample.Topology.Directions,
                Samples = new[] { sample },
                ExportOptions = new VoxExportOptions { Template = vox },
            };
        }

        public Tile Parse(string s)
        {
            return new Tile(byte.Parse(s));
        }
    }
}
