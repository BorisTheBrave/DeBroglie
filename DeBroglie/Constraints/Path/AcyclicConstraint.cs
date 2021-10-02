using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Enforces that there are no loops at all
    /// </summary>
    public class AcyclicConstraint : ITileConstraint
    {
        private IPathView pathView;

        public IPathSpec PathSpec { get; set; }

        public void Init(TilePropagator propagator)
        {
            pathView = PathSpec.MakeView(propagator);
            pathView.Init();
        }

        public void Check(TilePropagator propagator)
        {
            pathView.Update();

            var graph = pathView.Graph;
            var mustBePath = pathView.MustBePath;
            // TODO: Support relevant?
            var visited = new bool[graph.NodeCount];
            for (var i = 0; i < graph.NodeCount; i++)
            {
                if (!mustBePath[i]) continue;
                if (visited[i]) continue;

                // Start DFS
                var stack = new Stack<(int, int)>();
                stack.Push((-1, i));
                while(stack.Count > 0)
                {
                    var (prev, u) = stack.Pop();
                    if(visited[u])
                    {
                        propagator.SetContradiction("Acyclic constraint found cycle", this);
                        return;
                    }
                    visited[u] = true;
                    foreach(var v in graph.Neighbours[u])
                    {
                        if (!mustBePath[v]) continue;
                        if (v == prev) continue;
                        stack.Push((u, v));
                    }
                }
            }

        }
    }
}
