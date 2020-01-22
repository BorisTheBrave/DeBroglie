using DeBroglie.Models;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Trackers
{
    internal class ChangeTracker : ITracker
    {
        private readonly TileModelMapping tileModelMapping;

        // Using pattern topology
        private List<int> changedIndices;

        // Double buffering
        private List<int> changedIndices2;

        private int generation;

        private int[] lastChangedGeneration;

        internal ChangeTracker(TileModelMapping tileModelMapping)
        {
            this.tileModelMapping = tileModelMapping;
        }

        /// <summary>
        /// Returns the set of indices that have been changed since the last call.
        /// </summary>
        public IEnumerable<int> GetChangedIndices()
        {
            var currentChangedIndices = changedIndices;

            // Switch over double buffering
            (changedIndices, changedIndices2) = (changedIndices2, changedIndices);
            changedIndices.Clear();
            generation++;

            if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null)
            {
                return currentChangedIndices;
            }
            else
            {
                return currentChangedIndices.SelectMany(i =>
                    tileModelMapping.PatternCoordToTileCoordIndexAndOffset.Get(i).Select(x => x.Item2));
            }
        }

        public void DoBan(int index, int pattern)
        {
            var g = lastChangedGeneration[index];
            if(g != generation)
            {
                lastChangedGeneration[index] = generation;
                changedIndices.Add(index);
            }
        }

        public void Reset()
        {
            changedIndices = new List<int>();
            changedIndices2 = new List<int>();
            lastChangedGeneration = new int[tileModelMapping.PatternTopology.IndexCount];
            generation = 1;
        }

        public void UndoBan(int index, int pattern)
        {
            DoBan(index, pattern);
        }
    }
}
