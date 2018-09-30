using System;

namespace DeBroglie.Topo
{
    /// <summary>
    /// DirectionType indicates what neighbors are considered adjacent to each tile.
    /// </summary>
    public enum DirectionsType
    {
        Unknown,
        Cartesian2d,
        Hexagonal2d,
        Cartesian3d,
    }

    /// <summary>
    /// Wrapper around DirectionsType supplying some convenience data.
    /// </summary>
    public struct Directions
    {
        public int[] DX { get; private set; }
        public int[] DY { get; private set; }
        public int[] DZ { get; private set; }

        public int Count { get; private set; }

        public DirectionsType Type { get; private set; }

        /// <summary>
        /// The Directions associated with square grids.
        /// </summary>
        public static readonly Directions Cartesian2d = new Directions
        {
            DX = new[] { 1, -1, 0, 0 },
            DY = new[] { 0, 0, 1, -1 },
            DZ = new[] { 0, 0, 0, 0 },
            Count = 4,
            Type = DirectionsType.Cartesian2d,
        };

        /// <summary>
        /// The Directions associated with hexagonal grids.
        /// Conventially, x is treated as moving right, and y as moving down and left,
        /// But the same Directions object will work just as well will several other conventions
        /// as long as you are consistent.
        /// </summary>
        public static readonly Directions Hexagonal2d = new Directions
        {
            DX = new[] { 1, -1, 0, 0, 1, -1 },
            DY = new[] { 0, 0, 1, -1, 1, -1 },
            DZ = new[] { 0, 0, 0, 0, 0, 0 },
            Count = 6,
            Type = DirectionsType.Hexagonal2d,
        };

        /// <summary>
        /// The Directions associated with cubic grids.
        /// </summary>
        public static readonly Directions Cartesian3d = new Directions
        {
            DX = new[] { 1, -1, 0, 0, 0, 0 },
            DY = new[] { 0, 0, 1, -1, 0, 0 },
            DZ = new[] { 0, 0, 0, 0, 1, -1 },
            Count = 6,
            Type = DirectionsType.Cartesian3d,
        };

        /// <summary>
        /// Given a direction index, returns the direction index that makes the reverse movement.
        /// </summary>
        public int Inverse(int d)
        {
            return d ^ 1;
        }

        public int GetDirection(int x, int y, int z=0)
        {
            for(int d=0;d<Count;d++)
            {
                if(x == DX[d] && y == DY[d] && z == DZ[d])
                {
                    return d;
                }
            }
            throw new Exception($"No direction corresponds to ({x}, {y}, {z})");
        }
    }
}
