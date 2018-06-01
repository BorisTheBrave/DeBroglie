namespace DeBroglie
{
    public interface ITopArray<T>
    {
        Topology Topology { get; }

        T Get(int x, int y);

        T Get(int index);
    }
}
