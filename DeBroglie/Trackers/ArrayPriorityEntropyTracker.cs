using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Trackers
{
    /// <summary>
    /// An <see cref="IRandomPicker"/> that picks cells based on min entropy heuristic.
    /// It's slower than <see cref="EntropyTracker"/> but supports two extra features:
    /// * The frequencies can be set on a per cell basis.
    /// * In addition to frequency, priority can be set. Only tiles of the highest priority for a given cell are considered available.
    /// </summary>
    internal class ArrayPriorityEntropyTracker : ITracker, IIndexPicker, IPatternPicker
    {
        private readonly WeightSetCollection weightSetCollection;

        // Track some useful per-cell values
        private EntropyValues[] entropyValues;

        private bool[] mask;

        private int indices;

        private Wave wave;

        public ArrayPriorityEntropyTracker(WeightSetCollection weightSetCollection)
        {
            this.weightSetCollection = weightSetCollection;

        }

        public void Init(WavePropagator wavePropagator)
        {
            mask = wavePropagator.Topology.Mask;
            wave = wavePropagator.Wave;
            indices = wave.Indicies;
            entropyValues = new EntropyValues[indices];

            Reset();
            wavePropagator.AddTracker(this);
        }

        // Don't run init twice
        void IPatternPicker.Init(WavePropagator wavePropagator)
        {

        }


        public void DoBan(int index, int pattern)
        {
            var frequencySet = weightSetCollection.Get(index);
            if (entropyValues[index].Decrement(frequencySet.priorityIndices[pattern], frequencySet.frequencies[pattern], frequencySet.plogp[pattern]))
            {
                PriorityReset(index);
            }
        }

        public void Reset()
        {
            // TODO: Perf boost by assuming wave is truly fresh?
            EntropyValues initial;
            initial.PriorityIndex = 0;
            initial.PlogpSum = 0;
            initial.Sum = 0;
            initial.Count = 0;
            initial.Entropy = 0;
            for (int index = 0; index < indices; index++)
            {
                entropyValues[index] = initial;
                if (weightSetCollection.Get(index) != null)
                {
                    PriorityReset(index);
                }
            }
        }

        // The priority has just changed, recompute
        private void PriorityReset(int index)
        {
            var frequencySet = weightSetCollection.Get(index);
            ref var v = ref entropyValues[index];
            v.PlogpSum = 0;
            v.Sum = 0;
            v.Count = 0;
            v.Entropy = 0;
            while (v.PriorityIndex < frequencySet.groups.Length)
            {
                ref var g = ref frequencySet.groups[v.PriorityIndex];
                for (var i = 0; i < g.patternCount; i++)
                {
                    if (wave.Get(index, g.patterns[i]))
                    {
                        v.Sum += g.frequencies[i];
                        v.PlogpSum += g.plogp[i];
                        v.Count += 1;
                    }
                }
                if(v.Count == 0)
                {
                    // Try again with the next priorityIndex
                    v.PriorityIndex++;
                    continue;
                }
                v.RecomputeEntropy();
                return;
            }
        }

        public void UndoBan(int index, int pattern)
        {
            var frequencySet = weightSetCollection.Get(index);
            if (entropyValues[index].Increment(frequencySet.priorityIndices[pattern], frequencySet.frequencies[pattern], frequencySet.plogp[pattern]))
            {
                PriorityReset(index);
            }
        }

        // Finds the cells with minimal entropy (excluding 0, decided cells)
        // and picks one randomly.
        // Returns -1 if every cell is decided.
        public int GetRandomIndex(Func<double> randomDouble)
        {
            int selectedIndex = -1;
            // TODO: At the moment this is a linear scan, but potentially
            // could use some data structure
            int minPriorityIndex = int.MaxValue;
            double minEntropy = double.PositiveInfinity;
            int countAtMinEntropy = 0;
            for (int i = 0; i < indices; i++)
            {
                if (mask != null && !mask[i])
                    continue;
                var c = wave.GetPatternCount(i);
                var pi = entropyValues[i].PriorityIndex;
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (pi < minPriorityIndex || (pi == minPriorityIndex && e < minEntropy))
                {
                    countAtMinEntropy = 1;
                    minEntropy = e;
                    minPriorityIndex = pi;
                }
                else if (pi == minPriorityIndex && e == minEntropy)
                {
                    countAtMinEntropy++;
                }
            }
            var n = (int)(countAtMinEntropy * randomDouble());

            for (int i = 0; i < indices; i++)
            {
                if (mask != null && !mask[i])
                    continue;
                var c = wave.GetPatternCount(i);
                var pi = entropyValues[i].PriorityIndex;
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (pi == minPriorityIndex && e == minEntropy)
                {
                    if (n == 0)
                    {
                        selectedIndex = i;
                        break;
                    }
                    n--;
                }
            }
            return selectedIndex;
        }

        public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble)
        {
            var frequencySet = weightSetCollection.Get(index);
            ref var g = ref frequencySet.groups[entropyValues[index].PriorityIndex];
            return RandomPickerUtils.GetRandomPossiblePattern(wave, randomDouble, index, g.frequencies, g.patterns);
        }

        /**
          * Struct containing the values needed to compute the entropy of all the cells.
          * This struct is updated every time the cell is changed.
          * p'(pattern) is equal to Frequencies[pattern] if the pattern is still possible, otherwise 0.
          */
        private struct EntropyValues
        {
            public int PriorityIndex;
            public double PlogpSum;     // The sum of p'(pattern) * log(p'(pattern)).
            public double Sum;          // The sum of p'(pattern).
            public int Count;
            public double Entropy;      // The entropy of the cell.

            public void RecomputeEntropy()
            {
                Entropy = Math.Log(Sum) - PlogpSum / Sum;
            }

            public bool Decrement(int priorityIndex, double p, double plogp)
            {
                if (priorityIndex == PriorityIndex)
                {
                    PlogpSum -= plogp;
                    Sum -= p;
                    Count--;
                    if (Count == 0)
                    {
                        PriorityIndex++;
                        return true;
                    }
                    RecomputeEntropy();
                }
                return false;
            }

            public bool Increment(int priorityIndex, double p, double plogp)
            {
                if (priorityIndex == PriorityIndex)
                {
                    PlogpSum += plogp;
                    Sum += p;
                    Count++;
                    RecomputeEntropy();
                }
                if (priorityIndex < PriorityIndex)
                {
                    PriorityIndex = priorityIndex;
                    return true;
                }
                return false;
            }
        }
    }
}
