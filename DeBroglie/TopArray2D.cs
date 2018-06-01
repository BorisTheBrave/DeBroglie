namespace DeBroglie
{
    public class TopArray2D<T> : ITopArray<T>
    {
        private readonly T[,] values;

        public TopArray2D(T[,] values, bool periodic)
        {
            Topology = new Topology(Directions.Cartesian2d,
                values.GetLength(0),
                values.GetLength(1),
                periodic);
            this.values = values;
        }

        public TopArray2D(T[,] values, Topology topology)
        {
            Topology = topology;
            this.values = values;
        }

        public Topology Topology { get; private set; }

        public T Get(int x, int y)
        {
            return values[x, y];
        }

        public T Get(int index)
        {
            int x, y;
            Topology.GetCoord(index, out x, out y);
            return Get(x, y);
        }

    }
}
