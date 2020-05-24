using System;

namespace DeBroglie.Trackers
{
    internal interface IEntropyTracker : ITracker
    {
        int GetRandomMinEntropyIndex(Func<double> randomDouble, Func<int, bool> indexFilter = null);

        int GetRandomPossiblePatternAt(int index, Func<double> randomDouble);
    }
}
