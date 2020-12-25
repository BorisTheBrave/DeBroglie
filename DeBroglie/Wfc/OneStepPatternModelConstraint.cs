using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeBroglie.Topo;

namespace DeBroglie.Wfc
{
    /// <summary>
    /// When a pattern is selected, update the possible patterns in immediately adjacent cells
    /// but don't recurse beyond that.
    /// This constraint is much faster than <see cref="Ac4PatternModelConstraint"/>, but doesn't
    /// enforce full arc consistency, so leads to contradictions more frequently.
    /// </summary>
    internal class OneStepPatternModelConstraint : IPatternModelConstraint
    {
        private int[][][] propagatorArray;
        private WavePropagator propagator;
        private ITopology topology;
        private Wave wave;
        private int directionsCount;
        private int patternCount;
        private BitArray[][] propagatorArrayDense;
        private Stack<IndexPatternItem> toPropagate;


        public OneStepPatternModelConstraint(WavePropagator propagator, PatternModel model)
        {
            this.propagatorArray = model.Propagator;
            this.propagator = propagator;
            this.topology = propagator.Topology;
            this.directionsCount = propagator.Topology.DirectionsCount;
            this.patternCount = model.PatternCount;
            this.propagatorArrayDense = model.Propagator.Select(a1 => a1.Select(x =>
            {
                var dense = new BitArray(patternCount);
                foreach (var p in x) dense[p] = true;
                return dense;
            }).ToArray()).ToArray();
            toPropagate = new Stack<IndexPatternItem>();
        }


        public void Clear()
        {
            toPropagate.Clear();
            this.wave = propagator.Wave;
        }

        public void DoBan(int index, int pattern)
        {
            // Assumes DoBan is called before wave.RemovePossibility
            if(wave.GetPatternCount(index) == 2)
            {
                for(var p=0;p<patternCount;p++)
                {
                    if (p == pattern)
                        continue;
                    if (wave.Get(index, p))
                    {
                        DoSelect(index, p);
                        return;
                    }
                }
            }
        }

        public void UndoBan(int index, int pattern)
        {
            // As we always Undo in reverse order, if item is in toPropagate, it'll
            // be at the top of the stack.
            // If item is in toPropagate, then we haven't got round to processing yet, so there's nothing to undo.
            if (toPropagate.Count > 0)
            {
                var top = toPropagate.Peek();
                if (top.Index == index && top.Pattern == pattern)
                {
                    toPropagate.Pop();
                    return;
                }
            }
        }

        public void DoSelect(int index, int pattern)
        {
            toPropagate.Push(new IndexPatternItem { Index = index, Pattern = pattern });
        }

        public void Propagate()
        {
            while (toPropagate.Count > 0)
            {
                var item = toPropagate.Pop();
                var index = item.Index;
                var pattern = item.Pattern;


                int x, y, z;
                topology.GetCoord(index, out x, out y, out z);
                for (var d = 0; d < directionsCount; d++)
                {
                    if (!topology.TryMove(x, y, z, (Direction)d, out var i2, out Direction id, out EdgeLabel el))
                    {
                        continue;
                    }
                    var patternsDense = propagatorArrayDense[pattern][(int)el];

                    for (var p = 0; p < patternCount; p++)
                    {
                        if (patternsDense[p])
                            continue;

                        if (wave.Get(i2, p))
                        {

                            if (propagator.InternalBan(i2, p))
                            {
                                propagator.SetContradiction();
                            }
                        }
                    }
                }
            }
        }
    }
}
