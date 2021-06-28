using DeBroglie.Wfc;
using System;

namespace DeBroglie.Trackers
{
    internal static class RandomPickerUtils
    {
        public static int GetRandomPossiblePattern(Wave wave, Func<double> randomDouble, int index, double[] frequencies)
        {
            var patternCount = frequencies.Length;
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

        public static int GetRandomPossiblePattern(Wave wave, Func<double> randomDouble, int index, double[] frequencies, int[] patterns)
        {
            if(patterns == null)
            {
                return GetRandomPossiblePattern(wave, randomDouble, index, frequencies);
            }

            var s = 0.0;
            var patternCount = frequencies.Length;
            for (var i = 0; i < patternCount; i++)
            {
                var pattern = patterns[i];
                if (wave.Get(index, pattern))
                {
                    s += frequencies[i];
                }
            }
            var r = randomDouble() * s;
            for (var i = 0; i < patternCount; i++)
            {
                var pattern = patterns[i];
                if (wave.Get(index, pattern))
                {
                    r -= frequencies[i];
                }
                if (r <= 0)
                {
                    return pattern;
                }
            }
            return patterns[patterns.Length - 1];
        }
    }
}
