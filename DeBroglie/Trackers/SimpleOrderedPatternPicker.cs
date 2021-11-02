using DeBroglie.Wfc;
using System;

namespace DeBroglie.Trackers
{
    internal class SimpleOrderedPatternPicker : IPatternPicker
    {
        private readonly Wave wave;
        private readonly int patternCount;

        public SimpleOrderedPatternPicker(Wave wave, int patternCount)
        {
            this.wave = wave;
            this.patternCount = patternCount;
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
