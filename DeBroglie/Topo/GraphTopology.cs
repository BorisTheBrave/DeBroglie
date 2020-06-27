using DeBroglie.Rot;

namespace DeBroglie.Topo
{
    /// <summary>
    /// Summarises the type of graph 
    /// </summary>
    public class GraphInfo
    {
        /// <summary>
        /// The degree of the graph (max number of neighbours)
        /// </summary>
        public int DirectionsCount { get; set; }

        /// <summary>
        /// Lists the number of distinct edge labels this graph uses.
        /// </summary>
        public int EdgeLabelCount { get; set; }

        /// <summary>
        /// Optionally specifies how edge labels correspond to directions.
        /// It's usually easiest to create this with <see cref="MeshTopologyBuilder"/>
        /// </summary>
        public (Direction, Direction, Rotation)[] EdgeLabelInfo { get; set; }
    }

    /// <summary>
    /// An generic topology representing any graph data strcture with a given maximum degree.
    /// The x-axis is the same as the index, indicating which vertex in the graph. y and z are unused and are always zero.
    /// </summary>
    public class GraphTopology : ITopology
    {
        private readonly int indexCount;
        private readonly int directionsCount;
        private readonly bool[] mask;

        // By index, direction
        private readonly NeighbourDetails[,] neighbours;

        /// <summary>
        /// Constructs a new GraphTopology.
        /// neighbours[i, d] should be an array indicating what the d'th neighbour of the i'th node is.
        /// d should be a Direction, cast to an integer.
        /// </summary>
        /// <param name="neighbours"></param>
        public GraphTopology(NeighbourDetails[,] neighbours, bool[] mask = null)
        {
            indexCount = neighbours.GetLength(0);
            directionsCount = neighbours.GetLength(1);
            this.neighbours = neighbours;
            this.mask = mask;
        }

        public int IndexCount => indexCount;

        public int DirectionsCount => directionsCount;

        public int Width => indexCount;

        public int Height => 1;

        public int Depth => 1;

        public bool[] Mask => mask;

        public GraphTopology WithMask(bool[] mask)
        {
            return new GraphTopology(neighbours, mask);
        }

        ITopology ITopology.WithMask(bool[] mask)
        {
            return WithMask(mask);
        }

        public void GetCoord(int index, out int x, out int y, out int z)
        {
            x = index;
            y = 0;
            z = 0;
        }

        public int GetIndex(int x, int y, int z)
        {
            return x;
        }

        public bool TryMove(int index, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel)
        {
            var neighbour = neighbours[index, (int)direction];
            dest = neighbour.Index;
            inverseDirection = neighbour.InverseDirection;
            edgeLabel = neighbour.EdgeLabel;
            return neighbour.Index >= 0;
        }

        public bool TryMove(int index, Direction direction, out int dest)
        {
            var neighbour = neighbours[index, (int)direction];
            dest = neighbour.Index;
            return neighbour.Index >= 0;
        }

        public bool TryMove(int x, int y, int z, Direction direction, out int dest)
        {
            var neighbour = neighbours[x, (int)direction];
            dest = neighbour.Index;
            return neighbour.Index >= 0;
        }

        public bool TryMove(int x, int y, int z, Direction direction, out int dest, out Direction inverseDirection, out EdgeLabel edgeLabel)
        {
            var neighbour = neighbours[x, (int)direction];
            dest = neighbour.Index;
            inverseDirection = neighbour.InverseDirection;
            edgeLabel = neighbour.EdgeLabel;
            return neighbour.Index >= 0;
        }

        public bool TryMove(int x, int y, int z, Direction direction, out int destx, out int desty, out int destz)
        {
            var neighbour = neighbours[x, (int)direction];
            destx = neighbour.Index;
            desty = 0;
            destz = 0;
            return neighbour.Index >= 0;
        }

        /// <summary>
        /// Describes a single neighbour of a node.
        /// (also called a half-edge in some literature).
        /// </summary>
        public struct NeighbourDetails
        {
            /// <summary>
            /// Where this edge leads to.
            /// Set to -1 to indicate no neighbour.
            /// </summary>
            public int Index { get; set; }
            
            /// <summary>
            /// The edge label of this edge
            /// </summary>
            public EdgeLabel EdgeLabel { get; set; }

            /// <summary>
            /// The direction to move from Index which will return back along this edge.
            /// </summary>
            public Direction InverseDirection { get; set; }
        }
    }
}
