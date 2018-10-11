using DeBroglie.Topo;
using System;
using System.Drawing;

namespace DeBroglie.Console
{
    public class BitmapLoader : ISampleSetLoader
    {

        public SampleSet Load(string filename)
        {
            Bitmap bitmap;
            try
            {
                bitmap = new Bitmap(filename);
            }
            catch (ArgumentException)
            {
                throw new Exception($"Couldn't load filename: {filename}");
            }
            var colorArray = BitmapUtils.ToColorArray(bitmap);
            var topology = new Topology(Directions.Cartesian2d, colorArray.GetLength(0), colorArray.GetLength(1), false, false);
            return new SampleSet
            {
                Directions = Directions.Cartesian2d,
                Samples = new[] { TopoArray.Create(colorArray, topology).ToTiles() },
            };
        }

        public Tile Parse(string s)
        {
            return new Tile(ColorTranslator.FromHtml(s));
        }

    }
}
