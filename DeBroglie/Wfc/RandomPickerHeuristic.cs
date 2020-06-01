using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Wfc
{

    /// <summary>
    /// Chooses the next tile based of minimum "entropy", i.e. 
    /// the tiles which are already most constrained.
    /// </summary>
    internal class RandomPickerHeuristic : IPickHeuristic
    {
        private IRandomPicker randomPicker;

        private Func<double> randomDouble;

        public RandomPickerHeuristic(IRandomPicker randomPicker, Func<double> randomDouble)
        {
            this.randomPicker = randomPicker;
            this.randomDouble = randomDouble;
        }

        public void PickObservation(out int index, out int pattern)
        {
            // Choose a random cell
            index = randomPicker.GetRandomIndex(randomDouble);
            if (index == -1)
            {
                pattern = -1;
                return;
            }
            // Choose a random pattern
            pattern = randomPicker.GetRandomPossiblePatternAt(index, randomDouble);
        }
    }
}
