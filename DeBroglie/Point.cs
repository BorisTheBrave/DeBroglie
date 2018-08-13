namespace DeBroglie
{
    /// <summary>
    /// Represents a location in a topology.
    /// </summary>
    public struct Point
    {
        public int X;
        public int Y;
        public int Z;

        public Point(int x, int y, int z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
