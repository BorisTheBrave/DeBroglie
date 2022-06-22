using System;
using System.Collections.Generic;
using DeBroglie.Topo;
using DeBroglie.Trackers;

namespace DeBroglie.Wfc
{

    internal class WavePropagatorOptions
    {
        public IBacktrackPolicy BacktrackPolicy { get; set; }
        public int MaxBacktrackDepth { get; set; }
        public IWaveConstraint[] Constraints { get; set; }
        public Func<double> RandomDouble { get; set; }
        public IIndexPicker IndexPicker { get; set; }
        public IPatternPicker PatternPicker { get; set; }
        public bool Clear { get; set; } = true;
        public ModelConstraintAlgorithm ModelConstraintAlgorithm { get; set; }
    }

    /// <summary>
    /// WavePropagator holds a wave, and supports updating it's possibilities 
    /// according to the model constraints.
    /// </summary>
    internal class WavePropagator
    {
        // Main data tracking what we've decided so far
        private Wave wave;

        private IPatternModelConstraint patternModelConstraint;

        // From model
        private int patternCount;
        private double[] frequencies;

        // Used for backtracking
        private Deque<IndexPatternItem> backtrackItems;
        private Deque<int> backtrackItemsLengths;
        private Deque<IndexPatternItem> prevChoices;
        // Used for MaxBacktrackDepth
        private int droppedBacktrackItemsCount;
        // In
        private int backtrackCount; // Purely informational
        private int backjumpCount; // Purely informational

        // Basic parameters
        private int indexCount;
        private readonly bool backtrack;
        private readonly int maxBacktrackDepth;
        private readonly IWaveConstraint[] constraints;
        private Func<double> randomDouble;

        // We evaluate constraints at the last possible minute, instead of eagerly like the model,
        // As they can potentially be expensive.
        private bool deferredConstraintsStep;

        // The overall status of the propagator, always kept up to date
        private Resolution status;

        public string contradictionReason;
        public object contradictionSource;

        private ITopology topology;
        private int directionsCount;

        private List<ITracker> trackers;
        private List<IChoiceObserver> choiceObservers;

        private readonly IIndexPicker indexPicker;
        private readonly IPatternPicker patternPicker;
        private IBacktrackPolicy backtrackPolicy;

        public WavePropagator(
            PatternModel model,
            ITopology topology,
            WavePropagatorOptions options)
        {
            this.patternCount = model.PatternCount;
            this.frequencies = model.Frequencies;

            this.indexCount = topology.IndexCount;
            this.backtrack = options.BacktrackPolicy != null;
            this.backtrackPolicy = options.BacktrackPolicy;
            this.maxBacktrackDepth = options.MaxBacktrackDepth;
            this.constraints = options.Constraints ?? new IWaveConstraint[0];
            this.topology = topology;
            this.randomDouble = options.RandomDouble ?? new Random().NextDouble;
            directionsCount = topology.DirectionsCount;
            this.indexPicker = options.IndexPicker ?? new EntropyTracker();
            this.patternPicker = options.PatternPicker ?? new WeightedRandomPatternPicker();

            switch (options.ModelConstraintAlgorithm)
            {
                case ModelConstraintAlgorithm.OneStep:
                    patternModelConstraint = new OneStepPatternModelConstraint(this, model);
                    break;
                case ModelConstraintAlgorithm.Default:
                case ModelConstraintAlgorithm.Ac4:
                    patternModelConstraint = new Ac4PatternModelConstraint(this, model);
                    break;
                case ModelConstraintAlgorithm.Ac3:
                    patternModelConstraint = new Ac3PatternModelConstraint(this, model);
                    break;
                default:
                    throw new Exception();
            }

            if (options.Clear)
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
            // Simple, inefficient way
            if (!Optimizations.QuickSelect)
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

            bool isContradiction = false;

            patternModelConstraint.DoSelect(index, chosenPattern);

            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (pattern == chosenPattern)
                {
                    continue;
                }
                if (wave.Get(index, pattern))
                {
                    // This is mostly a repeat of InternalBan, as except for patternModelConstraint,
                    // Selects are just seen as a set of bans


                    // Record information for backtracking
                    if (backtrack)
                    {
                        backtrackItems.Push(new IndexPatternItem
                        {
                            Index = index,
                            Pattern = pattern,
                        });
                    }

                    // Don't update patternModelConstraint here, it's been done above in DoSelect

                    // Update the wave
                    isContradiction = isContradiction || wave.RemovePossibility(index, pattern);

                    // Update trackers
                    foreach (var tracker in trackers)
                    {
                        tracker.DoBan(index, pattern);
                    }

                }
            }
            return false;
        }
        #endregion

        /// <summary>
        /// Returns the only possible value of a cell if there is only one, 
        /// otherwise returns -1 (multiple possible) or -2 (none possible)
        /// </summary>
        public int GetDecidedPattern(int index)
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

