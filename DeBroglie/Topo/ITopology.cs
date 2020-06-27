namespace DeBroglie.Topo
{
    /// <summary>
    /// A <see cref="ITopology"/> specifies a discrete area, volume or graph, and provides generic navigation methods.
    /// Topologies are used to support generation in a wide variety of shapes.
    /// Topologies do not actually store data, they just specify the dimensions. Actual data is stored in an <see cref="ITopoArray{T}"/>.
    /// Further information can be found in the documentation.
    /// </summary>
    public interface ITopology
    {
        /// <summary>
        /// Number of unique indices (distinct locations) in the topology
        /// </summary>
        int IndexCount { get; }

        /// <summary>
        /// Number of unique directions
        /// </summary>
        int DirectionsCount { get; }

        /// <summary>
        /// The extent along the x-axis.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// The extent along the y-axis.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// The extent along the z-axis.
        /// </summary>
        int Depth { get; }

        // TODO: We do not need all these overloads, surely

        /// <summary>
        /// Given an index and a direction, gives the index that is one step in that direction,
        /// if it exists and is not masked out. Otherwise, it returns false.
        /// Additionally returns information about the edge traversed.
        /// </summary>
        bool TryMove(int index, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel);

        /// <summary>
        /// Given an index and a direction, gives the index that is one step in that direction,
        /// if it exists and is not masked out. Otherwise, it returns false.
        /// </summary>
        bool TryMove(int index, Direction direction, out int dest);

        /// <summary>
        /// Given a co-ordinate and a direction, gives the index that is one step in that direction,
        /// if it exists and is not masked out. Otherwise, it returns false.
        /// Additionally returns information about the edge traversed.
        /// </summary>
        bool TryMove(int x, int y, int z, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel);

        /// <summary>
        /// Given a co-ordinate and a direction, gives the index that is one step in that direction,
        /// if it exists and is not masked out. Otherwise, it returns false.
        /// </summary>
        bool TryMove(int x, int y, int z, Direction direction, out int dest);

        /// <summary>
        /// Given a co-ordinate and a direction, gives the co-ordinate that is one step in that direction,
        /// if it exists and is not masked out. Otherwise, it returns false.
        /// </summary>
        bool TryMove(int x, int y, int z, Direction direction, out int destx, out int desty, out int destz);

        /// <summary>
        /// A array with one value per index indcating if the value is missing. 
        /// Not all uses of Topology support masks.
        /// </summary>
        bool[] Mask { get; }

        /// <summary>
        /// Reduces a three dimensional co-ordinate to a single integer. This is mostly used internally.
        /// </summary>
        int GetIndex(int x, int y, int z);

        /// <summary>
        /// Inverts <see cref="GetIndex(int, int, int)"/>
        /// </summary>
        void GetCoord(int index, out int x, out int y, out int z);

        /// <summary>
        /// Returns a topology with the same structure as this one,
        /// but with a different mask.
        /// </summary>
        ITopology WithMask(bool[] mask);
    }
}
