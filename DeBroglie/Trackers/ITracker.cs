using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Trackers
{
    /// <summary>
    /// Trackers are objects that maintain state that is a summary of the current state of the propagator.
    /// By updating that state as the propagator changes, they can give a significant performance benefit
    /// over calculating the value from scratch each time it is needed.
    /// </summary>
    internal interface ITracker
    {
        void Reset();

        void DoBan(int index, int pattern);

        void UndoBan(int index, int pattern);
    }

    /// <summary>
    /// Callback for when choices/backtracks occur on WavePropagator
    /// </summary>
    // TODO: Move this class eleswhere?
    internal interface IChoiceObserver
    {
        // Called before the wave propagator is updated for the choice
        void MakeChoice(int index, int pattern);

        // Called after the wave propagator is backtracked
        void Backtrack();
    }
}
