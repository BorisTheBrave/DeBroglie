using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Trackers
{
    internal class FrequencySet
    {
        public struct Group
        {
            public int priority;
            public int patternCount;
            public double weightSum;
            public int[] patterns;
            public double[] frequencies;
            public List<double> plogp;
        }

        public FrequencySet(double[] weights, int[] priorities = null)
        {
            if(priorities == null)
            {
                priorities = Enumerable.Repeat(0, weights.Length).ToArray();
            }

            var groupsByPriority = new Dictionary<int, Group>();
            var frequenciesByPriority = new Dictionary<int, List<double>>();
            var patternsByPriority = new Dictionary<int, List<int>>();
            // Group the patterns by prioirty
            for (var i = 0; i < weights.Length; i++)
            {
                var priority = priorities[i];
                var weight = weights[i];
                if (!groupsByPriority.TryGetValue(priority, out var group))
                {
                    group = new Group
                    {
                        priority = priority,
                        plogp = new List<double>(),
                    };
                    frequenciesByPriority[priority] = new List<double>();
                    patternsByPriority[priority] = new List<int>();
                }
                group.patternCount += 1;
                group.weightSum += weight;
                patternsByPriority[priority].Add(i);
                groupsByPriority[priority] = group;
            }
            frequencies = new double[weights.Length];
            plogp = new double[weights.Length];
            // Compute normalized frequencies
            for (var i = 0; i < weights.Length; i++)
            {
                var priority = priorities[i];
                var group = groupsByPriority[priority];
                var f = weights[i] / group.weightSum;
                frequencies[i] = f;
                plogp[i] = ToPLogP(f);
                frequenciesByPriority[priority].Add(f);
                group.plogp.Add(ToPLogP(f));
            }
            // Convert from list to array
            foreach (var priority in groupsByPriority.Keys.ToList())
            {
                var g = groupsByPriority[priority];
                groupsByPriority[priority] = new Group
                {
                    priority = g.priority,
                    patternCount = g.patternCount,
                    weightSum = g.weightSum,
                    patterns = patternsByPriority[priority].ToArray(),
                    frequencies = frequenciesByPriority[priority].ToArray(),
                    plogp = g.plogp,
                };
            }
            // Order groups by priority
            groups = groupsByPriority.OrderByDescending(x => x.Key).Select(x => x.Value).ToArray();
            var priorityToPriorityIndex = groups.Select((g, i) => new { g, i }).ToDictionary(t => t.g.priority, t => t.i);
            priorityIndices = priorities.Select(p => priorityToPriorityIndex[p]).ToArray();
        }

        private double ToPLogP(double frequency)
        {
            return frequency > 0.0 ? frequency * Math.Log(frequency) : 0.0;
        }

        public int[] priorityIndices;
        public double[] frequencies;
        public double[] plogp;

        public Group[] groups { get; }
    }
}
