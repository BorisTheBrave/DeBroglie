using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace DeBroglie
{

    [XmlRoot("samples")]
    public class Samples
    {
        [XmlElement(Type = typeof(Overlapping), ElementName = "overlapping")]
        [XmlElement(Type = typeof(SimpleTiled), ElementName = "simpletiled")]
        public List<Item> Items { get; set; }
    }

    public class Item
    {

    }

    public class Overlapping : Item
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("N")]
        public int N { get; set; } = 2;

        [XmlAttribute("symmetry")]
        public int Symmetry { get; set; } = 8;

        [XmlAttribute("ground")]
        public int Ground { get; set; }

        [XmlAttribute("periodic")]
        public string Periodic { get; set; } = "false";

        public bool IsPeriodic => Periodic.ToLower() == "true";

        [XmlAttribute("periodicInput")]
        public string PeriodicInput { get; set; } = "true";

        public bool IsPeriodicInput => PeriodicInput.ToLower() == "true";

        [XmlAttribute("width")]
        public int Width { get; set; } = 48;

        [XmlAttribute("height")]
        public int Height { get; set; } = 48;

        [XmlAttribute("screenshots")]
        public int Screenshots { get; set; } = 2;
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

        private static OverlappingModel<Color> LoadOverlapped(string filename, int n, bool periodic, int symmetries)
        {
            var bitmap = new Bitmap(filename);

            var sample = ToColorArray(bitmap);

            return new OverlappingModel<Color>(sample, n, periodic, symmetries);
        }

        private static Samples LoadSamplesFile(string filename)
        {
            var serializer = new XmlSerializer(typeof(Samples));
            using (var fs = new FileStream(filename, FileMode.Open))
            {
                return (Samples)serializer.Deserialize(fs);
            }
        }

        public static void Process()
        {
            var samples = LoadSamplesFile("samples.xml");
            Directory.CreateDirectory("output");
            foreach (var item in samples.Items)
            {
                int itemCount = 0;
                if (item is Overlapping)
                {
                    var overlapping = item as Overlapping;
                    var model = LoadOverlapped("samples/" + overlapping.Name + ".png", overlapping.N, overlapping.IsPeriodicInput, overlapping.Symmetry);

                    var constraints = new List<IWaveConstraint>();
                    if (overlapping.Ground != 0)
                        constraints.Add(model.GetGroundConstraint());

                    var propagator = new WavePropagator(model, overlapping.Width, overlapping.Height, overlapping.IsPeriodic, constraints: constraints.ToArray());

                    for (var i = 0; i < overlapping.Screenshots; i++)
                    {
                        propagator.Clear();
                        var status = propagator.Run();
                        if (status == CellStatus.Decided)
                        {
                            var array = model.ToArray(propagator, Color.Gray, Color.Red);
                            var bitmap = ToBitmap(array);
                            var filename = string.Format("output/{0} {1} {2}.png", overlapping.Name, itemCount, i);
                            bitmap.Save(filename);
                        }
                    }
                    itemCount++;
                }
            }
        }
    }
}
