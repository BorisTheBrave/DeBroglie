namespace DeBroglie
{
    public struct PatternArray<T>
    {
        public T[,] Values;

        public int Width
        {
            get { return Values.GetLength(0); }
        }

        public int Height
        {
            get { return Values.GetLength(1); }
        }

        public PatternArray<T> Reflected()
        {
            var width = Width;
            var height = Height;
            var values = new T[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    values[x, y] = Values[width - 1 - x, y];
                }
            }
            return new PatternArray<T> { Values = values };
        }

        public PatternArray<T> Rotated()
        {

            var width = Width;
            var height = Height;
            var values = new T[height, width];
            for (var x = 0; x < height; x++)
            {
                for (var y = 0; y < width; y++)
                {
                    values[x, y] = Values[width - 1 - y, x];
                }
            }
            return new PatternArray<T> { Values = values };
        }
    }

}
