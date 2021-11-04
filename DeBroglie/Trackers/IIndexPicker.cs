using System;
using System.Collections.Generic;

namespace DeBroglie.Trackers
{
    /// <summary>
    /// Class implementing the heuristic choice of index
    /// </summary>
    internal interface IIndexPicker
    {
        int GetRandomIndex(Func<double> randomDouble);
    }

    internal interface IFilteredIndexPicker
    {
        int GetRandomIndex(Func<double> randomDouble, IEnumerable<int> indices);
    }
}
