namespace DeBroglie.Models
{
    internal struct PatternArray
    {
        public Tile[,,] Values;

        public int Width
        {
            get { return Values.GetLength(0); }
        }

        public int Height
        {
            get { return Values.GetLength(1); }
        }

        public int Depth
        {
            get { return Values.GetLength(2); }
        }
    }

}
