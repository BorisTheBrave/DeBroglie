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
        private Wave wave;

        // From model
        private int[][][] propagator;
        private int patternCount;
        private double[] frequencies;

        private int width;
        private int height;
        private int indices;

        private Random random = new Random();

        private Stack<PropagateItem> toPropagate;

        private bool periodic;

        Directions directions;

        /**
          * compatible[index, pattern, direction] contains the number of patterns present in the wave
          * that can be placed in the cell next to index in the opposite direction of direction without being
          * in contradiction with pattern placed in index.
          * If possibilites[index][pattern] is set to false, then compatible[index, pattern, direction] has every direction negative or null
          */
        private int[,,] compatible;

        public WavePropagator(Model model, int width, int height, bool periodic)
        {
            wave = new Wave(model.Frequencies, width * height);

            this.propagator = model.Propagator;
            this.patternCount = model.PatternCount;
            this.frequencies = model.Frequencies;

            this.width = width;
            this.height = height;
            this.indices = width * height;
            this.periodic = periodic;

            this.directions = Directions.Cartesian2dDirections;

            this.toPropagate = new Stack<PropagateItem>();

            // Initialize compatible
            compatible = new int[indices, patternCount, directions.Count];
            for (int index = 0; index < indices; index++)
            {
                for (int pattern = 0; pattern < patternCount; pattern++)
                {
                    for (int d = 0; d < directions.Count; d++)
                    {
                        compatible[index, pattern, d] = propagator[pattern][directions.Inverse(d)].Length;
                    }
                }
            }
        }

        private int GetIndex(int x, int y)
        {
            return x + y * width;
        }

        private void GetCoord(int index, out int x, out int y)
        {
            x = index % width;
            y = index / width;
        }

        private bool TryMove(int index, int direction, out int dest)
        {
            int x, y;
            GetCoord(index, out x, out y);
            return TryMove(x, y, direction, out dest);
        }

        private bool TryMove(int x, int y, int direction, out int dest)
        {
            x += directions.DX[direction];
            y += directions.DY[direction];
            if(periodic)
            {
                if (x < 0) x += width;
                if (x >= width) x -= width;
                if (y < 0) y += height;
                if (y >= height) y -= height;
            }
            else
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    dest = -1;
                    return false;
                }
            }
            dest = GetIndex(x, y);
            return true;
        }

        /**
         * Requires that index, pattern is possible
         */
        private bool UnsafeBan(int index, int pattern)
        {
            // Update compatible (so that we never ban twice)
            for (var d = 0; d < directions.Count; d++)
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

        public bool Ban(int index, int pattern)
        {
            if (wave.Get(index, pattern))
            {
                return UnsafeBan(index, pattern);
            }
            return false;
        }

        public bool Select(int index, int chosenPattern)
        {
            for (var pattern = 0; pattern < patternCount; pattern++)
            {
                if (pattern == chosenPattern)
                    continue;
                if (Ban(index, pattern))
                    return true;
            }
            return false;
        }

        private CellStatus Propagate()
        {
            PropagateItem item;
            while (toPropagate.TryPop(out item))
            {
                int x, y;
                GetCoord(item.Index, out x, out y);
                for (var d = 0; d < directions.Count; d++)
                {
                    int i2;
                    if(!TryMove(x, y, d, out i2))
                    {
                        continue;
                    }
                    var patterns = propagator[item.Pattern][d];
                    foreach(var p in patterns)
                    {
                        var c = --compatible[i2, p, d];
                        // We've just now ruled out this possible pattern
                        if(c == 0)
                        {
                            if (UnsafeBan(i2, p))
                                return CellStatus.Contradiction;
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

        private CellStatus Observe()
        {
            // Choose a random cell
            var index = wave.GetRandomMinEntropyIndex(random);
            if (index == -1)
                return CellStatus.Decided;
            // Choose a random pattern
            var chosenPattern = GetRandomPossiblePatternAt(index);
            // Decide on the given cell
            if (Select(index, chosenPattern))
                return CellStatus.Contradiction;
            return CellStatus.Undecided;
        }

        public CellStatus Step()
        {
            CellStatus status = Observe();
            if (status != CellStatus.Undecided) return status;
            status = Propagate();
            return status;
        }

        public CellStatus Run()
        {
            CellStatus status;
            while (true)
            {
                status = Step();
                if (status != CellStatus.Undecided) return status;
            }
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

        // Returns the array of resolved patterns, or
        // -1 or -2 to indicate cells that are undecided or in contradiction.
        public int[,] ToArray()
        {
            var result = new int[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var index = GetIndex(x, y);
                    result[x, y] = GetDecidedCell(index);
                }
            }
            return result;
        }

        public List<int>[,] ToArraySets()
        {
            var result = new List<int>[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var index = GetIndex(x, y);
                    List<int> hs = result[x, y] = new List<int>();

                    for (var p = 0; p < patternCount; p++)
                    {
                        if (wave.Get(index, p))
                        {
                            hs.Add(p);
                        }
                    }
                }
            }
            return result;
        }

        private struct PropagateItem
        {
            public int Index { get; set; }
            public int Pattern { get; set; }
        }
    }
}
