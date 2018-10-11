using DeBroglie.MagicaVoxel;
using DeBroglie.Topo;
using System.IO;

namespace DeBroglie.Console
{
    public class MagicaVoxelLoader : ISampleSetLoader
    {
        public SampleSet Load(string filename)
        {
            Vox vox;
            using (var stream = File.OpenRead(filename))
            {
                var br = new BinaryReader(stream);
                vox = VoxSerializer.Read(br);
            }
            var sample = VoxUtils.Load(vox).ToTiles();
            return new SampleSet
            {
                Directions = sample.Topology.Directions,
                Samples = new[] { sample },
                Template = vox,
            };
        }

        public Tile Parse(string s)
        {
            return new Tile(byte.Parse(s));
        }
    }
}
