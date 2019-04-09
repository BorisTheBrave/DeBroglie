using DeBroglie.Console.Export;
using DeBroglie.Topo;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace DeBroglie.Console.Import
{
    public class BitmapImporter : ISampleSetImporter
    {

        public SampleSet Load(string filename)
        {
            Image<Rgba32> bitmap;
            try
            {
                bitmap = Image.Load(filename);
            }
            catch (ArgumentException)
            {
                throw new Exception($"Couldn't load filename: {filename}");
            }
            var colorArray = BitmapUtils.ToColorArray(bitmap);
            var topology = new Topology(DirectionSet.Cartesian2d, colorArray.GetLength(0), colorArray.GetLength(1), false, false);
            return new SampleSet
            {
                Directions = DirectionSet.Cartesian2d,
                Samples = new[] { TopoArray.Create(colorArray, topology).ToTiles() },
                ExportOptions = new BitmapExportOptions(),
            };
        }

        public Tile Parse(string s)
        {
            if(!s.StartsWith("#"))
            {
                throw new Exception("Expected color to start with #");
            }
            s = s.Substring(1);
            string r, g, b, a;
            switch(s.Length)
            {
                case 3:
                    r = s.Substring(0, 1);
                    g = s.Substring(1, 1);
                    b = s.Substring(2, 1);
                    a = "f";
                    break;
                case 6:
                    r = s.Substring(0, 2);
                    g = s.Substring(2, 2);
                    b = s.Substring(4, 2);
                    a = "ff";
                    break;
                case 8:
                    r = s.Substring(0, 2);
                    g = s.Substring(2, 2);
                    b = s.Substring(4, 2);
                    a = s.Substring(6, 2);
                    break;
                default:
                    throw new Exception($"Cannot parse color with {s.Length} digits");

            }
            return new Tile(new Rgba32(
                int.Parse(r, System.Globalization.NumberStyles.HexNumber),
                int.Parse(g, System.Globalization.NumberStyles.HexNumber),
                int.Parse(b, System.Globalization.NumberStyles.HexNumber),
                int.Parse(a, System.Globalization.NumberStyles.HexNumber)
                ));
        }

    }
}
