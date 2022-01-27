using DeBroglie.Wfc;
using System;

namespace DeBroglie.Trackers
{
    internal class SimpleOrderedPatternPicker : IPatternPicker
    {
        private Wave wave;
        private int patternCount;

        public void Init(WavePropagator wavePropagator)
        {

            this.wave = wavePropagator.Wave;
            this.patternCount = wavePropagator.PatternCount;
        }

        public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble)
        {
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    return pattern;
                }
            }
            return -1;
        }
    }
}
