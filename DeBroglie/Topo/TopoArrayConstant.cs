namespace DeBroglie.Topo
{
    internal class TopoArrayConstant<T> : ITopoArray<T>
    {
        private readonly T value;

        public TopoArrayConstant(T value, GridTopology topology)
        {
            Topology = topology;
            this.value = value;
        }

        public GridTopology Topology { get; private set; }

        ITopology ITopoArray<T>.Topology => Topology;

        public T Get(int x, int y, int z)
        {
            return value;
        }

        public T Get(int index)
        {
            return value;
        }
    }
}
