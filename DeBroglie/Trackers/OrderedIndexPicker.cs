using DeBroglie.Wfc;
using System;
using System.Collections.Generic;

namespace DeBroglie.Trackers
{
    internal class OrderedIndexPicker : IIndexPicker, IFilteredIndexPicker
    {
        private readonly int[] indexOrder;

        private Wave wave;

        public OrderedIndexPicker(int[] indexOrder)
        {
            this.indexOrder = indexOrder;
        }

        public void Init(WavePropagator wavePropagator)
        {
            this.wave = wavePropagator.Wave;
        }


        public int GetRandomIndex(Func<double> randomDouble)
        {
            foreach(var i in indexOrder)
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

        public int GetRandomIndex(Func<double> randomDouble, IEnumerable<int> indices)
        {
            var set = new HashSet<int>(indices);
            foreach(var i in indices)
            {
                if (!set.Contains(i))
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
    }
}
