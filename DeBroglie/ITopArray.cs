namespace DeBroglie
{
    public interface ITopArray<T>
    {
        Topology Topology { get; }

        T Get(int x, int y, int z = 0);

        T Get(int index);
    }
}
