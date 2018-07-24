namespace DeBroglie
{

    internal class TopArray2D<T> : ITopArray<T>
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

        public T Get(int x, int y, int z)
        {
            return values[x, y];
        }

        public T Get(int index)
        {
            int x, y, z;
            Topology.GetCoord(index, out x, out y, out z);
            return Get(x, y, z);
        }
    }
}
