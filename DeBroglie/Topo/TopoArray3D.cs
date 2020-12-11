namespace DeBroglie.Topo
{

    internal class TopoArray3D<T> : ITopoArray<T>
    {
        private readonly T[,,] values;

        public TopoArray3D(T[,,] values, bool periodic)
        {
            Topology = new GridTopology(
                values.GetLength(0),
                values.GetLength(1),
                values.GetLength(2),
                periodic);
            this.values = values;
        }

        public TopoArray3D(T[,,] values, ITopology topology)
        {
            Topology = topology;
            this.values = values;
        }

        public ITopology Topology { get; private set; }

        public T Get(int x, int y, int z)
        {
            return values[x, y, z];
        }

        public T Get(int index)
        {
            int x, y, z;
            Topology.GetCoord(index, out x, out y, out z);
            return Get(x, y, z);
        }
    }
}
