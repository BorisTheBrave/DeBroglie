using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Trackers
{
    class WeightedRandomPatternPicker : IPatternPicker
    {
        private readonly Wave wave;

        private readonly double[] frequencies;

        public WeightedRandomPatternPicker(Wave wave, double[] frequencies)
        {
            this.wave = wave;
            this.frequencies = frequencies;
        }

        public void GetDistributionAt(int index, out double[] frequencies, out int[] patterns)
        {
            frequencies = this.frequencies;
            patterns = null;
        }

        public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble)
        {
            return RandomPickerUtils.GetRandomPossiblePattern(wave, randomDouble, index, frequencies);
        }
    }
}
