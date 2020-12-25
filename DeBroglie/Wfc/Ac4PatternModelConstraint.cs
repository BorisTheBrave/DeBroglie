using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeBroglie.Topo;

namespace DeBroglie.Wfc
{
    /// <summary>
    /// Implements pattern adjacency propagation using the arc consistency 4 algorithm.
    /// 
    /// Roughly speaking, this algorith keeps a count for each cell/pattern/direction of the "support",
    /// i.e. how many possible cells could adjoin that particular pattern.
    /// This count can be straightforwardly updated, and when it drops to zero, we know that that cell/pattern is not possible, and can be banned.
    /// </summary>
    internal class Ac4PatternModelConstraint : IPatternModelConstraint
    {
        // From model
        private int[][][] propagatorArray;
        private int patternCount;

        // Re-organized propagatorArray
        private BitArray[][] propagatorArrayDense;

        // Useful values
        private readonly WavePropagator propagator;
        private readonly int directionsCount;
        private readonly ITopology topology;
        private int indexCount;

        // List of locations that still need to be checked against for fulfilling the model's conditions
        private Stack<IndexPatternItem> toPropagate;

        /**
          * compatible[index, pattern, direction] contains the number of patterns present in the wave
          * that can be placed in the cell next to index in direction without being
          * in contradiction with pattern placed in index.
          * If possibilites[index][pattern] is set to false, then compatible[index, pattern, direction] has every direction negative or null
          */
        private int[,,] compatible;

        public Ac4PatternModelConstraint(WavePropagator propagator, PatternModel model)
        {
            this.toPropagate = new Stack<IndexPatternItem>();
            this.propagator = propagator;

            this.propagatorArray = model.Propagator;
            this.patternCount = model.PatternCount;

            this.propagatorArrayDense = model.Propagator.Select(a1 => a1.Select(x =>
            {
                var dense = new BitArray(patternCount);
                foreach (var p in x) dense[p] = true;
                return dense;
            }).ToArray()).ToArray();

            this.topology = propagator.Topology;
            this.indexCount = topology.IndexCount;
            this.directionsCount = topology.DirectionsCount;
        }

