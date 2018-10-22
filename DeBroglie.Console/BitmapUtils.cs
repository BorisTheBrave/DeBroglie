using System.Collections.Generic;
using System.Drawing;

namespace DeBroglie.Console
{
    public static class BitmapUtils
    {
        public static Color[,] ToColorArray(Bitmap bitmap)
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

        public static Bitmap ToBitmap(Color[,] colorArray)
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

        public static Color ColorAverage(IEnumerable<Color> colors)
        {
            int alpha = 0;
            int red = 0;
            int green = 0;
            int blue = 0;
            int n = 0;
            foreach (var color in colors)
            {
                alpha += color.A;
                red += color.R;
                green += color.G;
                blue += color.B;
                n += 1;
            }
            return Color.FromArgb(alpha / n, red / n, green / n, blue / n);
        }

        public static Bitmap Slice(Bitmap b, int x, int y, int width, int height)
        {
            var newImage = new Bitmap(width, height);
            Blit(newImage, b, 0, 0, x, y, width, height);
            return newImage;
        }

        public static void Blit(Bitmap dest, Bitmap src, int destX, int destY, int srcX, int srcY, int width, int height)
        {
            var graphics = Graphics.FromImage(dest);
            var size = new SizeF(width, height);
            var dstRect = new RectangleF(new PointF(destX, destY), size);
            var srcRect = new RectangleF(new PointF(srcX, srcY), size);
            graphics.DrawImage(src, dstRect, srcRect, GraphicsUnit.Pixel);
        }
    }
}
