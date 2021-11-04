using DeBroglie.Wfc;
using System;
using System.Collections.Generic;

namespace DeBroglie.Trackers
{
    internal class SimpleOrderedIndexPicker : IIndexPicker, IFilteredIndexPicker
    {
        private readonly int patternCount;

        private readonly double[] frequencies;

        private readonly bool[] mask;

        private readonly int indices;

        private readonly Wave wave;

        public SimpleOrderedIndexPicker(
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

        public int GetRandomIndex(Func<double> randomDouble)
        {
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

        public int GetRandomIndex(Func<double> randomDouble, IEnumerable<int> indices)
        {
            foreach(var i in indices)
            {
                var c = wave.GetPatternCount(i);
                if (c <= 1)
                {
                    continue;
                }
                return i;
            }
            return -1;
        }
    }
}
