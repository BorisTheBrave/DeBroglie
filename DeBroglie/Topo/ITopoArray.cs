namespace DeBroglie.Topo
{
    public interface ITopoArray<T>
    {
        Topology Topology { get; }

        T Get(int x, int y, int z = 0);

        T Get(int index);
    }
}
