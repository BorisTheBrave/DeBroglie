using DeBroglie.Models;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Trackers
{
    /// <summary>
    /// Tracks recently changed indices.
    /// This is useful for efficiently dealing with a small amount of changes to a large topology.
    /// Note that the list of changed indices can contain duplicates.
    /// The behaviour during the first call to GetChangedIndices() is undefined and subject to change.
    /// </summary>
    public class ChangeTracker : ITracker
    {
        private readonly TileModelMapping tileModelMapping;
        private readonly int indexCount;

        // Using pattern topology
        private List<int> changedIndices;

        // Double buffering
        private List<int> changedIndices2;

        private int generation;

        private int[] lastChangedGeneration;

        internal ChangeTracker(TileModelMapping tileModelMapping)
            :this(tileModelMapping, tileModelMapping.PatternTopology.IndexCount)
        {
        }

        internal ChangeTracker(TileModelMapping tileModelMapping, int indexCount)
        {
            this.tileModelMapping = tileModelMapping;
            this.indexCount = indexCount;
        }

        public int ChangedCount => changedIndices.Count;

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

            if(generation == int.MaxValue)
            {
                throw new Exception($"Change Tracker doesn't support more than {int.MaxValue} executions");
            }

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

        void ITracker.DoBan(int index, int pattern)
        {
            var g = lastChangedGeneration[index];
            if(g != generation)
            {
                lastChangedGeneration[index] = generation;
                changedIndices.Add(index);
            }
        }

        void ITracker.Reset()
        {
            changedIndices = new List<int>();
            changedIndices2 = new List<int>();
            lastChangedGeneration = new int[indexCount];
            generation = 1;
        }

        void ITracker.UndoBan(int index, int pattern)
        {
            var g = lastChangedGeneration[index];
            if (g != generation)
            {
                lastChangedGeneration[index] = generation;
                changedIndices.Add(index);
            }
        }
    }
}
