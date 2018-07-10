using DeBroglie.MagicaVoxel;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using TiledLib.Layer;

namespace DeBroglie.Console
{
    public abstract class ItemsProcessor<T>
    {
        protected abstract ITopArray<T> Load(string filename, Item item);

        protected abstract void Save(TileModel<T> model, TilePropagator<T> tilePropagator, string filename);

        private static TileModel<T> GetModel(Item item, ITopArray<T> sample)
        {
            if (item is Overlapping overlapping)
            {
                return new OverlappingModel<T>(sample, overlapping.N, overlapping.Symmetry);
            }
            else if(item is Adjacent adjacent)
            {
                return new AdjacentModel<T>(sample);
            }
            throw new System.Exception();
        }

        public void Process(Item item, string directory)
        {
            if(item is SimpleTiled)
            {
                // Not supported atm
                return;
            }
            if (item.Src == null)
            {
                throw new System.Exception("src attribute must be set");
            }

            if (item.Dest == null)
            {
                throw new System.Exception("dest attribute must be set");
            }

            var src = Path.Combine(directory, item.Src);
            var dest = Path.Combine(directory, item.Dest);
            var contdest = Path.ChangeExtension(dest, ".contradiction" + Path.GetExtension(dest));
            System.Console.WriteLine($"Reading {src}");

            var topArray = Load(src, item);

            var model = GetModel(item, topArray);

            var is3d = topArray.Topology.Directions.Type == DirectionsType.Cartesian3d;
            var topology = new Topology(topArray.Topology.Directions, item.Width, item.Height, is3d ? item.Depth : 1, item.IsPeriodic);

            // Setup constraints
            var constraints = new List<IWaveConstraint>();
            if (item is Overlapping overlapping && overlapping.Ground != 0)
                constraints.Add(((OverlappingModel<T>)model).GetGroundConstraint());
            if (is3d)
                constraints.Add(new BorderConstraint(model as TileModel<byte>));


            System.Console.WriteLine($"Processing {dest}");
            var propagator = new TilePropagator<T>(model, topology, item.Backtrack, waveConstraints: constraints.ToArray());
            CellStatus status = CellStatus.Contradiction;
            for (var retry = 0; retry < 5; retry++)
            {
                if (retry != 0)
                    propagator.Clear();
                status = propagator.Run();
                if (status == CellStatus.Decided)
                {
                    break;
                }
                System.Console.WriteLine($"Found contradiction, retrying");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            if (status == CellStatus.Decided)
            {
                System.Console.WriteLine($"Writing {dest}");
                Save(model, propagator, dest);
                File.Delete(contdest);
            }
            else
            {
                System.Console.WriteLine($"Writing {contdest}");
                Save(model, propagator, contdest);
                File.Delete(dest);
            }
        }
    }

    public class BitmapItemsProcessor : ItemsProcessor<Color>
    {
        private static Color[,] ToColorArray(Bitmap bitmap)
        {
            Color[,] sample = new Color[bitmap.Width, bitmap.Height];
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    sample[x, y] = bitmap.GetPixel(x, y);
                }
            }
            return sample;
        }

        private static Bitmap ToBitmap(Color[,] colorArray)
        {
            var bitmap = new Bitmap(colorArray.GetLength(0), colorArray.GetLength(1));
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    bitmap.SetPixel(x, y, colorArray[x, y]);
                }
            }
            return bitmap;
        }

        protected override ITopArray<Color> Load(string filename, Item item)
        {
            var bitmap = new Bitmap(filename);
            var colorArray = ToColorArray(bitmap);
            return new TopArray2D<Color>(colorArray, item.IsPeriodicInput);
        }

        protected override void Save(TileModel<Color> model, TilePropagator<Color> propagator, string filename)
        {
            var array = propagator.ToTopArray(Color.Gray, Color.Magenta).ToArray2d();
            var bitmap = ToBitmap(array);
            bitmap.Save(filename);
        }
    }

    public class TiledItemsProcessor : ItemsProcessor<int>
    {
        private TiledLib.Map map;
        private string srcFilename;

        protected override ITopArray<int> Load(string filename, Item item)
        {
            srcFilename = filename;
            map = TiledUtil.Load(filename);
            var layer = (TileLayer)map.Layers[0];
            return TiledUtil.ReadLayer(map, layer);
        }

        protected override void Save(TileModel<int> model, TilePropagator<int> tilePropagator, string filename)
        {
            var layerArray = tilePropagator.ToTopArray();
            var layer = TiledUtil.WriteLayer(map, layerArray);
            map.Layers = new[] { layer };
            map.Width = layer.Width;
            map.Height = layer.Height;
            TiledUtil.Save(filename, map);

            // Check for any external files that may also need copying
            foreach(var tileset in map.Tilesets)
            {
                if(tileset.ImagePath != null)
                {
                    var srcImagePath = Path.Combine(Path.GetDirectoryName(srcFilename), tileset.ImagePath);
                    var destImagePath = Path.Combine(Path.GetDirectoryName(filename), tileset.ImagePath);
                    if(File.Exists(srcImagePath) && !File.Exists(destImagePath))
                    {
                        File.Copy(srcImagePath, destImagePath);
                    }
                }
            }
        }
    }

    public class VoxItemsProcessor : ItemsProcessor<byte>
    {
        Vox vox;

        protected override ITopArray<byte> Load(string filename, Item item)
        {
            using (var stream = File.OpenRead(filename))
            {
                var br = new BinaryReader(stream);
                vox = VoxSerializer.Read(br);
            }
            return VoxUtils.Load(vox);

        }

        protected override void Save(TileModel<byte> model, TilePropagator<byte> tilePropagator, string filename)
        {
            var array = tilePropagator.ToTopArray();
            VoxUtils.Save(vox, array);

            using (var stream = new FileStream(filename, FileMode.Create))
            {
                var br = new BinaryWriter(stream);
                VoxSerializer.Write(br, vox);
            }
        }
    }

    public static class ItemsProcessor
    {

        private static Items LoadItemsFile(string filename)
        {
            var serializer = new XmlSerializer(typeof(Items));
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                return (Items)serializer.Deserialize(fs);
            }
        }

        public static void Process(string filename)
        {
            var directory = Path.GetDirectoryName(filename);
            var items = LoadItemsFile(filename);
            foreach (var item in items.AllItems)
            {
                if (item is SimpleTiled)
                {
                    // Not supported atm
                    continue;
                }
                if (item.Src.EndsWith(".png"))
                {
                    var processor = new BitmapItemsProcessor();
                    processor.Process(item, directory);
                }
                else if (item.Src.EndsWith(".tmx"))
                {
                    var processor = new TiledItemsProcessor();
                    processor.Process(item, directory);
                }
                else if (item.Src.EndsWith(".vox"))
                {
                    var processor = new VoxItemsProcessor();
                    processor.Process(item, directory);
                }
                else
                {
                    throw new System.Exception($"Unrecongized extenion for {item.Src}");
                }
            }
        }
    }
}
