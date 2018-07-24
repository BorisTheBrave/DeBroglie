namespace DeBroglie
{
    public static class TopArray
    {
        public static ITopArray<T> Create<T>(T[,] values, bool periodic)
        {
            return new TopArray2D<T>(values, periodic);
        }

        public static ITopArray<T> Create<T>(T[,] values, Topology topology)
        {
            return new TopArray2D<T>(values, topology);
        }


        public static ITopArray<T> Create<T>(T[,,] values, bool periodic)
        {
            return new TopArray3D<T>(values, periodic);
        }

        public static ITopArray<T> Create<T>(T[,,] values, Topology topology)
        {
            return new TopArray3D<T>(values, topology);
        }
    }
}
