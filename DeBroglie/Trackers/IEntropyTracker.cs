using System;

namespace DeBroglie.Trackers
{
    internal interface IEntropyTracker : ITracker
    {
        int GetRandomMinEntropyIndex(Func<double> randomDouble, int[] externalPriority = null);

        int GetRandomPossiblePatternAt(int index, Func<double> randomDouble);
    }
}
