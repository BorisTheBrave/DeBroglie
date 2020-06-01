using System;

namespace DeBroglie.Trackers
{
    internal interface IRandomPicker
    {
        int GetRandomIndex(Func<double> randomDouble, int[] externalPriority = null);

        int GetRandomPossiblePatternAt(int index, Func<double> randomDouble);
    }
}
