using DeBroglie.Wfc;
using System;
using System.Collections.Generic;

namespace DeBroglie.Trackers
{
    /// <summary>
    /// Class implementing the heuristic choice of pattern at a given index
    /// </summary>
    internal interface IPatternPicker
    {
        int GetRandomPossiblePatternAt(int index, Func<double> randomDouble);

        void GetDistributionAt(int index, out double[] frequencies, out int[] patterns);
    }
}