        public void StepConstraints()
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
        public string ContradictionReason => contradictionReason;
        public object ContradictionSource => contradictionSource;
        public int BacktrackCount => backtrackCount;
        public int BackjumpCount => backjumpCount;

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
            contradictionReason = null;
            contradictionSource = null;
            this.trackers = new List<ITracker>();
            this.choiceObservers = new List<IChoiceObserver>();
            indexPicker.Init(this);
            patternPicker.Init(this);
            backtrackPolicy?.Init(this);

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
         * Indicates that the generation cannot proceed, forcing the algorithm to backtrack or exit.
         */
        public void SetContradiction(string reason, object source)
        {
            status = Resolution.Contradiction;
            contradictionReason = reason;
            contradictionSource = source;
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
            // This will be true if the user has called Ban, etc, since the last step.
            if (deferredConstraintsStep)
            {
                StepConstraints();
            }

            // If we're already in a final state. skip making an observiation.
            if (status == Resolution.Undecided)
            {
                // Pick a index to use
                var index = indexPicker.GetRandomIndex(randomDouble);

                if (index != -1)
                {
                    // Pick a tile to select at that index
                    var pattern = patternPicker.GetRandomPossiblePatternAt(index, randomDouble);

                    RecordBacktrack(index, pattern);

                    // Use the pick
                    if (InternalSelect(index, pattern))
                    {
                        status = Resolution.Contradiction;
                    }
                }

                // Re-evaluate status
                if (status == Resolution.Undecided) patternModelConstraint.Propagate();
                if (status == Resolution.Undecided) StepConstraints();

                // If we've made all possible choices, and found no contradictions,
                // then we've succeeded.
                if (index == -1 && status == Resolution.Undecided)
                {
                    status = Resolution.Decided;
                    return status;
                }
            }

            TryBacktrackUntilNoContradiction();

            return status;
        }

        public void AddBacktrackPoint()
        {
            RecordBacktrack(-1, -1);
        }

        private void RecordBacktrack(int index, int pattern)
        {
            if (!backtrack)
                return;

            backtrackItemsLengths.Push(droppedBacktrackItemsCount + backtrackItems.Count);
            prevChoices.Push(new IndexPatternItem { Index = index, Pattern = pattern });

            foreach (var co in choiceObservers)
            {
                co.MakeChoice(index, pattern);
            }

            // Clean up backtracks if they are too long
            while (maxBacktrackDepth > 0 && backtrackItemsLengths.Count > maxBacktrackDepth)
            {
                var newDroppedCount = backtrackItemsLengths.Unshift();
                prevChoices.Unshift();
                backtrackItems.DropFirst(newDroppedCount - droppedBacktrackItemsCount);
                droppedBacktrackItemsCount = newDroppedCount;
            }

        }

        private void TryBacktrackUntilNoContradiction()
        {
            if (!backtrack)
                return;

            while (status == Resolution.Contradiction)
            {
                var backjumpAmount = backtrackPolicy.GetBackjump();

                for (var i = 0; i < backjumpAmount; i++)
                {
                    if (backtrackItemsLengths.Count == 1)
                    {
                        // We've backtracked as much as we can, but 
                        // it's still not possible. That means it is imposible
                        return;
                    }

                    // Actually undo various bits of state
                    DoBacktrack();
                    var item = prevChoices.Pop();
                    status = Resolution.Undecided;
                    contradictionReason = null;
                    contradictionSource = null;
                    foreach (var co in choiceObservers)
                    {
                        co.Backtrack();
                    }

                    if (backjumpAmount == 1)
                    {
                        backtrackCount++;

                        // Mark the given choice as impossible
                        if (item.Index >= 0 && InternalBan(item.Index, item.Pattern))
                        {
                            status = Resolution.Contradiction;
                        }
                    }
                }

                if(backjumpAmount > 1)
                {
                    backjumpCount++;
                }

                // Revalidate status.
                if (status == Resolution.Undecided) patternModelConstraint.Propagate();
                if (status == Resolution.Undecided) StepConstraints();
            }
        }

        // Undoes any work that was done since the last backtrack point.
        private void DoBacktrack()
        {
            var targetLength = backtrackItemsLengths.Pop() - droppedBacktrackItemsCount;
            // Undo each item that was added since the backtrack
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
                patternModelConstraint.UndoBan(index, pattern);

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

        public void AddChoiceObserver(IChoiceObserver co)
        {
            choiceObservers.Add(co);
        }

        public void RemoveChoiceObserver(IChoiceObserver co)
        {
            choiceObservers.Remove(co);
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
            return TopoArray.CreateByIndex(GetDecidedPattern, topology);
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

        public IEnumerable<int> GetPossiblePatterns(int index)
        {
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    yield return pattern;
                }
            }
        }
    }
}
