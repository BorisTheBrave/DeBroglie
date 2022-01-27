using DeBroglie.Wfc;
using System;

namespace DeBroglie.Trackers
{

    internal class ArrayPriorityPatternPicker : IPatternPicker
    {
        private Wave wave;

        private readonly WeightSetCollection weightSetCollection;

        public ArrayPriorityPatternPicker(WeightSetCollection weightSetCollection)
        {
            this.weightSetCollection = weightSetCollection;
        }

        public void Init(WavePropagator wavePropagator)
        {
            wave = wavePropagator.Wave;
        }

        public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble)
        {
            var fs = weightSetCollection.Get(index);

            // Run through the groups with descending prioirty
            for (var g = 0; g < fs.groups.Length; g++)
            {
                var patterns = fs.groups[g].patterns;
                var frequencies = fs.groups[g].frequencies;
                // Scan the group
                var s = 0.0;
                for (var i = 0; i < patterns.Length; i++)
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
