using DeBroglie.Models;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Trackers
{
    internal class WeightSetCollection
    {
        private readonly ITopoArray<int> weightSetByIndex;
        private readonly IDictionary<int, IDictionary<Tile, PriorityAndWeight>> weightSets;
        private readonly TileModelMapping tileModelMapping;
        private readonly IDictionary<int, FrequencySet> frequencySets;

        public WeightSetCollection(ITopoArray<int> weightSetByIndex, IDictionary<int, IDictionary<Tile, PriorityAndWeight>> weightSets, TileModelMapping tileModelMapping)
        {
            this.weightSetByIndex = weightSetByIndex;
            this.weightSets = weightSets;
            this.tileModelMapping = tileModelMapping;
            frequencySets = new Dictionary<int, FrequencySet>();
        }

        public FrequencySet Get(int index)
        {
            var id = weightSetByIndex.Get(index);

            if (frequencySets.TryGetValue(id, out var fs))
                return fs;

            return frequencySets[id] = GetFrequencySet(weightSets[id], tileModelMapping);
        }

        private static FrequencySet GetFrequencySet(IDictionary<Tile, PriorityAndWeight> weights, TileModelMapping tileModelMapping)
        {
            // TODO: Handle overlapped
            if (tileModelMapping.PatternCoordToTileCoordIndexAndOffset != null)
                throw new NotImplementedException();

            var offset = 0;
            var newWeights = new double[tileModelMapping.PatternModel.PatternCount];
            var newPriorities = new int[tileModelMapping.PatternModel.PatternCount];
            foreach (var kv in weights)
            {
                var pattern = tileModelMapping.TilesToPatternsByOffset[offset][kv.Key].Single();
                newWeights[pattern] = kv.Value.Weight;
                newPriorities[pattern] = kv.Value.Priority;
            }
            return new FrequencySet(newWeights, newPriorities);
        }
    }
}
