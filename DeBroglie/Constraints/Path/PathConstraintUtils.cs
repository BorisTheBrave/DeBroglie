using System;
using System.Collections.Generic;
using DeBroglie.Topo;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Contains utilities relating to <see cref="ConnectedConstraint"/>
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
            public int relevantChildSubtreeCount;
        }

        public class AtrticulationPointsInfo
        {
            public bool[] IsArticulation { get; set; }
            public int ComponentCount { get; set; }
            public int?[] Component { get; set; }
        }

        /// <summary>
        /// First, find the subgraph of graph given by just the walkable vertices.
        /// <paramref name="relevant"/> defaults to walkable if null.
        /// 
        /// A cut-vertex is defined as any point that, if removed, there exist two other relevant points
        /// that no longer have a path.
        /// 
        /// For an explanation, see:
        /// https://www.boristhebrave.com/2018/04/28/random-paths-via-chiseling/
        /// </summary>
        public static AtrticulationPointsInfo GetArticulationPoints(SimpleGraph graph, bool[] walkable, bool[] relevant = null)
        {
            var indices = walkable.Length;

            if (indices != graph.NodeCount)
                throw new Exception($"Length of walkable doesn't match count of nodes");

            // TODO: Restructe so we don't need construct these fresh every time.
            var low = new int[indices];
            var num = 1;
            var dfsNum = new int[indices];
            var isArticulation = new bool[indices];
            var component = new int?[indices];
            var currentComponent = 0;

            // This hideous function is a iterative version
            // of the much more elegant recursive version below.
            // Unfortunately, the recursive version tends to blow the stack for large graphs
            int CutVertex(int initialU)
            {
                var stack = new List<CutVertexFrame>();

                stack.Add(new CutVertexFrame { u = initialU });

                // This is the "returned" value from recursing
                var childRelevantSubtree = false;

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
                                component[u] = currentComponent;
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

                                if (childRelevantSubtree)
                                {
                                    frame.relevantChildSubtreeCount++;
                                }
                                if (low[v] >= dfsNum[u])
                                {
                                    if (childRelevantSubtree)
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
                                return frame.relevantChildSubtreeCount;
                            }
                            else
                            {
                                // Set childRelevantSubtree with the return value from this recursed call
                                var isRelevant = relevant == null || relevant[u];
                                var descendantOrSelfIsRelevant = frame.relevantChildSubtreeCount > 0 || isRelevant;
                                childRelevantSubtree = descendantOrSelfIsRelevant;
                                // Pop the frame
                                stack.RemoveAt(frameIndex);
                                // Resume the caller (which will be in state 2)
                                break;
                            }
                    }
                }
            }

            Tuple<int, bool> cutvertex(int u)
            {
                
                var relevantChildSubtreeCount = 0;
                component[u] = currentComponent;
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
                        // v is a child of u
                        var relevantChildSubtree = cutvertex(v).Item2;
                        if (relevantChildSubtree)
                        {
                            relevantChildSubtreeCount++;
                        }
                        if (low[v] >= dfsNum[u])
                        {
                            if (relevantChildSubtree)
                            {
                                isArticulation[u] = true;
                            }
                        }
                        low[u] = Math.Min(low[u], low[v]);
                    }
                    else
                    {
                        // v is an ancestor of u
                        low[u] = Math.Min(low[u], dfsNum[v]);
                    }
                }
                var isRelevant = relevant == null || relevant[u];
                var descendantOrSelfIsRelevant = relevantChildSubtreeCount > 0 || isRelevant;
                return Tuple.Create(relevantChildSubtreeCount, descendantOrSelfIsRelevant);
            }

            // Find starting point
            for (var i = 0; i < indices; i++)
            {
                if (!walkable[i]) continue;
                // Only consider relevant nodes for root.
                // (this is a precondition of cutvertex)
                if (relevant != null && !relevant[i]) continue;
                // Already visited
                if (dfsNum[i] != 0) continue;

                var relevantChildSubtreeCount = CutVertex(i);
                //var relevantChildSubtreeCount = cutvertex(i).Item1;
                isArticulation[i] = relevantChildSubtreeCount > 1;
                currentComponent++;
            }

            return new AtrticulationPointsInfo
            {
                IsArticulation = isArticulation,
                Component = component,
                ComponentCount = currentComponent,
            };
        }


        public class SimpleGraph
        {
            public int NodeCount { get; set; }

            public int[][] Neighbours { get; set; }
        }
    }
}
