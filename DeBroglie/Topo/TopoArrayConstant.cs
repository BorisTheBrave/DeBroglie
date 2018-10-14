namespace DeBroglie.Topo
{
    internal class TopoArrayConstant<T> : ITopoArray<T>
    {
        private readonly T value;

        public TopoArrayConstant(T value, Topology topology)
        {
            Topology = topology;
            this.value = value;
        }

        public Topology Topology { get; private set; }

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
