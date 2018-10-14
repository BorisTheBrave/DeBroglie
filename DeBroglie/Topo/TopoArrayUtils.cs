using System;

namespace DeBroglie.Topo
{

    public static class TopoArrayUtils
    {
        public static ValueTuple<int, int> RotateVector(int x, int y, int rotateCw, bool reflectX)
        {
            if (reflectX)
            {
                x = -x;
            }
            switch (rotateCw)
            {
                case 0:
                    return (x, y);
                case 1:
                    return (-y, x);
                case 2:
                    return (-x, -y);
                case 3:
                    return (y, -x);
                default:
                    throw new Exception();
            }
        }

        public static ValueTuple<int, int> HexRotateVector(int x, int y, int rotateCw, bool reflectX)
        {
            var microRotate = rotateCw % 3;
            var rotate180 = rotateCw % 2 == 1;
            return HexRotateVector(x, y, microRotate, rotate180, reflectX);
        }

        private static ValueTuple<int, int> HexRotateVector(int x, int y, int microRotate, bool rotate180, bool reflectX)
        {

            if (reflectX)
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
                case 1: q = s; s = r; r = q2; break;
                case 2: q = r; r = s; s = q2; break;
            }
            if (rotate180)
            {
                q = -q;
                r = -r;
                s = -s;
            }
            x = -r;
            y = s;
            return (x, y);
        }


        public delegate bool TileRotate<T>(T tile, out T result);

        public static ITopoArray<Tile> Rotate(ITopoArray<Tile> original, int rotateCw, bool reflectX = false, TileRotation tileRotation = null)
        {
            bool TileRotate(Tile tile, out Tile result)
            {
                return tileRotation.Rotate(tile, rotateCw, reflectX, out result);
            }
            return Rotate<Tile>(original, rotateCw, reflectX, tileRotation == null ? null : (TileRotate<Tile> )TileRotate);
        }

        public static ITopoArray<T> Rotate<T>(ITopoArray<T> original, int rotateCw, bool reflectX = false, TileRotate<T> tileRotate = null)
        {
            if (rotateCw == 0 && !reflectX)
                return original;

            ValueTuple<int, int> MapCoord(int x, int y)
            {
                return RotateVector(x, y, rotateCw, reflectX);
            }

            return RotateInner(original, MapCoord, tileRotate);
        }

        public static ITopoArray<Tile> HexRotate(ITopoArray<Tile> original, int rotate, bool reflectX = false, TileRotation tileRotation = null)
        {
            bool TileRotate(Tile tile, out Tile result)
            {
                return tileRotation.Rotate(tile, rotate, reflectX, out result);
            }
            return HexRotate<Tile>(original, rotate, reflectX, tileRotation == null ? null : (TileRotate<Tile>)TileRotate);
        }

        public static ITopoArray<T> HexRotate<T>(ITopoArray<T> original, int rotateCw, bool reflectX, TileRotate<T> tileRotate = null)
        {
            if (rotateCw == 0 && !reflectX)
                return original;

            var microRotate = rotateCw % 3;
            var rotate180 = rotateCw % 2 == 1;

            // Actually do a reflection/rotation
            ValueTuple<int, int> MapCoord(int x, int y)
            {
                return HexRotateVector(x, y, microRotate, rotate180, reflectX);
            }

            return RotateInner(original, MapCoord, tileRotate);
        }


        private static ITopoArray<T> RotateInner<T>(ITopoArray<T> original, Func<int, int, ValueTuple<int, int>> mapCoord, TileRotate<T> tileRotate = null)
        {
            // Find new bounds
            var (x1, y1) = mapCoord(0, 0);
            var (x2, y2) = mapCoord(original.Topology.Width - 1, 0);
            var (x3, y3) = mapCoord(original.Topology.Width - 1, original.Topology.Height - 1);
            var (x4, y4) = mapCoord(0, original.Topology.Height - 1);

            var minx = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
            var maxx = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));
            var miny = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
            var maxy = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));

            // Arrange so that co-ordinate transfer is into the rect bounced by width, height
            var offsetx = -minx;
            var offsety = -miny;
            var width = maxx - minx + 1;
            var height = maxy - miny + 1;
            var depth = original.Topology.Depth;

            var mask = new bool[width * height * depth];
            var topology = new Topology(original.Topology.Directions, width, height, original.Topology.Depth, false, false, false, mask);
            var values = new T[width, height, depth];

            // Copy from original to values based on the rotation, setting up the mask as we go.
            for (var z = 0; z < original.Topology.Depth; z++)
            {
                for (var y = 0; y < original.Topology.Height; y++)
                {
                    for (var x = 0; x < original.Topology.Width; x++)
                    {
                        var (newX, newY) = mapCoord(x, y);
                        newX += offsetx;
                        newY += offsety;
                        int newIndex = topology.GetIndex(newX, newY, 0);
                        var newValue = original.Get(x, y, z);
                        bool hasNewValue = true;
                        if(tileRotate != null)
                        {
                            hasNewValue = tileRotate(newValue, out newValue);
                        }
                        values[newX, newY, z] = newValue;
                        mask[newIndex] = hasNewValue && original.Topology.ContainsIndex(original.Topology.GetIndex(x, y, z));
                    }
                }
            }

            return new TopoArray3D<T>(values, topology);
        }
    }
}
