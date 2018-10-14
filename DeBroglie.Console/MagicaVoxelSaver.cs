using DeBroglie.Console.Export;
using DeBroglie.MagicaVoxel;
using DeBroglie.Models;
using System.IO;

namespace DeBroglie.Console
{

    public class MagicaVoxelSaver : ISampleSetSaver
    { 
        public void Save(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            var voxExportOptions = exportOptions as VoxExportOptions;
            if(voxExportOptions == null)
            {
                throw new System.Exception($"Cannot export from {exportOptions.TypeDescription} to .vox");
            }
            var vox = voxExportOptions.Template;
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
