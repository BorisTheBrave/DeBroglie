using System;

namespace DeBroglie
{
    public static class TopArrayUtils
    {
        public static ITopArray<T> HexRotate<T>(ITopArray<T> original, int rotate, bool reflect = false)
        {
            if (rotate == 0 && !reflect)
                return original;

            var microRotate = rotate % 3;
            var rotate180 = rotate % 2 == 1;
            var offsetx = 0;
            var offsety = 0;

            // Actually do a reflection/rotation
            void OriginalToNewCoord(int x, int y, out int outX, out int outY)
            {
                if (reflect)
                {
                    x = -x + y;
                }
                var q = x - y;
                var r = -x;
                var s = y;
                var q2 = q;
                switch (microRotate)
                {
                    case 0: break;
                    case 1: q = r; r = s; s = q2; break;
                    case 2: q = s; s = r; r = q2; break;
                }
                if (rotate180)
                {
                    q = -q;
                    r = -r;
                    s = -s;
                }
                x = -r;
                y = s;
                outX = x + offsetx;
                outY = y + offsety;
            }

            // Find new bounds
            int x1, y1, x2, y2, x3, y3, x4, y4;
            OriginalToNewCoord(0, 0, out x1, out y1);
            OriginalToNewCoord(original.Topology.Width - 1, 0, out x2, out y2);
            OriginalToNewCoord(original.Topology.Width - 1, original.Topology.Height - 1, out x3, out y3);
            OriginalToNewCoord(0, original.Topology.Height - 1, out x4, out y4);

            var minx = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
            var maxx = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));
            var miny = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
            var maxy = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));

            // Arrange so that co-ordinate transfer is into the rect bounced by width, height
            offsetx = -minx;
            offsety = -miny;
            var width = maxx - minx + 1;
            var height = maxy - miny + 1;

            var mask = new bool[width * height];
            var topology = new Topology(Directions.Hexagonal2d, width, height, false, mask);
            var values = new T[width, height];

            // Copy from original to values based on the rotation, setting up the mask as we go.
            for (var x = 0; x < original.Topology.Width; x++)
            {
                for (var y = 0; y < original.Topology.Height; y++)
                {
                    int newX, newY;
                    OriginalToNewCoord(x, y, out newX, out newY);
                    int newIndex = topology.GetIndex(newX, newY);
                    values[newX, newY] = original.Get(x, y);
                    mask[newIndex] = original.Topology.ContainsIndex(original.Topology.GetIndex(x, y));
                }
            }

            return new TopArray2D<T>(values, topology);
        }

    }

}
