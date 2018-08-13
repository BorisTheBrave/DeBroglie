namespace DeBroglie.Topo
{
    /// <summary>
    /// Utility class containing methods for construction <see cref="ITopoArray{T}"/> objects.
    /// </summary>
    public static class TopoArray
    {
        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> from an array. <c>result.Get(x, y) == values[x, y]</c>
        /// </summary>
        public static ITopoArray<T> Create<T>(T[,] values, bool periodic)
        {
            return new TopoArray2D<T>(values, periodic);
        }

        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> from an array. <c>result.Get(x, y) == values[x, y]</c>
        /// </summary>
        public static ITopoArray<T> Create<T>(T[,] values, Topology topology)
        {
            return new TopoArray2D<T>(values, topology);
        }

        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> from an array. <c>result.Get(x, y) == values[y][x].</c>
        /// </summary>
        public static ITopoArray<T> Create<T>(T[][] values, bool periodic)
        {
            return new RaggedTopoArray2D<T>(values, periodic);
        }

        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> from an array. <c>result.Get(x, y) == values[y][x].</c>
        /// </summary>
        public static ITopoArray<T> Create<T>(T[][] values, Topology topology)
        {
            return new RaggedTopoArray2D<T>(values, topology);
        }

        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> from an array. <c>result.Get(x, y, z) == values[x, y, z].</c>
        /// </summary>
        public static ITopoArray<T> Create<T>(T[,,] values, bool periodic)
        {
            return new TopoArray3D<T>(values, periodic);
        }

        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> from an array. <c>result.Get(x, y, z) == values[x, y, z].</c>
        /// </summary>
        public static ITopoArray<T> Create<T>(T[,,] values, Topology topology)
        {
            return new TopoArray3D<T>(values, topology);
        }
    }
}
