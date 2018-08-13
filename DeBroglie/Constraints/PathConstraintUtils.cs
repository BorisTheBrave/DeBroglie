using System;
using DeBroglie.Topo;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Contains utilities relating to <see cref="PathConstraint"/>
    /// </summary>
    public static class PathConstraintUtils
    {
        /// <summary>
        /// Find articulation points.
        /// For an explanation, see:
        /// https://www.boristhebrave.com/2018/04/28/random-paths-via-chiseling/
        /// </summary>
        public static bool[] GetArticulationPoints(Topology topology, bool[] walkable, bool[] relevant)
        {
            var indices = walkable.Length;

            var low = new int[indices];
            var num = 1;
            var dfsNum = new int[indices];
            var isArticulation = new bool[indices];

            Tuple<int, bool> cutvertex(int u)
            {
                var childCount = 0;
                var isRelevant = relevant != null && relevant[u];
                if (isRelevant)
                {
                    isArticulation[u] = true;
                }
                var isRelevantSubtree = isRelevant;
                low[u] = dfsNum[u] = num++;

                int ux, uy, uz;
                topology.GetCoord(u, out ux, out uy, out uz);
                for (var d = 0; d < topology.Directions.Count; d++)
                {
                    int v;
                    if (!topology.TryMove(ux, uy, uz, d, out v))
                    {
                        continue;
                    }
                    if (!walkable[v])
                    {
                        continue;
                    }
                    // v is a neighbour of u
                    var unvisited = dfsNum[v] == 0;
                    if (unvisited)
                    {
                        var childRelevantSubtree = cutvertex(v).Item2;
                        childCount++;
                        if (childRelevantSubtree)
                        {
                            isRelevantSubtree = true;
                        }
                        if (low[v] >= dfsNum[u])
                        {
                            if (relevant == null || childRelevantSubtree)
                            {
                                isArticulation[u] = true;
                            }
                        }
                        low[u] = Math.Min(low[u], low[v]);
                    }
                    else
                    {
                        low[u] = Math.Min(low[u], dfsNum[v]);
                    }
                }
                return Tuple.Create(childCount, isRelevantSubtree);
            }

            // Find starting point
            for (var i = 0; i < indices; i++)
            {
                if (!walkable[i]) continue;
                if (relevant != null && !relevant[i]) continue;
                var childCount = cutvertex(i).Item1;
                isArticulation[i] = childCount > 1 || relevant != null;
                if (isArticulation[i])
                    break;
            }

            // Check we've visited every relevant point.
            // If not, there's no way to satisfy the constraint.
            for (var i = 0; i < indices; i++)
            {
                if (relevant != null && relevant[i] && dfsNum[i] == 0)
                {
                    return null;
                }
            }

            return isArticulation;
        }
    }
}
