using System;

namespace DeBroglie.Topo
{
    /// <summary>
    /// Utility class containing methods for construction <see cref="ITopoArray{T}"/> objects.
    /// </summary>
    public static class TopoArray
    {
        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> from an array. <c>result.Get(i) == values[i]</c>
        /// </summary>
        public static ITopoArray<T> Create<T>(T[] values, ITopology topology)
        {
            return new TopoArray1D<T>(values, topology);
        }

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
        public static ITopoArray<T> Create<T>(T[,] values, ITopology topology)
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
        public static ITopoArray<T> Create<T>(T[][] values, ITopology topology)
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
        public static ITopoArray<T> Create<T>(T[,,] values, ITopology topology)
        {
            return new TopoArray3D<T>(values, topology);
        }

        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> from an array. <c>result.Get(x, y, z) == values[x, y, z].</c>
        /// </summary>
        public static ITopoArray<T> FromConstant<T>(T value, ITopology topology)
        {
            return new TopoArrayConstant<T>(value, topology);
        }

        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> by invoking f at each location in the topology.
        /// </summary>
        public static ITopoArray<T> CreateByPoint<T>(Func<Point, T> f, ITopology topology)
        {
            var array = new T[topology.Width, topology.Height, topology.Depth];
            for (var z = 0; z < topology.Depth; z++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var x = 0; x < topology.Width; x++)
                    {
                        var index = topology.GetIndex(x, y, z);
                        if (topology.ContainsIndex(index))
                        {
                            array[x, y, z] = f(new Point(x, y, z));
                        }
                    }
                }
            }
            return Create(array, topology);
        }

        /// <summary>
        /// Constructs an <see cref="ITopoArray{T}"/> by invoking f at each location in the topology.
        /// </summary>
        public static ITopoArray<T> CreateByIndex<T>(Func<int, T> f, ITopology topology)
        {
            var array = new T[topology.IndexCount];
            foreach (var i in topology.GetIndices())
            {
                array[i] = f(i);
            }
            return Create(array, topology);
        }
    }
}
