using DeBroglie.Wfc;
using System;
using System.Collections.Generic;

namespace DeBroglie.Trackers
{
    internal class SimpleOrderedIndexPicker : IIndexPicker, IFilteredIndexPicker
    {
        private bool[] mask;

        private int indices;

        private Wave wave;

        public SimpleOrderedIndexPicker()
        {
        }

        public void Init(WavePropagator wavePropagator)
        {
            wave = wavePropagator.Wave;

            this.mask = wavePropagator.Topology.Mask;

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
