using System;
using System.Collections.Generic;
using DeBroglie.Topo;
using DeBroglie.Trackers;

namespace DeBroglie.Wfc
{
    /// <summary>
    /// WavePropagator holds a wave, and supports updating it's possibilities 
    /// according to the model constraints.
    /// </summary>
    internal class WavePropagator
    {
        // Main data tracking what we've decided so far
        private Wave wave;

        private PatternModelConstraint patternModelConstraint;

        // From model
        private int patternCount;
        private double[] frequencies;

        // Used for backtracking
        private Deque<IndexPatternItem> backtrackItems;
        private Deque<int> backtrackItemsLengths;
        private Deque<IndexPatternItem> prevChoices;
        private int droppedBacktrackItemsCount;
        private int backtrackCount; // Purely informational

        // Basic parameters
        private int indexCount;
        private readonly bool backtrack;
        private readonly int backtrackDepth;
        private readonly IWaveConstraint[] constraints;
        private Func<double> randomDouble;
        private readonly FrequencySet[] frequencySets;

        // We evaluate constraints at the last possible minute, instead of eagerly like the model,
        // As they can potentially be expensive.
        private bool deferredConstraintsStep;

        // The overall status of the propagator, always kept up to date
        private Resolution status;

        private ITopology topology;
        private int directionsCount;

        private List<ITracker> trackers;

        private IPickHeuristic pickHeuristic;

        public WavePropagator(
            PatternModel model,
            ITopology topology,
            int backtrackDepth = 0,
            IWaveConstraint[] constraints = null,
            Func<double> randomDouble = null,
            FrequencySet[] frequencySets = null,
            bool clear = true)
        {
            this.patternCount = model.PatternCount;
            this.frequencies = model.Frequencies;

            this.indexCount = topology.IndexCount;
            this.backtrack = backtrackDepth != 0;
            this.backtrackDepth = backtrackDepth;
            this.constraints = constraints ?? new IWaveConstraint[0];
            this.topology = topology;
            this.randomDouble = randomDouble ?? new Random().NextDouble;
            this.frequencySets = frequencySets;
            directionsCount = topology.DirectionsCount;

            patternModelConstraint = new PatternModelConstraint(this, model);

            if (clear)
                Clear();
        }

        // This is only exposed publically
        // in case users write their own constraints, it's not 
        // otherwise useful.
        #region Internal API

        public Wave Wave => wave;
        public int IndexCount => indexCount;
        public ITopology Topology => topology;
        public Func<double> RandomDouble => randomDouble;

        public int PatternCount => patternCount;
        public double[] Frequencies => frequencies;

        /**
         * Requires that index, pattern is possible
         */
        public bool InternalBan(int index, int pattern)
        {
            // Record information for backtracking
            if (backtrack)
            {
                backtrackItems.Push(new IndexPatternItem
                {
                    Index = index,
                    Pattern = pattern,
                });
            }

            patternModelConstraint.DoBan(index, pattern);
            
            // Update the wave
            var isContradiction = wave.RemovePossibility(index, pattern);

            // Update trackers
            foreach (var tracker in trackers)
            {
                tracker.DoBan(index, pattern);
            }

            return isContradiction;
        }

        public bool InternalSelect(int index, int chosenPattern)
        {
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (pattern == chosenPattern)
                {
                    continue;
                }
                if (wave.Get(index, pattern))
                {
                    if (InternalBan(index, pattern))
                        return true;
                }
            }
            return false;
        }
        #endregion


        private void Observe(out int index, out int pattern)
        {
            pickHeuristic.PickObservation(out index, out pattern);
            if (index == -1)
            {
                return;
            }

            // Decide on the given cell
            if (InternalSelect(index, pattern))
            {
                status = Resolution.Contradiction;
            }
        }

