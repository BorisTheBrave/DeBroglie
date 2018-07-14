namespace DeBroglie
{
    public struct PatternArray
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

        public PatternArray Reflected()
        {
            var width = Width;
            var height = Height;
            var depth = Depth;
            var values = new Tile[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        values[x, y, z] = Values[width - 1 - x, y, z];
                    }
                }
            }
            return new PatternArray { Values = values };
        }

        public PatternArray Rotated()
        {

            var width = Width;
            var height = Height;
            var depth = Depth;
            var values = new Tile[height, width, depth];
            for (var x = 0; x < height; x++)
            {
                for (var y = 0; y < width; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        values[x, y, z] = Values[width - 1 - y, x, z];
                    }
                }
            }
            return new PatternArray { Values = values };
        }
    }

}
