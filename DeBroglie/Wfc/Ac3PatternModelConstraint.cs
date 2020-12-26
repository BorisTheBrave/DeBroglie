using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DeBroglie.Topo;

namespace DeBroglie.Wfc
{
    internal class Ac3PatternModelConstraint : IPatternModelConstraint
    {
        // From model
        private int[][][] propagatorArray;
        private int patternCount;

        // Re-organized propagatorArray
        private BitArray[][] propagatorArrayDense;

        // Useful values
        private readonly WavePropagator propagator;
        private readonly int directionsCount;
        private readonly int[][][] incoming;
        private readonly ITopology topology;
        private int indexCount;

        // List of locations that still need to be checked against for fulfilling the model's conditions
        private HashSet<(int, Direction)> toPropagate;

        public Ac3PatternModelConstraint(WavePropagator propagator, PatternModel model)
        {
            this.toPropagate = new HashSet<(int, Direction)>();
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

            var flatPropagator = Enumerable.Range(0, propagator.PatternCount)
                .SelectMany(p => Enumerable.Range(0, propagatorArray[p].Length)
                    .SelectMany(el => propagatorArray[p][el].Select(p2 => (p, el, p2))));

            var elCount = propagatorArray.Select(x => x.Length).Max() + 1;

            this.incoming = DenseRegroup(flatPropagator, patternCount, x => x.p2, g => DenseRegroup(g, elCount, x => x.el, g2 => g2.Select(x => x.p).ToArray()));
        }

        private static V[] DenseRegroup<T, V>(IEnumerable<T> items, int count, Func<T, int> keyFunc, Func<List<T>, V> valueFunc)
        {
            var vList = Enumerable.Range(0, count).Select(_ => new List<T>()).ToArray();
            foreach(var item in items)
            {
                var k = keyFunc(item);
                vList[k].Add(item);
            }
            return vList.Select(valueFunc).ToArray();
        }

        public void DoBan(int index, int pattern)
        {
            for (var d = 0; d < directionsCount; d++)
            {
                toPropagate.Add((index, (Direction)d));
            }
        }

        public void UndoBan(int index, int pattern)
        {
        }

        public void DoSelect(int index, int pattern)
        {
            // We just record which cells are dirty, so
            // there's no difference between a ban and a select.
            DoBan(index, pattern);
        }

        public void Propagate()
        {
            var wave = propagator.Wave;
            while(toPropagate.Count > 0)
            {
                var item = toPropagate.First();
                toPropagate.Remove(item);
                var (index, d) = item;
                topology.GetCoord(index, out var x, out var y, out var z);
                if (!topology.TryMove(x, y, z, d, out var i2, out Direction id, out EdgeLabel el))
                {
                    continue;
                }
                for (var pattern = 0; pattern < patternCount; pattern++)
                {
                    if (!wave.Get(i2, pattern))
                        continue;

                    var incomingPatterns = incoming[pattern][(int)el];
                    var found = false;
                    foreach (var p in incomingPatterns)
                    {
                        if (wave.Get(index, p))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        if(propagator.InternalBan(i2, pattern))
                        {
                            propagator.SetContradiction();
                        }
                    }
                }
            }
        }

        public void Clear()
        {
        }
    }
}
