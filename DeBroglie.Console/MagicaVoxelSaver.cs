using DeBroglie.Console.Export;
using DeBroglie.MagicaVoxel;
using DeBroglie.Models;
using DeBroglie.Topo;
using System.IO;
using System.Linq;

namespace DeBroglie.Console
{

    public class MagicaVoxelSaver : ISampleSetSaver
    { 
        public void Save(TileModel model, TilePropagator tilePropagator, string filename, DeBroglieConfig config, ExportOptions exportOptions)
        {
            Vox vox;
            ITopoArray<byte> array;
            if (exportOptions is VoxExportOptions veo)
            {
                vox = veo.Template;
                array = tilePropagator.ToValueArray<byte>();
            }
            else if(exportOptions is VoxSetExportOptions vseo)
            {
                vox = vseo.Template;
                var tileArray = tilePropagator.ToArray();
                var subTiles = vseo.SubTiles.ToDictionary(x => x.Key, x => VoxUtils.ToTopoArray(x.Value));
                array = MoreTopoArrayUtils.ExplodeTiles(tileArray, subTiles, vseo.TileWidth, vseo.TileHeight, vseo.TileDepth);
            }
            else
            {
                throw new System.Exception($"Cannot export from {exportOptions.TypeDescription} to .vox");
            }
            
            VoxUtils.Save(vox, array);

            using (var stream = new FileStream(filename, FileMode.Create))
            {
                var br = new BinaryWriter(stream);
                VoxSerializer.Write(br, vox);
            }
        }
    }
}
