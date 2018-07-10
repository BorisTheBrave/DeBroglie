using System;
using System.Collections.Generic;

namespace DeBroglie
{

    public enum CellStatus
    {
        Decided = 0,
        Undecided = -1,
        Contradiction = -2,
    }

    /**
     * WavePropagator holds a wave, and supports updating it's possibilities
     * according to the model constraints.
     */
    public class WavePropagator
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
        private bool periodic;
        private readonly bool backtrack;
        private readonly IWaveConstraint[] constraints;
        private Random random;

        private Stack<PropagateItem> toPropagate;


        private Topology topology;
        private int directionsCount;

        /**
          * compatible[index, pattern, direction] contains the number of patterns present in the wave
          * that can be placed in the cell next to index in the opposite direction of direction without being
          * in contradiction with pattern placed in index.
          * If possibilites[index][pattern] is set to false, then compatible[index, pattern, direction] has every direction negative or null
          */
        private int[,,] compatible;

        public WavePropagator(Model model, Topology topology, bool backtrack = false, IWaveConstraint[] constraints = null, Random random = null, bool clear = true)
        {
            this.propagator = model.Propagator;
            this.patternCount = model.PatternCount;
            this.frequencies = model.Frequencies;

            this.width = topology.Width;
            this.height = topology.Height;
            this.depth = topology.Depth;
            this.indices = width * height * depth;
            this.periodic = topology.Periodic;
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
        public bool Periodic => periodic;
        public Topology Topology => topology;

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

        private CellStatus Propagate()
        {
            while (toPropagate.Count > 0)
            {
                var item = toPropagate.Pop();
                int x, y, z;
                topology.GetCoord(item.Index, out x, out y, out z);
                for (var d = 0; d < directionsCount; d++)
                {
                    int i2;
                    if (!topology.TryMove(x, y, z, d, out i2))
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
                                return CellStatus.Contradiction;
                            }
                        }
                    }
                }
            }
            return CellStatus.Undecided;
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

        private CellStatus Observe(out int index, out int pattern)
        {
            // Choose a random cell
            index = wave.GetRandomMinEntropyIndex(random);
            if (index == -1)
            {
                pattern = -1;
                return CellStatus.Decided;
            }
            // Choose a random pattern
            pattern = GetRandomPossiblePatternAt(index);
            // Decide on the given cell
            if (InternalSelect(index, pattern))
            {
                return CellStatus.Contradiction;
            }
            return CellStatus.Undecided;
        }

        // Returns the only possible value of a cell if there is only one,
        // otherwise returns -1 (multiple possible) or -2 (none possible)
        private int GetDecidedCell(int index)
        {
            int decidedPattern = (int)CellStatus.Contradiction;
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (wave.Get(index, pattern))
                {
                    if (decidedPattern == (int)CellStatus.Contradiction)
                    {
                        decidedPattern = pattern;
                    }
                    else
                    {
                        return (int)CellStatus.Undecided;
                    }
                }
            }
            return decidedPattern;
        }

        private CellStatus InitConstraints()
        {
            foreach (var constraint in constraints)
            {
                var status = constraint.Init(this);
                if (status != CellStatus.Undecided) return status;
                status = Propagate();
                if (status != CellStatus.Undecided) return status;
            }
            return CellStatus.Undecided;
        }

        private CellStatus StepConstraints()
        {
            foreach (var constraint in constraints)
            {
                var status = constraint.Check(this);
                if (status != CellStatus.Undecided) return status;
                status = Propagate();
                if (status != CellStatus.Undecided) return status;
            }
            return CellStatus.Undecided;
        }

        public int BacktrackCount => backtrackCount;

        /**
         * Resets the wave to it's original state
         */
        public void Clear()
        {
            wave = new Wave(frequencies, indices);

            if(backtrack)
            {
                prevWaves = new Stack<Wave>();
                prevCompatible = new Stack<int[,,]>();
                prevChoices = new Stack<PropagateItem>();
            }

            compatible = new int[indices, patternCount, directionsCount];
            for (int index = 0; index < indices; index++)
            {
                for (int pattern = 0; pattern < patternCount; pattern++)
                {
                    for (int d = 0; d < directionsCount; d++)
                    {
                        compatible[index, pattern, d] = propagator[pattern][topology.Directions.Inverse(d)].Length;
                    }
                }
            }

            InitConstraints();
        }

        /**
         * Removes pattern as a possibility from index
         */
        public CellStatus Ban(int x, int y, int z, int pattern)
        {
            var index = topology.GetIndex(x, y, z);
            if (wave.Get(index, pattern))
            {
                if (InternalBan(index, pattern))
                {
                    return CellStatus.Contradiction;
                }
            }
            return Propagate();
        }

        /**
         * Removes all other patterns as possibilities for index.
         */
        public CellStatus Select(int x, int y, int z, int pattern)
        {
            var index = topology.GetIndex(x, y, z);
            if (InternalSelect(index, pattern))
            {
                return CellStatus.Contradiction;
            }
            return Propagate();
        }

        /**
         * Make some progress in the WaveFunctionCollapseAlgorithm
         */
        public CellStatus Step()
        {
            if(backtrack)
            {
                prevWaves.Push(wave.Clone());
                prevCompatible.Push((int[,,])compatible.Clone());
            }

            int index, pattern;
            var status = Observe(out index, out pattern);

            if(backtrack)
            {
                prevChoices.Push(new PropagateItem { Index = index, Pattern = pattern });
            }

            restart:

            if (status == CellStatus.Undecided) status = Propagate();
            if (status == CellStatus.Undecided) status = StepConstraints();

            if (backtrack && status == CellStatus.Contradiction)
            {
                // Actually backtrack
                while (true)
                {
                    if(prevWaves.Count == 0)
                    {
                        // We've backtracked as much as we can, but 
                        // it's still not possible. That means it is imposible
                        return CellStatus.Contradiction;
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
                    status = CellStatus.Undecided;
                    goto restart;
                }
            }

            return status;
        }

        /**
         * Rpeatedly step until the status is Decided or Contradiction
         */
        public CellStatus Run()
        {
            CellStatus status;
            while (true)
            {
                status = Step();
                if (status != CellStatus.Undecided) return status;
            }
        }

        /**
         * Returns the array of decided patterns, writing
         * -1 or -2 to indicate cells that are undecided or in contradiction.
         */
        public ITopArray<int> ToTopArray()
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
            return new TopArray3D<int>(result, topology);
        }

        /**
         * Returns an array where each cell is a list of remaining possible patterns.
         */
        public ITopArray<ISet<int>> ToTopArraySets()
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
            return new TopArray3D<ISet<int>>(result, topology);
        }

        private struct PropagateItem
        {
            public int Index { get; set; }
            public int Pattern { get; set; }
        }
    }
}
