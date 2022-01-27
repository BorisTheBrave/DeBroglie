using DeBroglie.Wfc;
using System;
using System.Collections.Generic;

namespace DeBroglie.Trackers
{
    internal class EntropyTracker : ITracker, IIndexPicker, IFilteredIndexPicker
    {
        private int patternCount;

        private double[] frequencies;

        // Track some useful per-cell values
        private EntropyValues[] entropyValues;

        // See the definition in EntropyValues
        private double[] plogp;

        private bool[] mask;

        private int indices;

        private Wave wave;

        public void Init(WavePropagator wavePropagator)
        {
            Init(wavePropagator.Wave, wavePropagator.Frequencies, wavePropagator.Topology.Mask);
            wavePropagator.AddTracker(this);
        }

        // For debugging
        public void Init(Wave wave, double[] frequencies, bool[] mask)
        {
            this.frequencies = frequencies;
            this.patternCount = frequencies.Length;
            this.mask = mask;

            this.wave = wave;
            this.indices = wave.Indicies;

            // Initialize plogp
            plogp = new double[patternCount];
            for (int pattern = 0; pattern < patternCount; pattern++)
            {
                var f = frequencies[pattern];
                var v = f > 0 ? f * Math.Log(f) : 0.0;
                plogp[pattern] = v;
            }

            entropyValues = new EntropyValues[indices];

            Reset();
        }

        public void DoBan(int index, int pattern)
        {
            entropyValues[index].Decrement(frequencies[pattern], plogp[pattern]);
        }

        public void Reset()
        {
            // Assumes Reset is called on a truly new Wave.

            EntropyValues initial;
            initial.PlogpSum = 0;
            initial.Sum = 0;
            initial.Entropy = 0;
            for (int pattern = 0; pattern < patternCount; pattern++)
            {
                var f = frequencies[pattern];
                var v = f > 0 ? f * Math.Log(f) : 0.0;
                initial.PlogpSum += v;
                initial.Sum += f;
            }
            initial.RecomputeEntropy();
            for (int index = 0; index < indices; index++)
            {
                entropyValues[index] = initial;
            }
        }

        public void UndoBan(int index, int pattern)
        {
            entropyValues[index].Increment(frequencies[pattern], plogp[pattern]);
        }

        // Finds the cells with minimal entropy (excluding 0, decided cells)
        // and picks one randomly.
        // Returns -1 if every cell is decided.
        public int GetRandomIndex(Func<double> randomDouble)
        {
            int selectedIndex = -1;
            double minEntropy = double.PositiveInfinity;
            int countAtMinEntropy = 0;
            for (int i = 0; i < indices; i++)
            {
                if (mask != null && !mask[i])
                    continue;
                var c = wave.GetPatternCount(i);
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (e < minEntropy)
                {
                    countAtMinEntropy = 1;
                    minEntropy = e;
                }
                else if (e == minEntropy)
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
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (e == minEntropy)
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

        public int GetRandomIndex(Func<double> randomDouble, IEnumerable<int> indices)
        {

            int selectedIndex = -1;
            double minEntropy = double.PositiveInfinity;
            int countAtMinEntropy = 0;
            foreach(var i in indices)
            {
                var c = wave.GetPatternCount(i);
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (e < minEntropy)
                {
                    countAtMinEntropy = 1;
                    minEntropy = e;
                }
                else if (e == minEntropy)
                {
                    countAtMinEntropy++;
                }
            }
            var n = (int)(countAtMinEntropy * randomDouble());

            foreach (var i in indices)
            {
                var c = wave.GetPatternCount(i);
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (e == minEntropy)
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

        /**
          * Struct containing the values needed to compute the entropy of all the cells.
          * This struct is updated every time the cell is changed.
          * p'(pattern) is equal to Frequencies[pattern] if the pattern is still possible, otherwise 0.
          */
        private struct EntropyValues
        {
            public double PlogpSum;     // The sum of p'(pattern) * log(p'(pattern)).
            public double Sum;          // The sum of p'(pattern).
            public double Entropy;      // The entropy of the cell.

            public void RecomputeEntropy()
            {
                Entropy = Math.Log(Sum) - PlogpSum / Sum;
            }

            public void Decrement(double p, double plogp)
            {
                PlogpSum -= plogp;
                Sum -= p;
                RecomputeEntropy();
            }

            public void Increment(double p, double plogp)
            {
                PlogpSum += plogp;
                Sum += p;
                RecomputeEntropy();
            }
        }
    }
}
