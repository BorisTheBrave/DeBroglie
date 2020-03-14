using DeBroglie.Rot;
using System;

namespace DeBroglie.Topo
{

    public static class TopoArrayUtils
    {
        public static ValueTuple<int, int> RotateVector(DirectionSetType type, int x, int y, Rotation rotation)
        {
            if (type == DirectionSetType.Cartesian2d ||
                type == DirectionSetType.Cartesian3d)
            {
                return SquareRotateVector(x, y, rotation);
            }
            else if (type == DirectionSetType.Hexagonal2d)
            {
                return HexRotateVector(x, y, rotation);
            }
            else
            {
                throw new Exception($"Unknown directions type {type}");
            }
        }

        public static ValueTuple<int, int> SquareRotateVector(int x, int y, Rotation rotation)
        {
            if (rotation.ReflectX)
            {
                x = -x;
            }
            switch (rotation.RotateCw)
            {
                case 0 * 90:
                    return (x, y);
                case 1 * 90:
                    return (-y, x);
                case 2 * 90:
                    return (-x, -y);
                case 3 * 90:
                    return (y, -x);
                default:
                    throw new Exception($"Unexpected angle {rotation.RotateCw}");
            }
        }

        public static ValueTuple<int, int> HexRotateVector(int x, int y, Rotation rotation)
        {
            var microRotate = (rotation.RotateCw / 60) % 3;
            var rotate180 = (rotation.RotateCw / 60) % 2 == 1;
            return HexRotateVector(x, y, microRotate, rotate180, rotation.ReflectX);
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

        public static Direction RotateDirection(DirectionSet directions, Direction direction, Rotation rotation)
        {
            var x = directions.DX[(int)direction];
            var y = directions.DY[(int)direction];
            var z = directions.DZ[(int)direction];

            (x, y) = RotateVector(directions.Type, x, y, rotation);

            return directions.GetDirection(x, y, z);
        }


        public delegate bool TileRotate<T>(T tile, out T result);

        public static ITopoArray<Tile> Rotate(ITopoArray<Tile> original, Rotation rotation, TileRotation tileRotation = null)
        {
            var gridTopology = original.Topology.AsGridTopology();
            var type = gridTopology.Directions.Type;
            if (type == DirectionSetType.Cartesian2d ||
                type == DirectionSetType.Cartesian3d)
            {
                return SquareRotate(original, rotation, tileRotation);
            }
            else if (type == DirectionSetType.Hexagonal2d)
            {
                return HexRotate(original, rotation, tileRotation);
            }
            else
            {
                throw new Exception($"Unknown directions type {type}");
            }
        }

        public static ITopoArray<T> Rotate<T>(ITopoArray<T> original, Rotation rotation, TileRotate<T> tileRotate = null)
        {
            var topology = original.Topology.AsGridTopology();
            var type = topology.Directions.Type;
            if (type == DirectionSetType.Cartesian2d ||
                type == DirectionSetType.Cartesian3d)
            {
                return SquareRotate(original, rotation, tileRotate);
            }
            else if (type == DirectionSetType.Hexagonal2d)
            {
                return HexRotate(original, rotation, tileRotate);
            }
            else
            {
                throw new Exception($"Unknown directions type {type}");
            }

        }


        public static ITopoArray<Tile> SquareRotate(ITopoArray<Tile> original, Rotation rotation, TileRotation tileRotation = null)
        {
            bool TileRotate(Tile tile, out Tile result)
            {
                return tileRotation.Rotate(tile, rotation, out result);
            }
            return SquareRotate<Tile>(original, rotation, tileRotation == null ? null : (TileRotate<Tile> )TileRotate);
        }

        public static ITopoArray<T> SquareRotate<T>(ITopoArray<T> original, Rotation rotation, TileRotate<T> tileRotate = null)
        {
            if (rotation.IsIdentity)
                return original;

            ValueTuple<int, int> MapCoord(int x, int y)
            {
                return SquareRotateVector(x, y, rotation);
            }

            return RotateInner(original, MapCoord, tileRotate);
        }

        public static ITopoArray<Tile> HexRotate(ITopoArray<Tile> original, Rotation rotation, TileRotation tileRotation = null)
        {
            bool TileRotate(Tile tile, out Tile result)
            {
                return tileRotation.Rotate(tile, rotation, out result);
            }
            return HexRotate<Tile>(original, rotation, tileRotation == null ? null : (TileRotate<Tile>)TileRotate);
        }

        public static ITopoArray<T> HexRotate<T>(ITopoArray<T> original, Rotation rotation, TileRotate<T> tileRotate = null)
        {
            if (rotation.IsIdentity)
                return original;

            var microRotate = (rotation.RotateCw / 60) % 3;
            var rotate180 = (rotation.RotateCw / 60) % 2 == 1;

            // Actually do a reflection/rotation
            ValueTuple<int, int> MapCoord(int x, int y)
            {
                return HexRotateVector(x, y, microRotate, rotate180, rotation.ReflectX);
            }

            return RotateInner(original, MapCoord, tileRotate);
        }


        private static ITopoArray<T> RotateInner<T>(ITopoArray<T> original, Func<int, int, ValueTuple<int, int>> mapCoord, TileRotate<T> tileRotate = null)
        {
            var originalTopology = original.Topology.AsGridTopology();

            // Find new bounds
            var (x1, y1) = mapCoord(0, 0);
            var (x2, y2) = mapCoord(originalTopology.Width - 1, 0);
            var (x3, y3) = mapCoord(originalTopology.Width - 1, originalTopology.Height - 1);
            var (x4, y4) = mapCoord(0, originalTopology.Height - 1);

            var minx = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
            var maxx = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));
            var miny = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
            var maxy = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));

            // Arrange so that co-ordinate transfer is into the rect bounced by width, height
            var offsetx = -minx;
            var offsety = -miny;
            var width = maxx - minx + 1;
            var height = maxy - miny + 1;
            var depth = originalTopology.Depth;

            var mask = new bool[width * height * depth];
            var topology = new GridTopology(originalTopology.Directions, width, height, originalTopology.Depth, false, false, false, mask);
            var values = new T[width, height, depth];

            // Copy from original to values based on the rotation, setting up the mask as we go.
            for (var z = 0; z < originalTopology.Depth; z++)
            {
                for (var y = 0; y < originalTopology.Height; y++)
                {
                    for (var x = 0; x < originalTopology.Width; x++)
                    {
                        var (newX, newY) = mapCoord(x, y);
                        newX += offsetx;
                        newY += offsety;
                        int newIndex = topology.GetIndex(newX, newY, z);
                        var newValue = original.Get(x, y, z);
                        bool hasNewValue = true;
                        if(tileRotate != null)
                        {
                            hasNewValue = tileRotate(newValue, out newValue);
                        }
                        values[newX, newY, z] = newValue;
                        mask[newIndex] = hasNewValue && originalTopology.ContainsIndex(originalTopology.GetIndex(x, y, z));
                    }
                }
            }

            return new TopoArray3D<T>(values, topology);
        }
    }
}
