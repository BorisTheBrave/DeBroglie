using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace DeBroglie.Console
{

    [XmlRoot("items")]
    public class Items
    {
        [XmlElement(Type = typeof(Overlapping), ElementName = "overlapping")]
        [XmlElement(Type = typeof(SimpleTiled), ElementName = "simpletiled")]
        public List<Item> AllItems { get; set; }
    }

    public class Item
    {
        [XmlAttribute("dest")]
        public string Dest { get; set; }

        [XmlAttribute("src")]
        public string Src { get; set; }

        [XmlAttribute("periodicInput")]
        public string PeriodicInput { get; set; } = "true";

        public bool IsPeriodicInput => PeriodicInput.ToLower() == "true";

        [XmlAttribute("periodic")]
        public string Periodic { get; set; } = "false";

        public bool IsPeriodic => Periodic.ToLower() == "true";

        [XmlAttribute("width")]
        public int Width { get; set; } = 48;

        [XmlAttribute("height")]
        public int Height { get; set; } = 48;
    }

    public class Overlapping : Item
    {
        [XmlAttribute("N")]
        public int N { get; set; } = 2;

        [XmlAttribute("symmetry")]
        public int Symmetry { get; set; } = 8;

        [XmlAttribute("ground")]
        public int Ground { get; set; }
    }

    public class SimpleTiled : Item
    {

    }

    public class SamplesProcessor
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

        private static TileModel<T> GetModel<T>(Item item, ITopArray<T> sample)
        {
            if (item is Overlapping overlapping)
            {
                return new OverlappingModel<T>(sample, overlapping.N, overlapping.Symmetry);
            }
            return null;
        }

        private static Items LoadItemsFile(string filename)
        {
            var serializer = new XmlSerializer(typeof(Items));
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                return (Items)serializer.Deserialize(fs);
            }
        }

        private static void ProcessBitmap(Item item, string directory)
        {
            if (item.Src == null || item.Dest == null)
                return;

            var src = Path.Combine(directory, item.Src);
            var dest = Path.Combine(directory, item.Dest);
            var contdest = Path.ChangeExtension(dest, ".contradiction" + Path.GetExtension(dest));
            System.Console.WriteLine($"Reading {src}");

            var bitmap = new Bitmap(src);

            var colorArray = ToColorArray(bitmap);
            var topArray = new TopArray2D<Color>(colorArray, item.IsPeriodicInput);

            var model = GetModel(item, topArray);
            if (model == null)
                return;

            System.Console.WriteLine($"Processing {dest}");
            var constraints = new List<IWaveConstraint>();
            if (item is Overlapping overlapping && overlapping.Ground != 0)
                constraints.Add(((OverlappingModel<Color>)model).GetGroundConstraint());

            var propagator = new WavePropagator(model, item.Width, item.Height, item.IsPeriodic, constraints: constraints.ToArray());
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
            var array = model.ToArray(propagator, Color.Gray, Color.Magenta);
            bitmap = ToBitmap(array);
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            if (status == CellStatus.Decided)
            {
                System.Console.WriteLine($"Writing {dest}");
                bitmap.Save(dest);
                File.Delete(contdest);
            }
            else
            {
                System.Console.WriteLine($"Writing {contdest}");
                bitmap.Save(contdest);
                File.Delete(dest);
            }
        }


        public static void Process(string filename)
        {
            var directory = Path.GetDirectoryName(filename);
            var items = LoadItemsFile(filename);
            foreach (var item in items.AllItems)
            {
                ProcessBitmap(item, directory);
            }
        }
    }
}
