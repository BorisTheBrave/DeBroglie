namespace DeBroglie
{
    public struct Directions
    {
        public int[] DX { get; private set; }
        public int[] DY { get; private set; }

        public int Count { get; private set; }

        public static readonly Directions Cartesian2d = new Directions
        {
            DX = new[] { 1, -1, 0, 0 },
            DY = new[] { 0, 0, 1, -1 },
            Count = 4,
        };

        public static readonly Directions Hexagonal2d = new Directions
        {
            DX = new[] { 1, -1, 0, 0, 1, -1 },
            DY = new[] { 0, 0, 1, -1, 1, -1 },
            Count = 6,
        };

        public int Inverse(int d)
        {
            return d ^ 1;
        }
    }
}
