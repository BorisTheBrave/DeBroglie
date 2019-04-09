using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System.Collections.Generic;

namespace DeBroglie.Console
{
    public static class BitmapUtils
    {
        // TODO: Get PixelSpan?
        public static Rgba32[,] ToColorArray(Image<Rgba32> bitmap)
        {
            Rgba32[,] sample = new Rgba32[bitmap.Width, bitmap.Height];
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    sample[x, y] = bitmap[x, y];
                }
            }
            return sample;
        }

        // TODO: Load Pixel Data
        public static Image<Rgba32> ToBitmap(Rgba32[,] colorArray)
        {
            var bitmap = new Image<Rgba32>(colorArray.GetLength(0), colorArray.GetLength(1));
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    bitmap[x, y] = colorArray[x, y];
                }
            }
            return bitmap;
        }

        public static Rgba32 ColorAverage(IEnumerable<Rgba32> colors)
        {
            if (colors == null)
            {
                return Rgba32.Transparent;
            }

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
            if (n == 0)
            {
                return Rgba32.Transparent;
            }
            else
            {
                return new Rgba32(red / n, green / n, blue / n, alpha / n);
            }
        }

        public static Image<Rgba32> Slice(Image<Rgba32> b, int x, int y, int width, int height)
        {
            var newImage = new Image<Rgba32>(width, height);
            Blit(newImage, b, 0, 0, x, y, width, height);
            return newImage;
        }

        public static void Blit(Image<Rgba32> dest, Image<Rgba32> src, int destX, int destY, int srcX, int srcY, int width, int height)
        {
            // TODO: Seriously, is this the best way in ImageSharp?
            var subImage = src.Clone(c => c.Crop(new Rectangle(srcX, srcY, width, height)));    
            dest.Mutate(c => c.DrawImage(subImage, new SixLabors.Primitives.Point(destX, destY), 1.0f));
        }
    }
}
