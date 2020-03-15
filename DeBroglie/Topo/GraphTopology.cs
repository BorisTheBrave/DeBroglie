namespace DeBroglie.Topo
{
    public class GraphTopology : ITopology
    {
        readonly int indexCount;
        readonly int directionsCount;

        // By index, direction
        readonly NeighbourDetails[,] neighbours;

        public GraphTopology(NeighbourDetails[,] neighbours)
        {
            indexCount = neighbours.GetLength(0);
            directionsCount = neighbours.GetLength(1);
            this.neighbours = neighbours;
        }

        public int IndexCount => indexCount;

        public int DirectionsCount => directionsCount;

        public int Width => indexCount;

        public int Height => 1;

        public int Depth => 1;

        public bool[] Mask => null;// Not supported for now?

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

        public struct NeighbourDetails
        {
            // Set to -1 to indicate no neighbour
            public int Index { get; set; }
            public EdgeLabel EdgeLabel { get; set; }
            public Direction InverseDirection { get; set; }
        }
    }
}
