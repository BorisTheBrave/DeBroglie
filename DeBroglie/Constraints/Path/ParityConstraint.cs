using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// This constraint doesn't actually constrain anything, it improves the quality for the search.
    /// It only works for tilesets that entirely, or predominantly, have even numbers of exits (i.e. straights, corners, crossroads, but not forks or deadends).
    /// These sorts of tilesets can be challenging for WFC without the additional guidance this constraint supplies.
    /// 
    /// The parity of any set of tiles is defined as the even/oddness of the sum over those tiles of path exits.
    /// Let's assume that path exits are symmetric - x has an exit to adjacent tile y if y has an exit to x.
    /// Then the parity of a connected region of cells is often computable even before all the tiles are selected.
    /// The parity constraint uses this to lookahead and determine what tiles must be placed. 
    /// 
    /// This constraint is experimental.
    /// </summary>
    public class ParityConstraint : ITileConstraint
    {
        private TilePropagatorTileSet oddPathTilesSet;
        private SelectedTracker oddPathTracker;
        private EdgedPathView pathView;
        private TilePropagator propagator;
        private ITopology topology;


        public EdgedPathSpec PathSpec { get; set; }

        public void Init(TilePropagator propagator)
        {
            var oddPathTiles = PathSpec.Exits.Where(x => x.Value.Count() % 2 == 1).Select(x=>x.Key).ToList();
            oddPathTilesSet = propagator.CreateTileSet(oddPathTiles);
            oddPathTracker = oddPathTiles.Count > 0 ? propagator.CreateSelectedTracker(oddPathTilesSet) : null;
            pathView = (EdgedPathView)PathSpec.MakeView(propagator);
            this.propagator = propagator;
            this.topology = propagator.Topology;
        }

        public void Check(TilePropagator propagator)
        {
            var visited = new bool[topology.IndexCount];

            foreach(var i in topology.GetIndices())
            {
                VisitIndex(i, visited);
            }
        }

        public void VisitIndex(int index, bool[] visited)
        {
            if (visited[index]) return;

            var visitedList = new List<int>();
            var parityCount = 0;

            (int, Direction?)? amigLocation = null;
            var hasMultipleAmbiguous = false;


            var stack = new Stack<int>();
            stack.Push(index);
            while (stack.Count > 0)
            {
                var i = stack.Pop();

                if (visited[i]) continue;

                visited[i] = true;
                visitedList.Add(i);

                var isOdd = oddPathTracker?.GetQuadstate(i) ?? Quadstate.No;

                // Does this tile have undefined parity in number of exits
                if (isOdd.IsMaybe())
                {
                    // Ambiguous
                    if (amigLocation != null)
                    {
                        hasMultipleAmbiguous = true;
                    }
                    else
                    {
                        amigLocation = (i, null);
                    }
                }
                if(isOdd.IsYes())
                {
                    parityCount++;
                }

                for (var d = 0; d < topology.DirectionsCount; d++)
                {
                    var direction = (Direction)d;
                    var qs = pathView.TrackerByExit.TryGetValue(direction, out var tracker) ? tracker.GetQuadstate(i) : Quadstate.No;
                    if (qs.IsYes()) parityCount++;
                    if (qs.IsMaybe())
                    {
                        if (topology.TryMove(i, direction, out var i2))
                        {
                            // Need to include this cell in the region
                            if (!visited[i2])
                            {
                                stack.Push(i2);
                            }
                        }
                        else
                        {
                            // If there's no corresponding tile to balance this one
                            // then this exit is free to be path or not, altering parity

                            if (amigLocation != null)
                            {
                                hasMultipleAmbiguous = true;
                            }
                            else
                            {
                                amigLocation = (i, direction);
                            }
                        }
                    }
                }
            }

            // We've now fully explored this region

            if (hasMultipleAmbiguous)
            {
                // There's nothing we can say about this case.
                return;
            }

            if (amigLocation != null)
            {
                // There's exactly one ambiguous point, so set it to ensure even parity
                var (ambigIndex, ambigDirection) = amigLocation.Value;
                topology.GetCoord(ambigIndex, out var x, out var y, out var z);
                if (ambigDirection == null)
                {
                    if (parityCount % 2 == 0)
                    {
                        propagator.Ban(x, y, z, oddPathTilesSet);
                    }
                    else
                    {
                        propagator.Select(x, y, z, oddPathTilesSet);
                    }
                }
                else
                {
                    if (parityCount % 2 == 0)
                    {
                        propagator.Ban(x, y, z, pathView.TileSetByExit[ambigDirection.Value]);
                    }
                    else
                    {
                        propagator.Select(x, y, z, pathView.TileSetByExit[ambigDirection.Value]);
                    }
                }
            }
            else
            {
                if (parityCount % 2 == 0)
                {
                    // This is fine
                }
                else
                {
                    // This is not fine, and there's nothing ambiguous to patch things up
                    propagator.SetContradiction("Parity constraint detected unfixable region of odd parity", this);
                }
            }
        }
    }
}
