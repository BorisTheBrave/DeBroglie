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
            public List<int> patterns;
            public List<double> frequencies;
            public List<double> plogp;
        }

        public FrequencySet(double[] weights, int[] priorities = null)
        {
            if(priorities == null)
            {
                priorities = Enumerable.Repeat(0, weights.Length).ToArray();
            }

            var groupsByPriority = new Dictionary<int, Group>();
            for(var i=0;i<weights.Length;i++)
            {
                var priority = priorities[i];
                var weight = weights[i];
                if (!groupsByPriority.TryGetValue(priority, out var group))
                {
                    group = new Group {
                        priority = priority,
                        patterns = new List<int>(),
                        frequencies = new List<double>(),
                        plogp = new List<double>(),
                    };
                }
                group.patternCount += 1;
                group.weightSum += weight;
                group.patterns.Add(i);
                groupsByPriority[priority] = group;
            }
            frequencies = new double[weights.Length];
            plogp = new double[weights.Length];
            for (var i = 0; i < weights.Length; i++)
            {
                var group = groupsByPriority[priorities[i]];
                var f = weights[i] / group.weightSum;
                frequencies[i] = f;
                plogp[i] = ToPLogP(f);
                group.frequencies.Add(f);
                group.plogp.Add(ToPLogP(f));
            }
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
