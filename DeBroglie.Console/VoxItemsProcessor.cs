using DeBroglie.MagicaVoxel;
using System.IO;

namespace DeBroglie.Console
{
    public class VoxItemsProcessor : ItemsProcessor
    {
        Vox vox;

        protected override ITopArray<Tile> Load(string filename, Item item)
        {
            using (var stream = File.OpenRead(filename))
            {
                var br = new BinaryReader(stream);
                vox = VoxSerializer.Read(br);
            }
            return VoxUtils.Load(vox).ToTiles();

        }

        protected override Tile Parse(string s)
        {
            return new Tile(byte.Parse(s));
        }

        protected override void Save(TileModel model, TilePropagator tilePropagator, string filename)
        {
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
