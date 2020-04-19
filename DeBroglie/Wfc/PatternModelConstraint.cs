using System.Collections.Generic;
using DeBroglie.Topo;

namespace DeBroglie.Wfc
{
    /// <summary>
    /// This works similarly to IWaveConstraint, in that it listens to changes in the Wave, and  
    /// makes appropriate changes to the propagator for the constraint.
    /// The constraint being enforced that adjacent patterns must filt PatternModel.Propatagor.
    /// 
    /// It's not implemented as a IWaveConstraint for historical reasons
    /// </summary>
    internal class PatternModelConstraint
    {
        // From model
        private int[][][] propagatorArray;
        private int patternCount;

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

        public PatternModelConstraint(WavePropagator propagator, PatternModel model)
        {
            this.toPropagate = new Stack<IndexPatternItem>();
            this.propagator = propagator;

            this.propagatorArray = model.Propagator;
            this.patternCount = model.PatternCount;

            this.topology = propagator.Topology;
            this.indexCount = topology.IndexCount;
            this.directionsCount = topology.DirectionsCount;
        }

        public void Clear()
        {
            toPropagate.Clear();

            compatible = new int[indexCount, patternCount, directionsCount];
            for (int index = 0; index < indexCount; index++)
            {
                if (!topology.ContainsIndex(index))
                    continue;

                for (int pattern = 0; pattern < patternCount; pattern++)
                {
                    for (int d = 0; d < directionsCount; d++)
                    {
                        if (topology.TryMove(index, (Direction)d, out var dest, out var _, out var el))
                        {
                            var compatiblePatterns = propagatorArray[pattern][(int)el].Length;
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

        public void UndoBan(IndexPatternItem item)
        {
            // Undo what was done in DoBan

            // First restore compatible for this cell
            // As it is set to zero in InteralBan
            for (var d = 0; d < directionsCount; d++)
            {
                compatible[item.Index, item.Pattern, d] += patternCount;
            }

            // As we always Undo in reverse order, if item is in toPropagate, it'll
            // be at the top of the stack.
            // If item is in toPropagate, then we haven't got round to processing yet, so there's nothing to undo.
            if (toPropagate.Count > 0)
            {
                var top = toPropagate.Peek();
                if(top.Equals(item))
                {
                    toPropagate.Pop();
                    return;
                }
            }

            // Not in toPropagate, therefore undo what was done in Propagate
            for (var d = 0; d < directionsCount; d++)
            {
                if (!topology.TryMove(item.Index, (Direction)d, out var i2, out var id, out var el))
                {
                    continue;
                }
                var patterns = propagatorArray[item.Pattern][(int)el];
                foreach (var p in patterns)
                {
                    ++compatible[i2, p, (int)id];
                }
            }
        }

        private void PropagateCore(int[] patterns, int i2, int d)
        {
            // Hot loop
            foreach (var p in patterns)
            {
                var c = --compatible[i2, p, d];
                // We've just now ruled out this possible pattern
                if (c == 0)
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
                for (var d = 0; d < directionsCount; d++)
                {
                    if (!topology.TryMove(x, y, z, (Direction)d, out var i2, out Direction id, out EdgeLabel el))
                    {
                        continue;
                    }
                    var patterns = propagatorArray[item.Pattern][(int)el];
                    PropagateCore(patterns, i2, (int)id);
                }
                // It's important we fully process the item before returning
                // so that we're in a consistent state for backtracking
                if (propagator.Status == Resolution.Contradiction)
                {
                    return;
                }
            }
            return;
        }

    }
}
