using DeBroglie.MagicaVoxel;
using DeBroglie.Models;
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

    public class MagicaVoxelSaver : ISampleSetSaver
    { 
        public void Save(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, object template)
        {
            var vox = template as Vox;
            var array = tilePropagator.ToValueArray<byte>();
            VoxUtils.Save(vox, array);

            using (var stream = new FileStream(filename, FileMode.Create))
            {
                var br = new BinaryWriter(stream);
                VoxSerializer.Write(br, vox);
            }
        }
    }
}
