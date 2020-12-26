using DeBroglie.Wfc;
using System;
using System.Text;

namespace DeBroglie.Trackers
{
    internal class OrderedRandomPicker : IRandomPicker
    {

        private readonly int patternCount;

        private readonly double[] frequencies;

        private readonly bool[] mask;

        private readonly int indices;

        private readonly Wave wave;

        public OrderedRandomPicker(
            Wave wave,
            double[] frequencies,
            bool[] mask)
        {
            this.frequencies = frequencies;
            this.patternCount = frequencies.Length;
            this.mask = mask;

            this.wave = wave;
            this.indices = wave.Indicies;
        }

        public int GetRandomIndex(Func<double> randomDouble, int[] externalPriority = null)
        {
            if(externalPriority != null)
            {
                throw new NotSupportedException();
            }
            for (int i = 0; i < indices; i++)
            {
                if (mask != null && !mask[i])
                    continue;
                var c = wave.GetPatternCount(i);
                if (c <= 1)
                {
                    continue;
                }
                return i;
            }
            return -1;
        }

        public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble)
        {
            var s = 0.0;
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    s += frequencies[pattern];
                }
            }
            var r = randomDouble() * s;
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    r -= frequencies[pattern];
                }
                if (r <= 0)
                {
                    return pattern;
                }
            }
            return patternCount - 1;
        }

    }

    internal class EntropyTracker : ITracker, IRandomPicker
    {
        private readonly int patternCount;

        private readonly double[] frequencies;

        // Track some useful per-cell values
        private readonly EntropyValues[] entropyValues;

        // See the definition in EntropyValues
        private readonly double[] plogp;

        private readonly bool[] mask;

        private readonly int indices;

        private readonly Wave wave;

        public EntropyTracker(
            Wave wave,
            double[] frequencies,
            bool[] mask)
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
        public int GetRandomIndex(Func<double> randomDouble, int[] externalPriority = null)
        {
            int selectedIndex = -1;
            // TODO: At the moment this is a linear scan, but potentially
            // could use some data structure
            int minExternalPriority = int.MinValue;
            double minEntropy = double.PositiveInfinity;
            int countAtMinEntropy = 0;
            for (int i = 0; i < indices; i++)
            {
                if (mask != null && !mask[i])
                    continue;
                var c = wave.GetPatternCount(i);
                var ep = externalPriority == null ? 0 : externalPriority[i];
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (ep > minExternalPriority || (ep == minExternalPriority && e < minEntropy))
                {
                    countAtMinEntropy = 1;
                    minExternalPriority = ep;
                    minEntropy = e;
                }
                else if (ep == minExternalPriority && e == minEntropy)
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
                var ep = externalPriority == null ? 0 : externalPriority[i];
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (ep == minExternalPriority && e == minEntropy)
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
            var s = 0.0;
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    s += frequencies[pattern];
                }
            }
            var r = randomDouble() * s;
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    r -= frequencies[pattern];
                }
                if (r <= 0)
                {
                    return pattern;
                }
            }
            return patternCount - 1;
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
