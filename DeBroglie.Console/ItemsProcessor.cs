using DeBroglie.MagicaVoxel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TiledLib.Layer;

namespace DeBroglie.Console
{
    public abstract class ItemsProcessor
    {
        protected abstract ITopArray<Tile> Load(string filename, Item item);

        protected abstract void Save(TileModel model, TilePropagator tilePropagator, string filename);

        protected abstract Tile Parse(string s);

        private static TileModel GetModel(Item item, ITopArray<Tile> sample)
        {
            if (item is Overlapping overlapping)
            {
                var symmetries = overlapping.Symmetry;
                return new OverlappingModel(sample, overlapping.N, symmetries > 1 ? symmetries / 2 : 1, symmetries > 1);
            }
            else if(item is Adjacent adjacent)
            {
                return new AdjacentModel(sample);
            }
            throw new System.Exception();
        }

        public void ProcessItem(Item item, string directory)
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

            // Setup tiles
            if(item.Tiles != null)
            {
                foreach (var tile in item.Tiles)
                {
                    var value = Parse(tile.Value);
                    if(tile.ChangeFrequency != null)
                    {
                        var cf = tile.ChangeFrequency.Trim();
                        double cfd;
                        if(cf.EndsWith("%"))
                        {
                            cfd = double.Parse(cf.TrimEnd('%')) / 100;
                        }
                        else
                        {
                            cfd = double.Parse(cf);
                        }
                        model.ChangeFrequency(value, cfd);
                    }
                }
            }

            // Setup constraints
            var waveConstraints = new List<IWaveConstraint>();
            if (item is Overlapping overlapping && overlapping.Ground != 0)
                waveConstraints.Add(((OverlappingModel)model).GetGroundConstraint());
            var constraints = new List<ITileConstraint>();
            if (is3d)
            {
                constraints.Add(new BorderConstraint
                {
                    Sides=BorderSides.ZMin,
                    Tile=new Tile(255),
                });
                constraints.Add(new BorderConstraint
                {
                    Sides = BorderSides.All,
                    ExcludeSides = BorderSides.ZMin,
                    Tile = new Tile(0),
                });
            }

            foreach (var constraint in item.Constraints)
            {
                if(constraint is PathData pathData)
                {
                    var pathTiles = new HashSet<Tile>(pathData.PathTiles.Select(Parse));
                    var p = new PathConstraint(pathTiles);
                    constraints.Add(p);
                }
                else if(constraint is BorderData borderData)
                {
                    var tile = Parse(borderData.Tile);
                    var sides = borderData.Sides == null ? BorderSides.All : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.Sides, true);
                    var excludeSides = borderData.ExcludeSides == null ? BorderSides.None : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.ExcludeSides, true);
                    if(!is3d)
                    {
                        sides = sides & ~BorderSides.ZMin & ~BorderSides.ZMax;
                        excludeSides = excludeSides & ~BorderSides.ZMin & ~BorderSides.ZMax;
                    }
                    constraints.Add(new BorderConstraint
                    {
                        Tile = tile,
                        Sides = sides,
                        ExcludeSides = excludeSides,
                    });
                }
            }


            System.Console.WriteLine($"Processing {dest}");
            var propagator = new TilePropagator(model, topology, item.Backtrack,
                constraints: constraints.ToArray(),
                waveConstraints: waveConstraints.ToArray());
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
                    processor.ProcessItem(item, directory);
                }
                else if (item.Src.EndsWith(".tmx"))
                {
                    var processor = new TiledItemsProcessor();
                    processor.ProcessItem(item, directory);
                }
                else if (item.Src.EndsWith(".vox"))
                {
                    var processor = new VoxItemsProcessor();
                    processor.ProcessItem(item, directory);
                }
                else
                {
                    throw new System.Exception($"Unrecongized extenion for {item.Src}");
                }
            }
        }
    }

    public class BitmapItemsProcessor : ItemsProcessor
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

        protected override ITopArray<Tile> Load(string filename, Item item)
        {
            var bitmap = new Bitmap(filename);
            var colorArray = ToColorArray(bitmap);
            return new TopArray2D<Color>(colorArray, item.IsPeriodicInput).ToTiles();
        }

        protected override void Save(TileModel model, TilePropagator propagator, string filename)
        {
            var array = propagator.ToValueArray(Color.Gray, Color.Magenta).ToArray2d();
            var bitmap = ToBitmap(array);
            bitmap.Save(filename);
        }

        protected override Tile Parse(string s)
        {
            throw new System.NotImplementedException();
        }
    }

    public class TiledItemsProcessor : ItemsProcessor
    {
        private TiledLib.Map map;
        private string srcFilename;

        protected override ITopArray<Tile> Load(string filename, Item item)
        {
            srcFilename = filename;
            map = TiledUtil.Load(filename);
            var layer = (TileLayer)map.Layers[0];
            return TiledUtil.ReadLayer(map, layer).ToTiles();
        }

        protected override Tile Parse(string s)
        {
            return new Tile(int.Parse(s));
        }

        protected override void Save(TileModel model, TilePropagator tilePropagator, string filename)
        {
            var layerArray = tilePropagator.ToValueArray<int>();
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
