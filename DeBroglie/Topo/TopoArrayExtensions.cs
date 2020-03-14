using System;

namespace DeBroglie.Topo
{
    public static class TopoArrayExtensions
    {
        /// <summary>
        /// Copies a <see cref="ITopoArray{T}"/> into a 2d array.
        /// </summary>
        public static T[,] ToArray2d<T>(this ITopoArray<T> topoArray)
        {
            var width = topoArray.Topology.Width;
            var height = topoArray.Topology.Height;
            var results = new T[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    results[x, y] = topoArray.Get(x, y);
                }
            }
            return results;
        }

        /// <summary>
        /// Copies a <see cref="ITopoArray{T}"/> into a 3d array.
        /// </summary>
        public static T[,,] ToArray3d<T>(this ITopoArray<T> topoArray)
        {
            var width = topoArray.Topology.Width;
            var height = topoArray.Topology.Height;
            var depth = topoArray.Topology.Depth;
            var results = new T[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        results[x, y, z] = topoArray.Get(x, y, z);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Calls <c>func</c> on each element of the array, returning a new <see cref="ITopoArray{T}"/>
        /// </summary>
        public static ITopoArray<U> Map<T, U>(this ITopoArray<T> topoArray, Func<T, U> func)
        {
            /*
            var width = topoArray.Topology.Width;
            var height = topoArray.Topology.Height;
            var depth = topoArray.Topology.Depth;
            var r = new U[width, height, depth];

            for (var z = 0; z < depth; z++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        r[x, y, z] = func(topoArray.Get(x, y, z));
                    }
                }
            }

            return new TopoArray3D<U>(r, topoArray.Topology);
            */
            var r = new U[topoArray.Topology.IndexCount];
            foreach(var i in topoArray.Topology.GetIndices())
            {
                r[i] = func(topoArray.Get(i));
            }
            return new TopoArray1D<U>(r, topoArray.Topology);
        }

        /// <summary>
        /// Wraps each element of an <see cref="ITopoArray{T}"/> in a <see cref="Tile"/> struct, 
        /// so it can be consumed by other DeBroglie classes.
        /// </summary>
        public static ITopoArray<Tile> ToTiles<T>(this ITopoArray<T> topoArray)
        {
            return topoArray.Map(v => new Tile(v));
        }
    }
}
