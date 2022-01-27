using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Trackers
{
    class WeightedRandomPatternPicker : IPatternPicker
    {
        private Wave wave;

        private double[] frequencies;

        public WeightedRandomPatternPicker()
        {
        }

        public void Init(WavePropagator wavePropagator)
        {
            wave = wavePropagator.Wave;
            frequencies = wavePropagator.Frequencies;
        }

        public int GetRandomPossiblePatternAt(int index, Func<double> randomDouble)
        {
            return RandomPickerUtils.GetRandomPossiblePattern(wave, randomDouble, index, frequencies);
        }
    }
}
