namespace DeBroglie.Topo
{
    public static class TopoArray
    {
        public static ITopoArray<T> Create<T>(T[,] values, bool periodic)
        {
            return new TopoArray2D<T>(values, periodic);
        }

        public static ITopoArray<T> Create<T>(T[,] values, Topology topology)
        {
            return new TopoArray2D<T>(values, topology);
        }

        public static ITopoArray<T> Create<T>(T[][] values, bool periodic)
        {
            return new RaggedTopoArray2D<T>(values, periodic);
        }

        public static ITopoArray<T> Create<T>(T[][] values, Topology topology)
        {
            return new RaggedTopoArray2D<T>(values, topology);
        }

        public static ITopoArray<T> Create<T>(T[,,] values, bool periodic)
        {
            return new TopoArray3D<T>(values, periodic);
        }

        public static ITopoArray<T> Create<T>(T[,,] values, Topology topology)
        {
            return new TopoArray3D<T>(values, topology);
        }
    }
}
