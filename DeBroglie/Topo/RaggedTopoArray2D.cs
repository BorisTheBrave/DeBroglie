using System.Linq;

namespace DeBroglie.Topo
{
    internal class RaggedTopoArray2D<T> : ITopoArray<T>
    {
        private readonly T[][] values;

        public RaggedTopoArray2D(T[][] values, bool periodic)
        {
            var height = values.Length;
            var width = values.Max(a => a.Length);
            Topology = new GridTopology(
                width,
                height,
                periodic);
            this.values = values;
        }

        public RaggedTopoArray2D(T[][] values, ITopology topology)
        {
            Topology = topology;
            this.values = values;
        }

        public ITopology Topology { get; private set; }

        public T Get(int x, int y, int z)
        {
            return values[y][x];
        }

        public T Get(int index)
        {
            int x, y, z;
            Topology.GetCoord(index, out x, out y, out z);
            return Get(x, y, z);
        }
    }
}
