namespace DeBroglie
{
    public struct Directions
    {
        public int[] DX;
        public int[] DY;

        public int Count;

        public static readonly Directions Cartesian2dDirections = new Directions
        {
            DX = new[] { 1, -1, 0, 0 },
            DY = new[] { 0, 0, 1, -1 },
            Count = 4
        };

        public int Inverse(int d)
        {
            return d ^ 1;
        }
    }
}
