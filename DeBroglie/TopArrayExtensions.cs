using System;

namespace DeBroglie
{
    public static class TopArrayExtensions
    {
        public static T[,] ToArray2d<T>(this ITopArray<T> topArray)
        {
            var width = topArray.Topology.Width;
            var height = topArray.Topology.Height;
            var results = new T[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    results[x, y] = topArray.Get(x, y);
                }
            }
            return results;
        }

        public static T[,,] ToArray3d<T>(this ITopArray<T> topArray)
        {
            var width = topArray.Topology.Width;
            var height = topArray.Topology.Height;
            var depth = topArray.Topology.Depth;
            var results = new T[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        results[x, y, z] = topArray.Get(x, y, z);
                    }
                }
            }
            return results;
        }

        public static ITopArray<U> Map<T, U>(this ITopArray<T> topArray, Func<T, U> func)
        {
            var width = topArray.Topology.Width;
            var height = topArray.Topology.Height;
            var depth = topArray.Topology.Depth;
            var r = new U[width, height, depth];

            for (var z = 0; z < depth; z++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        r[x, y, z] = func(topArray.Get(x, y, z));
                    }
                }
            }

            return new TopArray3D<U>(r, topArray.Topology);
        }

        public static ITopArray<Tile> ToTiles<T>(this ITopArray<T> topArray)
        {
            return topArray.Map(v => new Tile(v));
        }
    }
}