        public void Clear()
        {
            toPropagate.Clear();

            compatible = new int[indexCount, patternCount, directionsCount];

            var edgeLabels = new int[directionsCount];

            for (int index = 0; index < indexCount; index++)
            {
                if (!topology.ContainsIndex(index))
                    continue;

                // Cache edgeLabels
                for (int d = 0; d < directionsCount; d++)
                {
                    edgeLabels[d] = topology.TryMove(index, (Direction)d, out var dest, out var _, out var el) ? (int)el : -1;
                }

                for (int pattern = 0; pattern < patternCount; pattern++)
                {
                    for (int d = 0; d < directionsCount; d++)
                    {
                        var el = edgeLabels[d];
                        if (el >= 0)
                        {
                            var compatiblePatterns = propagatorArray[pattern][el].Length;
                            compatible[index, pattern, d] = compatiblePatterns;
                            if (compatiblePatterns == 0 && propagator.Wave.Get(index, pattern))
                            {
                                if (propagator.InternalBan(index, pattern))
                                {
                                    propagator.SetContradiction();
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Precondition that pattern at index is possible.
        public void DoBan(int index, int pattern)
        {
            // Update compatible (so that we never ban twice)
            for (var d = 0; d < directionsCount; d++)
            {
                compatible[index, pattern, d] -= patternCount;
            }
            // Queue any possible consequences of this changing.
            toPropagate.Push(new IndexPatternItem
            {
                Index = index,
                Pattern = pattern,
            });
        }

        // This is equivalent to calling DoBan on every possible pattern
        // except the passed in one.
        // But it is more efficient.
        // Precondition that pattern at index is possible.
        public void DoSelect(int index, int pattern)
        {
            // Update compatible (so that we never ban twice)
            for (var p = 0; p < patternCount; p++)
            {
                if (p == pattern)
                    continue;
                for (var d = 0; d < directionsCount; d++)
                {
                    if (compatible[index, p, d] > 0)
                    {
                        compatible[index, p, d] -= patternCount;
                    }
                }
            }

            // Queue any possible consequences of this changing.
            toPropagate.Push(new IndexPatternItem
            {
                Index = index,
                Pattern = ~pattern,
            });
        }

        public void UndoBan(int index, int pattern)
        {
            // Undo what was done in DoBan

            // First restore compatible for this cell
            // As it is set a negative value in InteralBan
            for (var d = 0; d < directionsCount; d++)
            {
                compatible[index, pattern, d] += patternCount;
            }

            // As we always Undo in reverse order, if item is in toPropagate, it'll
            // be at the top of the stack.
            // If item is in toPropagate, then we haven't got round to processing yet, so there's nothing to undo.
            if (toPropagate.Count > 0)
            {
                var top = toPropagate.Peek();
                if(top.Index == index && top.Pattern == pattern)
                {
                    toPropagate.Pop();
                    return;
                }
            }

            // Not in toPropagate, therefore undo what was done in Propagate
            for (var d = 0; d < directionsCount; d++)
            {
                if (!topology.TryMove(index, (Direction)d, out var i2, out var id, out var el))
                {
                    continue;
                }
                var patterns = propagatorArray[pattern][(int)el];
                foreach (var p in patterns)
                {
                    ++compatible[i2, p, (int)id];
                }
            }
        }

        private void PropagateBanCore(int[] patterns, int i2, int d)
        {
            // Hot loop
            foreach (var p in patterns)
            {
                var c = --compatible[i2, p, d];
                // Have we just now ruled out this possible pattern?
                if (c == 0)
                {
                    if (propagator.InternalBan(i2, p))
                    {
                        propagator.SetContradiction();
                    }
                }
            }
        }

        private void PropagateSelectCore(BitArray patternsDense, int i2, int id)
        {
            for (var p = 0; p < patternCount; p++)
            {
                var patternsContainsP = patternsDense[p];

                // Sets the value of compatible, triggering internal bans
                var prevCompatible = compatible[i2, p, (int)id];
                var currentlyPossible = prevCompatible > 0;
                var newCompatible = (currentlyPossible ? 0 : -patternCount) + (patternsContainsP ? 1 : 0);
                compatible[i2, p, (int)id] = newCompatible;

                // Have we just now ruled out this possible pattern?
                if (newCompatible == 0)
                {
                    if (propagator.InternalBan(i2, p))
                    {
                        propagator.SetContradiction();
                    }
                }
            }
        }

        public void Propagate()
        {
            while (toPropagate.Count > 0)
            {
                var item = toPropagate.Pop();
                int x, y, z;
                topology.GetCoord(item.Index, out x, out y, out z);
                if (item.Pattern >= 0)
                {
                    // Process a ban
                    for (var d = 0; d < directionsCount; d++)
                    {
                        if (!topology.TryMove(x, y, z, (Direction)d, out var i2, out Direction id, out EdgeLabel el))
                        {
                            continue;
                        }
                        var patterns = propagatorArray[item.Pattern][(int)el];
                        PropagateBanCore(patterns, i2, (int)id);
                    }
                }
                else
                {
                    // Process a select.
                    // Selects work similarly to bans, only instead of decrementing the compatible array
                    // we set it to a known value.
                    var pattern = ~item.Pattern;
                    for (var d = 0; d < directionsCount; d++)
                    {
                        if (!topology.TryMove(x, y, z, (Direction)d, out var i2, out Direction id, out EdgeLabel el))
                        {
                            continue;
                        }
                        var patternsDense = propagatorArrayDense[pattern][(int)el];

                        // TODO: Special case for when patterns.Length == 1?

                        PropagateSelectCore(patternsDense, i2, (int)id);


                    }
                }

                // It's important we fully process the item before returning
                // so that we're in a consistent state for backtracking
                // Hence we don't check this during the loops above
                if (propagator.Status == Resolution.Contradiction)
                {
                    return;
                }
            }
        }

    }
}
