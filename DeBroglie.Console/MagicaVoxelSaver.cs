using DeBroglie.MagicaVoxel;
using DeBroglie.Models;
using System.IO;

namespace DeBroglie.Console
{

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
