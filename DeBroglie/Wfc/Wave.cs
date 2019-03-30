using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeBroglie.Wfc
{

    /**
     * Wave is a fancy array that tracks various per-cell information.
     * Most importantly, it tracks possibilities - which patterns are possible to put
     * into which cells.
     * It has no notion of cell adjacency, cells are just referred to by integer index.
     */
    internal class Wave
    {
        public static int AllCellsDecided = -1;

        private readonly int patternCount;
        private readonly double[] frequencies;

        // possibilities[index*patternCount + pattern] is true if we haven't eliminated putting
        // that pattern at that index.
        private readonly BitArray possibilities;

        // Track some useful per-cell values
        private readonly EntropyValues[] entropyValues;

        // See the definition in EntropyValues
        private readonly double[] plogp;

        private readonly bool[] mask;

        private readonly int indices;


        private Wave(int patternCount, 
            double[] frequencies,
            BitArray possibilites,
            EntropyValues[] entropyValues,
            double[] plogp,
            int indices,
            bool[] mask)
        {
            this.patternCount = patternCount;
            this.frequencies = frequencies;
            this.possibilities = possibilites;
            this.entropyValues = entropyValues;
            this.plogp = plogp;
            this.indices = indices;
        }


        public Wave(double[] frequencies, int indices, bool[] mask)
        {
            this.patternCount = frequencies.Length;
            this.frequencies = frequencies;

            this.indices = indices;
            this.mask = mask;

            // Initialize possibilities
            possibilities = new BitArray(indices * patternCount, true);

            // Initialize plogp and entropyValues
            plogp = new double[patternCount];
            EntropyValues initial;
            initial.PlogpSum = 0;
            initial.Sum = 0;
            initial.PatternCount = 0;
            initial.Entropy = 0;
            for (int pattern = 0; pattern < patternCount; pattern++)
            {
                var f = frequencies[pattern];
                var v = f > 0 ? f * Math.Log(f) : 0.0;
                plogp[pattern] = v;
                initial.PlogpSum += v;
                initial.Sum += f;
                initial.PatternCount += 1;
            }
            initial.RecomputeEntropy();
            entropyValues = new EntropyValues[indices];
            for (int index = 0; index < indices; index++)
            {
                entropyValues[index] = initial;
            }
        }

        public Wave Clone()
        {
            return new Wave(
                patternCount,
                frequencies,
                (BitArray)possibilities.Clone(),
                (EntropyValues[])entropyValues.Clone(),
                plogp,
                indices,
                mask);
        }

        public bool Get(int index, int pattern)
        {
            return possibilities[index * patternCount + pattern];
        }

        // Returns true if there is a contradiction
        public bool RemovePossibility(int index, int pattern)
        {
            Debug.Assert(possibilities[index * patternCount + pattern] == true);
            possibilities[index * patternCount + pattern] = false;
            int c = entropyValues[index].Decrement(frequencies[pattern], plogp[pattern]);
            return c == 0;
        }

        public void AddPossibility(int index, int pattern)
        {
            Debug.Assert(possibilities[index * patternCount + pattern] == false);
            possibilities[index * patternCount + pattern] = true;
            entropyValues[index].Increment(frequencies[pattern], plogp[pattern]);
        }

        // Finds the cells with minimal entropy (excluding 0, decided cells)
        // and picks one randomly.
        // Returns AllCellsDecided if every cell is decided.
        public int GetRandomMinEntropyIndex(Random r)
        {
            int selectedIndex = AllCellsDecided;
            double minEntropy = double.PositiveInfinity;
            double randomizer = 0;
            for (int i = 0; i < indices; i++)
            {
                if (mask != null && !mask[i])
                    continue;
                var c = entropyValues[i].PatternCount;
                var e = entropyValues[i].Entropy;
                if (c <= 1)
                {
                    continue;
                }
                else if (e < minEntropy)
                {
                    selectedIndex = i;
                    minEntropy = e;
                    randomizer = r.NextDouble();
                }
                else if (e == minEntropy)
                {
                    var randomizer2 = r.NextDouble();
                    if (randomizer2 < randomizer)
                    {
                        selectedIndex = i;
                        minEntropy = e;
                        randomizer = randomizer2;
                    }
                }
            }
            return selectedIndex;
        }

        public double GetProgress()
        {
            var c = 0;
            foreach(bool b in possibilities)
            {
                if (!b) c += 1;
            }
            // We're basically done when we've banned all but one pattern for each index
            return ((double)c) / (patternCount-1) / indices;
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
            public int PatternCount;    // The number of patterns present in the wave in the cell.
            public double Entropy;      // The entropy of the cell.

            public void RecomputeEntropy()
            {
                Entropy = Math.Log(Sum) - PlogpSum / Sum;
            }

            public int Decrement(double p, double plogp)
            {
                PlogpSum -= plogp;
                Sum -= p;
                PatternCount--;
                RecomputeEntropy();
                return PatternCount;
            }

            public void Increment(double p, double plogp)
            {
                PlogpSum += plogp;
                Sum += p;
                PatternCount++;
                RecomputeEntropy();
            }
        }
    }
}
