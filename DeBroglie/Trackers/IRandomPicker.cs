using DeBroglie.Wfc;
using System;
using System.Collections.Generic;

namespace DeBroglie.Trackers
{
    internal interface IRandomPicker
    {
        int GetRandomIndex(Func<double> randomDouble, int[] externalPriority = null);

        int GetRandomPossiblePatternAt(int index, Func<double> randomDouble);

        void GetDistributionAt(int index, out double[] frequencies, out int[] patterns);
    }
}
