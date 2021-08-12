namespace DeBroglie.Topo
{
    /// <summary>
    /// A read-only array coupled with a specific <see cref="Topology"/>
    /// </summary>
    public interface ITopoArray<out T>
    {
        /// <summary>
        /// Gets the Topology associated with an array
        /// </summary>
        ITopology Topology { get; }

        /// <summary>
        /// Gets the value at a particular location.
        /// </summary>
        T Get(int x, int y, int z = 0);

        /// <summary>
        /// Gets the value at a particular location.
        /// See <see cref="Topology"/> to see how location indices work.
        /// </summary>
        T Get(int index);
    }
}
