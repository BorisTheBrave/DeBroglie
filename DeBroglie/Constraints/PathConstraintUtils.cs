using System;
using System.Collections.Generic;
using DeBroglie.Topo;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Contains utilities relating to <see cref="PathConstraint"/>
    /// </summary>
    public static class PathConstraintUtils
    {
        private static readonly int[] Emtpy = { };

        public static SimpleGraph CreateGraph(ITopology topology)
        {
            var nodeCount = topology.IndexCount;
            var neighbours = new int[nodeCount][];
            for (int i = 0; i < nodeCount; i++)
            {
                if(!topology.ContainsIndex(i))
                {
                    neighbours[i] = Emtpy;
                }

                var n = new List<int>();
                for (var d=0; d < topology.DirectionsCount; d++)
                {
                    if (topology.TryMove(i, (Direction)d, out var dest))
                    {
                        n.Add(dest);
                    }
                }
                neighbours[i] = n.ToArray();
            }

            return new SimpleGraph
            {
                NodeCount = nodeCount,
                Neighbours = neighbours,
            };
        }

        private struct CutVertexFrame
        {
            public int u;
            public int state;
            public int neighbourIndex;
            public bool isRelevantSubtree;
        }

        /// <summary>
        /// First, find the subgraph of graph given by just the walkable vertices.
        /// Then find any point, that if removed, mean there's no path between two
        /// given relevant points.
        /// If it's already not possible to path, then return null.
        /// Note: relevant points themselves are always returned as true.
        /// 
        /// Also optionally returns the extent of the connecteted component containing relevant.
        /// 
        /// If relevant is null, instead returns the points, that if removed, increase the number of
        /// connected components.
        /// 
        /// For an explanation, see:
        /// https://www.boristhebrave.com/2018/04/28/random-paths-via-chiseling/
        /// </summary>
        public static bool[] GetArticulationPoints(SimpleGraph graph, bool[] walkable, bool[] relevant = null, bool[] component = null)
        {
            var indices = walkable.Length;

            if (indices != graph.NodeCount)
                throw new Exception($"Length of walkable doesn't match count of nodes");

            var low = new int[indices];
            var num = 1;
            var dfsNum = new int[indices];
            var isArticulation = new bool[indices];

            // This hideous function is a iterative version
            // of the much more elegant recursive version below.
            // Unfortunately, the recursive version tends to blow the stack for large graphs
            int CutVertex(int initialU)
            {
                var stack = new List<CutVertexFrame>();

                stack.Add(new CutVertexFrame { u = initialU });

                // This is the "returned" value from recursing
                var childRelevantSubtree = false;

                var childCount = 0;

                while(true)
                {
                    var frameIndex = stack.Count - 1;
                    var frame = stack[frameIndex];
                    var u = frame.u;
                    switch(frame.state)
                    {
                        // Initialization
                        case 0:
                            {
                                var isRelevant = relevant != null && relevant[u];
                                if (isRelevant)
                                {
                                    isArticulation[u] = true;
                                }
                                if (component != null)
                                {
                                    component[u] = true;
                                }
                                frame.isRelevantSubtree = isRelevant;
                                low[u] = dfsNum[u] = num++;
                                // Enter loop
                                goto case 1;
                            }
                        // Loop over neighbours
                        case 1:
                            {
                                // Check loop condition
                                var neighbours = graph.Neighbours[u];
                                var neighbourIndex = frame.neighbourIndex;
                                if(neighbourIndex >= neighbours.Length)
                                {
                                    // Exit loop
                                    goto case 3;
                                }
                                var v = neighbours[neighbourIndex];
                                if (!walkable[v])
                                {
                                    // continue to next iteration of loop
                                    frame.neighbourIndex = neighbourIndex + 1;
                                    goto case 1;
                                }
                                
                                // v is a neighbour of u
                                var unvisited = dfsNum[v] == 0;
                                if (unvisited)
                                {
                                    // Recurse into v
                                    stack.Add(new CutVertexFrame { u = v });
                                    frame.state = 2;
                                    stack[frameIndex] = frame;
                                    break;
                                }
                                else
                                {
                                    low[u] = Math.Min(low[u], dfsNum[v]);
                                }

                                // continue to next iteration of loop
                                frame.neighbourIndex = neighbourIndex + 1;
                                goto case 1;
                            }
                        // Return from recursion (still in loop)
                        case 2:
                            {
                                // At this point, childRelevantSubtree
                                // has been set to the by the recursed call we've just returned from
                                var neighbours = graph.Neighbours[u];
                                var neighbourIndex = frame.neighbourIndex;
                                var v = neighbours[neighbourIndex];

                                if (frameIndex == 0)
                                {
                                    // Root frame
                                    childCount++;
                                }

                                if (childRelevantSubtree)
                                {
                                    frame.isRelevantSubtree = true;
                                }
                                if (low[v] >= dfsNum[u])
                                {
                                    if (relevant == null || childRelevantSubtree)
                                    {
                                        isArticulation[u] = true;
                                    }
                                }
                                low[u] = Math.Min(low[u], low[v]);

                                // continue to next iteration of loop
                                frame.neighbourIndex = neighbourIndex + 1;
                                goto case 1;
                            }
                        // Cleanup
                        case 3:
                            if(frameIndex == 0)
                            {
                                // Root frame
                                return childCount;
                            }
                            else
                            {
                                // Set childRelevantSubtree with the return value from this recursed call
                                childRelevantSubtree = frame.isRelevantSubtree;
                                // Pop the frame
                                stack.RemoveAt(frameIndex);
                                // Resume the caller (which will be in state 2)
                                break;
                            }
                    }
                }
            }


            /*
            Tuple<int, bool> cutvertex(int u)
            {
                var childCount = 0;
                var isRelevant = relevant != null && relevant[u];
                if (isRelevant)
                {
                    isArticulation[u] = true;
                }
                if (component != null) 
                {
                    component[u] = true;
                }
                var isRelevantSubtree = isRelevant;
                low[u] = dfsNum[u] = num++;

                foreach (var v in graph.Neighbours[u])
                {
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
            */

            // Find starting point
            for (var i = 0; i < indices; i++)
            {
                if (!walkable[i]) continue;
                if (relevant != null && !relevant[i]) continue;
                // Already visited
                if (dfsNum[i] != 0) continue;
                var childCount = CutVertex(i);
                if(relevant != null)
                {
                    // Relevant points are always articulation points
                    isArticulation[i] = true;
                    // There can only be a single relevant component, so can stop
                    break;
                }
                else
                {
                    // The root of the tree is an exception to CutVertex's calculations
                    // It's an articulation point if it has multiple children
                    // as removing it would give multiple subtrees.
                    isArticulation[i] = childCount > 1;
                }
            }

            // Check we've visited every relevant point.
            // If not, there's no way to satisfy the constraint.
            if (relevant != null)
            {
                for (var i = 0; i < indices; i++)
                {
                    if (relevant[i] && dfsNum[i] == 0)
                    {
                        return null;
                    }
                }
            }

            return isArticulation;
        }


        public class SimpleGraph
        {
            public int NodeCount { get; set; }

            public int[][] Neighbours { get; set; }
        }
    }
}
