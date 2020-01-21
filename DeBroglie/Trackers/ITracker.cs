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
}
