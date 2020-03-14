using System.Collections.Generic;

namespace DeBroglie.Topo
{
    public static class TopologyExtensions
    {
        public static GridTopology AsGridTopology(this ITopology topology)
        {
            if(topology is GridTopology t)
            {
                return t;
            }
            else
            {
                throw new System.Exception("Expected a grid-based topology");
            }
        }

        /// <summary>
        /// Returns true if a given index has not been masked out.
        /// </summary>
        public static bool ContainsIndex(this ITopology topology, int index)
        {
            var mask = topology.Mask;
            return mask == null || mask[index];
        }

        public static IEnumerable<int> GetIndices(this ITopology topology)
        {
            var indexCount = topology.IndexCount;
            var mask = topology.Mask;
            for (var i = 0; i < indexCount; i++)
            {
                if (mask == null || mask[i])
                    yield return i;
            }
        }
    }
}
