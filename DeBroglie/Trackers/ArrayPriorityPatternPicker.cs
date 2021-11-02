using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Trackers
{
    class ArrayPriorityPatternPicker : IPatternPicker
    {
        private readonly Wave wave;

        private readonly FrequencySet[] frequencySets;

        public ArrayPriorityPatternPicker(Wave wave, FrequencySet[] frequencySets)
        {
            this.wave = wave;
            this.frequencySets = frequencySets;
        }

        public void GetDistributionAt(int index, out double[] frequencies, out int[] patterns)
        {
            throw new Exception();
        }

        public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble)
        {
            var fs = frequencySets[index];

            // Run through the groups with descending prioirty
            for(var g=0;g<fs.groups.Length;g++)
            {
                var patterns = fs.groups[g].patterns;
                var frequencies = fs.groups[g].patterns;
                // Scan the group
                var s = 0.0;
                for(var i=0;i<patterns.Length;i++)
                {
                    if (wave.Get(index, patterns[i]))
                        s += frequencies[i];
                }
                if (s <= 0.0)
                    continue;

                // There's at least one choice at this priority level,
                // pick one at random.
                var r = randomDouble() * s;
                for (var i = 0; i < patterns.Length; i++)
                {
                    if (wave.Get(index, patterns[i]))
                    {
                        r -= frequencies[i];
                    }
                    if (r <= 0)
                    {
                        return patterns[i];
                    }
                }
                return patterns[patterns.Length - 1];
            }

            return -1;
        }
    }
}
