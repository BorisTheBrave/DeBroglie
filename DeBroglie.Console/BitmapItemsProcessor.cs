using System.Drawing;

namespace DeBroglie.Console
{
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
}
