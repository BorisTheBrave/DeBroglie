using DeBroglie.Wfc;
using System;
using System.Collections.Generic;

namespace DeBroglie.Trackers
{
    internal class OrderedIndexPicker : IIndexPicker, IFilteredIndexPicker
    {
        private readonly int[] indexOrder;

        private readonly Wave wave;

        public OrderedIndexPicker(
            Wave wave,
            int[] indexOrder)
        {
            this.wave = wave;
            this.indexOrder = indexOrder;
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