        // Returns the only possible value of a cell if there is only one,
        // otherwise returns -1 (multiple possible) or -2 (none possible)
        private int GetDecidedCell(int index)
        {
            int decidedPattern = (int)Resolution.Contradiction;
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    if (decidedPattern == (int)Resolution.Contradiction)
                    {
                        decidedPattern = pattern;
                    }
                    else
                    {
                        return (int)Resolution.Undecided;
                    }
                }
            }
            return decidedPattern;
        }

        private void InitConstraints()
        {
            foreach (var constraint in constraints)
            {
                constraint.Init(this);
                if (status != Resolution.Undecided) return;
                patternModelConstraint.Propagate();
                if (status != Resolution.Undecided) return;
            }
            return;
        }

        private void StepConstraints()
        {
            // TODO: Do we need to worry about evaluating constraints multiple times?
            foreach (var constraint in constraints)
            {
                constraint.Check(this);
                if (status != Resolution.Undecided) return;
                patternModelConstraint.Propagate();
                if (status != Resolution.Undecided) return;
            }
            deferredConstraintsStep = false;
        }

        public Resolution Status => status;
        public int BacktrackCount => backtrackCount;

        /**
         * Resets the wave to it's original state
         */
        public Resolution Clear()
        {
            wave = new Wave(frequencies.Length, indexCount);

            if (backtrack)
            {
                backtrackItems = new Deque<IndexPatternItem>();
                backtrackItemsLengths = new Deque<int>();
                backtrackItemsLengths.Push(0);
                prevChoices = new Deque<IndexPatternItem>();
            }

            status = Resolution.Undecided;
            this.trackers = new List<ITracker>();
            if (frequencySets != null)
            {
                var entropyTracker = new ArrayPriorityEntropyTracker(wave, frequencySets, topology.Mask);
                entropyTracker.Reset();
                AddTracker(entropyTracker);
                pickHeuristic = new ArrayPriorityEntropyHeuristic(entropyTracker, randomDouble);
            }
            else
            {
                var entropyTracker = new EntropyTracker(wave, frequencies, topology.Mask);
                entropyTracker.Reset();
                AddTracker(entropyTracker);
                pickHeuristic = new EntropyHeuristic(entropyTracker, randomDouble);
            }

            patternModelConstraint.Clear();

            if (status == Resolution.Contradiction) return status;

            InitConstraints();

            return status;
        }

        /**
         * Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
         */
        public void SetContradiction()
        {
            status = Resolution.Contradiction;
        }

        /**
         * Removes pattern as a possibility from index
         */
        public Resolution Ban(int x, int y, int z, int pattern)
        {
            var index = topology.GetIndex(x, y, z);
            if (wave.Get(index, pattern))
            {
                deferredConstraintsStep = true;
                if (InternalBan(index, pattern))
                {
                    return status = Resolution.Contradiction;
                }
            }
            patternModelConstraint.Propagate();
            return status;
        }

        /**
         * Make some progress in the WaveFunctionCollapseAlgorithm
         */
        public Resolution Step()
        {
            int index;

            // This will true if the user has called Ban, etc, since the last step.
            if (deferredConstraintsStep)
            {
                StepConstraints();
            }

            // If we're already in a final state. skip making an observiation, 
            // and jump to backtrack handling / return.
            if (status != Resolution.Undecided)
            {
                index = 0;
                goto restart;
            }

            // Record state before making a choice
            if (backtrack)
            {
                backtrackItemsLengths.Push(droppedBacktrackItemsCount + backtrackItems.Count);
                // Clean up backtracks if they are too long
                while (backtrackDepth != -1 && backtrackItemsLengths.Count > backtrackDepth)
                {
                    var newDroppedCount = backtrackItemsLengths.Unshift();
                    prevChoices.Unshift();
                    backtrackItems.DropFirst(newDroppedCount - droppedBacktrackItemsCount);
                    droppedBacktrackItemsCount = newDroppedCount;
                }
            }

            // Pick a tile and Select a pattern from it.
            Observe(out index, out var pattern);

            // Record what was selected for backtracking purposes
            if(index != -1 && backtrack)
            {
                prevChoices.Push(new IndexPatternItem { Index = index, Pattern = pattern });
            }

            // After a backtrack we resume here
            restart:

            if (status == Resolution.Undecided) patternModelConstraint.Propagate();
            if (status == Resolution.Undecided) StepConstraints();

            // Are all things are fully chosen?
            if (index == -1 && status == Resolution.Undecided)
            {
                status = Resolution.Decided;
                return status;
            }

            if (backtrack && status == Resolution.Contradiction)
            {
                // After back tracking, it's no logner the case things are fully chosen
                index = 0;

                // Actually backtrack
                while (true)
                {
                    if(backtrackItemsLengths.Count == 1)
                    {
                        // We've backtracked as much as we can, but 
                        // it's still not possible. That means it is imposible
                        return Resolution.Contradiction;
                    }
                    DoBacktrack();
                    var item = prevChoices.Pop();
                    backtrackCount++;
                    status = Resolution.Undecided;
                    // Mark the given choice as impossible
                    if (InternalBan(item.Index, item.Pattern))
                    {
                        status = Resolution.Contradiction;
                    }
                    if (status == Resolution.Undecided) patternModelConstraint.Propagate();

                    if (status == Resolution.Contradiction)
                    {
                        // If still in contradiction, repeat backtracking

                        continue;
                    }
                    else
                    {
                        // Include the last ban as part of the previous backtrack
                        backtrackItemsLengths.Pop();
                        backtrackItemsLengths.Push(droppedBacktrackItemsCount + backtrackItems.Count);
                    }
                    goto restart;
                }
            }

            return status;
        }

        private void DoBacktrack()
        {
            var targetLength = backtrackItemsLengths.Pop() - droppedBacktrackItemsCount;
            // Undo each item
            while (backtrackItems.Count > targetLength)
            {
                var item = backtrackItems.Pop();
                var index = item.Index;
                var pattern = item.Pattern;

                // Also add the possibility back
                // as it is removed in InternalBan
                wave.AddPossibility(index, pattern);
                // Update trackers
                foreach(var tracker in trackers)
                {
                    tracker.UndoBan(index, pattern);
                }
                // Next, undo the decremenents done in Propagate
                patternModelConstraint.UndoBan(item);

            }
        }

        public void AddTracker(ITracker tracker)
        {
            trackers.Add(tracker);
        }

        public void RemoveTracker(ITracker tracker)
        {
            trackers.Remove(tracker);
        }

        /**
         * Rpeatedly step until the status is Decided or Contradiction
         */
        public Resolution Run()
        {
            while (true)
            {
                Step();
                if (status != Resolution.Undecided) return status;
            }
        }

        /**
         * Returns the array of decided patterns, writing
         * -1 or -2 to indicate cells that are undecided or in contradiction.
         */
        public ITopoArray<int> ToTopoArray()
        {
            return TopoArray.CreateByIndex(GetDecidedCell, topology);
        }

        /**
         * Returns an array where each cell is a list of remaining possible patterns.
         */
        public ITopoArray<ISet<int>> ToTopoArraySets()
        {
            return TopoArray.CreateByIndex(index =>
            {
                var hs = new HashSet<int>();
                for (var pattern = 0; pattern < patternCount; pattern++)
                {
                    if (wave.Get(index, pattern))
                    {
                        hs.Add(pattern);
                    }
                }

                return (ISet<int>)(hs);
            }, topology);
        }
    }
}
