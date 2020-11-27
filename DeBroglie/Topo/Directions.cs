using System;
using System.Collections;
using System.Collections.Generic;

namespace DeBroglie.Topo
{
    public enum Axis
    {
        X,
        Y,
        Z,
        // The "third" axis used for DirectionSet.Hexagonal2d 
        // it's redundant with X and Y, but still useful to refer to.
        W,
    }

    public enum Direction
    {
        XPlus = 0,
        XMinus = 1,
        YPlus = 2,
        YMinus = 3,
        ZPlus = 4,
        ZMinus = 5,
        // Shared with Z, there's no DirectionSet that uses both.
        WPlus = 4,
        WMinus = 5,
    }

    /// <summary>
    /// DirectionType indicates what neighbors are considered adjacent to each tile.
    /// </summary>
    public enum DirectionSetType
    {
        Unknown,
        Cartesian2d,
        Hexagonal2d,
        Cartesian3d,
        Hexagonal3d,
    }

    public enum EdgeLabel
    {

    }

    /// <summary>
    /// Wrapper around DirectionsType supplying some convenience data.
    /// </summary>
    public struct DirectionSet : IEnumerable<Direction>
    {
        public int[] DX { get; private set; }
        public int[] DY { get; private set; }
        public int[] DZ { get; private set; }

        public int Count { get; private set; }

        public DirectionSetType Type { get; private set; }

        /// <summary>
        /// The Directions associated with square grids.
        /// </summary>
        public static readonly DirectionSet Cartesian2d = new DirectionSet
        {
            DX = new[] { 1, -1, 0, 0 },
            DY = new[] { 0, 0, 1, -1 },
            DZ = new[] { 0, 0, 0, 0 },
            Count = 4,
            Type = DirectionSetType.Cartesian2d,
        };

        /// <summary>
        /// The Directions associated with hexagonal grids.
        /// Conventially, x is treated as moving right, and y as moving down and left,
        /// But the same Directions object will work just as well will several other conventions
        /// as long as you are consistent.
        /// </summary>
        public static readonly DirectionSet Hexagonal2d = new DirectionSet
        {
            DX = new[] { 1, -1, 0, 0, 1, -1 },
            DY = new[] { 0, 0, 1, -1, 1, -1 },
            DZ = new[] { 0, 0, 0, 0, 0, 0 },
            Count = 6,
            Type = DirectionSetType.Hexagonal2d,
        };

        /// <summary>
        /// The Directions associated with grids of hexagon prisms.
        /// x is right, and z as moving down and left, y is up (prism axis), and w is the same as one unity of x and z.
        /// Note due to some stupid design descisions, you cannot use Direction.WPlus right now.
        /// </summary>
        public static readonly DirectionSet Hexagonal3d = new DirectionSet
        {
            //           X+  X-  Y+  Y-  Z+  Z-  W+  W-
            DX = new[] {  1, -1,  0,  0,  0,  0,  1, -1 },
            DY = new[] {  0,  0,  1, -1,  0,  0,  0,  0 },
            DZ = new[] {  0,  0,  0,  0,  1, -1,  1, -1 },
            Count = 8,
            Type = DirectionSetType.Hexagonal3d,
        };

        /// <summary>
        /// The Directions associated with cubic grids.
        /// </summary>
        public static readonly DirectionSet Cartesian3d = new DirectionSet
        {
            DX = new[] { 1, -1, 0, 0, 0, 0 },
            DY = new[] { 0, 0, 1, -1, 0, 0 },
            DZ = new[] { 0, 0, 0, 0, 1, -1 },
            Count = 6,
            Type = DirectionSetType.Cartesian3d,
        };

        /// <summary>
        /// Given a direction index, returns the direction index that makes the reverse movement.
        /// </summary>
        public Direction Inverse(Direction d)
        {
            return (Direction)((int)d ^ 1);
        }

        public Direction GetDirection(int x, int y, int z=0)
        {
            for (int d = 0; d < Count; d++)
            {
                if (x == DX[d] && y == DY[d] && z == DZ[d])
                {
                    return (Direction)d;
                }
            }
            throw new Exception($"No direction corresponds to ({x}, {y}, {z})");
        }

        public IEnumerator<Direction> GetEnumerator()
        {
            for (int d = 0; d < Count; d++)
            {
                yield return (Direction)d;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
