namespace DeBroglie.Topo
{
    // TODO: Add comments to this class
    public interface ITopology
    {
        int IndexCount { get; }

        int DirectionsCount { get; }

        int Width { get; }

        int Height { get; }

        int Depth { get; }

        // TODO: We do not need all these overloads, surely
        bool TryMove(int index, Direction direction, out int dest, out Direction inverseDirection);

        bool TryMove(int index, Direction direction, out int dest);

        bool TryMove(int x, int y, int z, Direction direction, out int dest);

        bool TryMove(int x, int y, int z, Direction direction, out int dest, out Direction inverseDirection);

        bool TryMove(int x, int y, int z, Direction direction, out int destx, out int desty, out int destz);

        bool[] Mask { get; }

        int GetIndex(int x, int y, int z);

        void GetCoord(int index, out int x, out int y, out int z);
    }
}
