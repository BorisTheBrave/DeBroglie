using System;
using System.Linq;

namespace DeBroglie
{

    public class PathConstraint : IWaveConstraint
    {
        // Set of patterns that are considered on the path
        public int[] PathPatterns { get; set; }

        // Set of points that must be connected by paths.
        // If null, then PathConstraint ensures that all path cells
        // are connected.
        public Point[] EndPoints { get; set; }

        public PathConstraint(int[] pathPatterns, Point[] endPoints = null)
        {
            this.PathPatterns = pathPatterns;
            this.EndPoints = endPoints;
        }

        public CellStatus Check(WavePropagator wp)
        {
            var wave = wp.Wave;
            var indices = wp.Indices;
            // Initialize couldBePath and mustBePath based on wave possibilities
            var couldBePath = new bool[indices];
            var mustBePath = new bool[indices];
            for (int i = 0; i < indices; i++)
            {
                var couldBe = false;
                var mustBe = true;
                for (int p = 0; p < wp.PatternCount; p++)
                {
                    if (wave.Get(i, p))
                    {
                        if (PathPatterns.Contains(p))
                            couldBe = true;
                        if (!PathPatterns.Contains(p))
                            mustBe = false;
                    }
                }
                couldBePath[i] = couldBe;
                mustBePath[i] = mustBe;
            }

            // Select relevant cells, i.e. those that must be connected.
            bool[] relevant;
            if(EndPoints == null)
            {
                relevant = mustBePath;
            }
            else
            {
                relevant = new bool[indices];
                if (EndPoints.Length == 0)
                    return CellStatus.Undecided;
                foreach(var endPoint in EndPoints)
                {
                    var index = wp.GetIndex(endPoint.X, endPoint.Y);
                    relevant[index] = true;
                }
            }

            // Find articulation points.
            // For an explanation, see:
            // https://www.boristhebrave.com/2018/04/28/random-paths-via-chiseling/
            var walkable = couldBePath;

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

                int ux, uy;
                wp.GetCoord(u, out ux, out uy);
                for (var d = 0; d < wp.Directions.Count; d++)
                {
                    int v;
                    if(!wp.TryMove(ux, uy, d, out v))
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
                    return CellStatus.Contradiction;
                }
            }

            // All articulation points must be paths,
            // So ban any other possibilities
            for (var i = 0; i < indices; i++)
            {
                if (!isArticulation[i])
                {
                    continue;
                }
                for (int p = 0; p < wp.PatternCount; p++)
                {
                    if (!PathPatterns.Contains(p) && wave.Get(i, p))
                    {
                        wp.InternalBan(i, p);
                    }
                }
            }

            return CellStatus.Undecided;
        }

        public static PathConstraint Create<T>(TileModel<T> overlappingModel, T[] pathTiles, Point[] endPoints)
        {
            var pathPatterns = pathTiles
                .SelectMany(t => overlappingModel.TilesToPatterns[t])
                .ToArray();
            return new PathConstraint(pathPatterns, endPoints);
        }
    }
}
