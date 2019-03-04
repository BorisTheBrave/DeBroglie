using System;
using System.Collections.Generic;
using DeBroglie.Topo;

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

        // Used for backtracking
        private Stack<Wave> prevWaves;
        private Stack<int[,,]> prevCompatible;
        private Stack<PropagateItem> prevChoices;
        private int backtrackCount; // Purely informational

        // From model
        private int[][][] propagator;
        private int patternCount;
        private double[] frequencies;

        // Basic parameters
        private int width;
        private int height;
        private int depth;
        private int indices;
        private bool periodicX;
        private bool periodicY;
        private bool periodicZ;
        private readonly bool backtrack;
        private readonly IWaveConstraint[] constraints;
        private Random random;

        // List of locations that still need to be checked against for fulfilling the model's conditions
        private Stack<PropagateItem> toPropagate;

        // We evaluate constraints at the last possible minute, instead of eagerly like the model,
        // As they can potentially be expensive.
        private bool deferredConstraintsStep;

        // The overall status of the propagator, always kept up to date
        private Resolution status;


        private Topology topology;
        private int directionsCount;

        /**
          * compatible[index, pattern, direction] contains the number of patterns present in the wave
          * that can be placed in the cell next to index in the opposite direction of direction without being
          * in contradiction with pattern placed in index.
          * If possibilites[index][pattern] is set to false, then compatible[index, pattern, direction] has every direction negative or null
          */
        private int[,,] compatible;

        public WavePropagator(PatternModel model, Topology topology, bool backtrack = false, IWaveConstraint[] constraints = null, Random random = null, bool clear = true)
        {
            this.propagator = model.Propagator;
            this.patternCount = model.PatternCount;
            this.frequencies = model.Frequencies;

            this.width = topology.Width;
            this.height = topology.Height;
            this.depth = topology.Depth;
            this.indices = width * height * depth;
            this.periodicX = topology.PeriodicX;
            this.periodicY = topology.PeriodicY;
            this.periodicZ = topology.PeriodicZ;
            this.backtrack = backtrack;
            this.constraints = constraints ?? new IWaveConstraint[0];
            this.topology = topology;
            this.random = random ?? new Random();
            directionsCount = topology.Directions.Count;

            this.toPropagate = new Stack<PropagateItem>();

            if(clear)
                Clear();
        }

        // This is only exposed publically
        // in case users write their own constraints, it's not 
        // otherwise useful.
        #region Internal API

        public Wave Wave => wave;
        public int Width => width;
        public int Height => height;
        public int Depth => depth;
        public int Indices => indices;
        public bool PeriodicX => periodicX;
        public bool PeriodicY => periodicY;
        public bool PeriodicZ => periodicZ;
        public Topology Topology => topology;
        public Random Random => random;

        public int[][][] Propagator => propagator;
        public int PatternCount => patternCount;
        public double[] Frequencies => frequencies;

        /**
         * Requires that index, pattern is possible
         */
        public bool InternalBan(int index, int pattern)
        {
            // Update compatible (so that we never ban twice)
            for (var d = 0; d < directionsCount; d++)
            {
                compatible[index, pattern, d] = 0;
            }
            // Queue any possible consequences of this changing.
            toPropagate.Push(new PropagateItem
            {
                Index = index,
                Pattern = pattern,
            });
            // Update the wave
            return wave.RemovePossibility(index, pattern);
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

        private void Propagate()
        {
            while (toPropagate.Count > 0)
            {
                var item = toPropagate.Pop();
                int x, y, z;
                topology.GetCoord(item.Index, out x, out y, out z);
                for (var d = 0; d < directionsCount; d++)
                {
                    int i2;
                    if (!topology.TryMove(x, y, z, (Direction)d, out i2))
                    {
                        continue;
                    }
                    var patterns = propagator[item.Pattern][d];
                    foreach (var p in patterns)
                    {
                        var c = --compatible[i2, p, d];
                        // We've just now ruled out this possible pattern
                        if (c == 0)
                        {
                            if (InternalBan(i2, p))
                            {
                                status = Resolution.Contradiction;
                                return;
                            }
                        }
                    }
                }
            }
            return;
        }

        private int GetRandomPossiblePatternAt(int index)
        {
            var s = 0.0;
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    s += frequencies[pattern];
                }
            }
            var r = random.NextDouble() * s;
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    r -= frequencies[pattern];
                }
                if (r <= 0)
                {
                    return pattern;
                }
            }
            return patternCount - 1;
        }

        private void Observe(out int index, out int pattern)
        {
            // Choose a random cell
            index = wave.GetRandomMinEntropyIndex(random);
            if (index == Wave.AllCellsDecided)
            {
                pattern = -1;
                return;
            }
            // Choose a random pattern
            pattern = GetRandomPossiblePatternAt(index);
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
                Propagate();
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
                Propagate();
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
            wave = new Wave(frequencies, indices, topology.Mask);
            toPropagate.Clear();
            status = Resolution.Undecided;

            if(backtrack)
            {
                prevWaves = new Stack<Wave>();
                prevCompatible = new Stack<int[,,]>();
                prevChoices = new Stack<PropagateItem>();
            }


            compatible = new int[indices, patternCount, directionsCount];
            for (int index = 0; index < indices; index++)
            {
                if (!topology.ContainsIndex(index))
                    continue;

                for (int pattern = 0; pattern < patternCount; pattern++)
                {
                    for (int d = 0; d < directionsCount; d++)
                    {
                        var inverseDirection = topology.Directions.Inverse((Direction)d);
                        var compatiblePatterns = propagator[pattern][(int)inverseDirection].Length;
                        compatible[index, pattern, d] = compatiblePatterns;
                        if(compatiblePatterns == 0 && topology.TryMove(index, inverseDirection, out var dest) && wave.Get(index, pattern))
                        {
                            if (InternalBan(index, pattern))
                            {
                                return status = Resolution.Contradiction;
                            }
                            break;
                        }
                    }
                }
            }

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
            Propagate();
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
                prevWaves.Push(wave.Clone());
                prevCompatible.Push((int[,,])compatible.Clone());
            }

            // Pick a tile and Select a pattern from it.
            Observe(out index, out var pattern);

            // Record what was selected for backtracking purposes
            if(index != Wave.AllCellsDecided && backtrack)
            {
                prevChoices.Push(new PropagateItem { Index = index, Pattern = pattern });
            }

            // After a backtrack we resume here
            restart:

            if (status == Resolution.Undecided) Propagate();
            if (status == Resolution.Undecided) StepConstraints();

            // Are all things are fully chosen?
            if (index == Wave.AllCellsDecided && status == Resolution.Undecided)
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
                    if(prevWaves.Count == 0)
                    {
                        // We've backtracked as much as we can, but 
                        // it's still not possible. That means it is imposible
                        return Resolution.Contradiction;
                    }
                    wave = prevWaves.Pop();
                    compatible = prevCompatible.Pop();
                    var item = prevChoices.Pop();
                    backtrackCount++;
                    toPropagate.Clear();
                    // Mark the given choice as impossible
                    if (InternalBan(item.Index, item.Pattern))
                    {
                        // Still in contradiction, need to backtrack further
                        continue;
                    }
                    status = Resolution.Undecided;
                    goto restart;
                }
            }

            return status;
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
            var result = new int[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        var index = topology.GetIndex(x, y, z);
                        result[x, y, z] = GetDecidedCell(index);
                    }
                }
            }
            return new TopoArray3D<int>(result, topology);
        }

        /**
         * Returns an array where each cell is a list of remaining possible patterns.
         */
        public ITopoArray<ISet<int>> ToTopoArraySets()
        {
            var result = new ISet<int>[width, height, depth];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        var index = topology.GetIndex(x, y, 0);
                        var hs = new HashSet<int>();
                        result[x, y, z] = hs;

                        for (var p = 0; p < patternCount; p++)
                        {
                            if (wave.Get(index, p))
                            {
                                hs.Add(p);
                            }
                        }
                    }
                }
            }
            return new TopoArray3D<ISet<int>>(result, topology);
        }

        private struct PropagateItem
        {
            public int Index { get; set; }
            public int Pattern { get; set; }
        }
    }
}
