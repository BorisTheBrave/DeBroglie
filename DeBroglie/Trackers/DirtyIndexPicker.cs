using DeBroglie.Topo;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Trackers
{
    class DirtyIndexPicker : IIndexPicker, ITracker
    {
        private readonly IFilteredIndexPicker filteredIndexPicker;
        private readonly HashSet<int> dirtyIndices;
        private readonly ITopoArray<int> cleanPatterns;

        public DirtyIndexPicker(IFilteredIndexPicker filteredIndexPicker, ITopoArray<int> cleanPatterns)
        {
            dirtyIndices = new HashSet<int>();
            this.filteredIndexPicker = filteredIndexPicker;
            this.cleanPatterns = cleanPatterns;
        }

        public void Init(WavePropagator wavePropagator)
        {
            filteredIndexPicker.Init(wavePropagator);
            wavePropagator.AddTracker(this);
        }

        public void DoBan(int index, int pattern)
        {
            var clean = cleanPatterns.Get(index);
            if (clean == pattern)
                dirtyIndices.Add(index);
        }

        public int GetRandomIndex(Func<double> randomDouble)
        {
            return filteredIndexPicker.GetRandomIndex(randomDouble, dirtyIndices);
        }

        public void Reset()
        {
            dirtyIndices.Clear();
        }

        public void UndoBan(int index, int pattern)
        {
            // Doesn't undo dirty, it's too annoying to track
        }
    }
}
