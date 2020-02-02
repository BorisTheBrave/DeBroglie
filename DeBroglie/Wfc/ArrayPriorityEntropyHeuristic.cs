using DeBroglie.Trackers;
using System;

namespace DeBroglie.Wfc
{
    /// <summary>
    /// Chooses the next tile based of minimum "entropy", i.e. 
    /// the tiles which are already most constrained.
    /// </summary>
    internal class ArrayPriorityEntropyHeuristic : IPickHeuristic
    {
        private ArrayPriorityEntropyTracker entropyTracker;

        private Func<double> randomDouble;

        public ArrayPriorityEntropyHeuristic(ArrayPriorityEntropyTracker entropyTracker, Func<double> randomDouble)
        {
            this.entropyTracker = entropyTracker;
            this.randomDouble = randomDouble;
        }

        public void PickObservation(out int index, out int pattern)
        {
            // Choose a random cell
            index = entropyTracker.GetRandomMinEntropyIndex(randomDouble);
            if (index == -1)
            {
                pattern = -1;
                return;
            }
            // Choose a random pattern
            pattern = entropyTracker.GetRandomPossiblePatternAt(index, randomDouble);
        }
    }
}
