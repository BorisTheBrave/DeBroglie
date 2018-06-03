namespace DeBroglie
{
    public struct PatternArray<T>
    {
        public T[,,] Values;

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

        public PatternArray<T> Reflected()
        {
            var width = Width;
            var height = Height;
            var depth = Depth;
            var values = new T[width, height, depth];
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
            return new PatternArray<T> { Values = values };
        }

        public PatternArray<T> Rotated()
        {

            var width = Width;
            var height = Height;
            var depth = Depth;
            var values = new T[height, width, depth];
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
            return new PatternArray<T> { Values = values };
        }
    }

}
