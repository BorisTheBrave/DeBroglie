using System;
using System.Collections.Generic;

namespace DeBroglie.Wfc
{
    public interface IBacktrackPolicy
    {
        void MakeChoice();

        void Backtrack();

        /// <summary>
        /// 0  = Give up
        /// 1  = Backtrack
        /// >1 = Backjump
        /// </summary>
        int GetBackjump();
    }



    public class ConstantBacktrackPolicy : IBacktrackPolicy
    {
        private readonly int amount;

        public ConstantBacktrackPolicy(int amount)
        {
            this.amount = amount;
        }

        public void Backtrack()
        {
        }

        public int GetBackjump()
        {
            return amount;
        }

        public void MakeChoice()
        {
        }
    }

    public class PatienceBackjumpPolicy : IBacktrackPolicy
    {
        private long counter;
        private int depth;
        private int maxDepth;
        private long start;

        private List<Level> levels;
        

        public void Reset()
        {
            counter = 0;
            depth = 0;
            maxDepth = 0;
            start = 0;
        }

        public void MakeChoice()
        {
            depth++;
            if(depth > maxDepth)
            {
                maxDepth = depth;
                // Reset levels
                levels = null;
                start = counter;
            }
        }

        public void Backtrack()
        {
            counter++;
            depth--;
        }

        private Level CreateLevel(int level)
        {
            return new Level
            {
                depth = maxDepth - 4 * (int)Math.Pow(2, level),
                timeout = start + 10 * (long)Math.Pow(2, level),
            };
        }

        private void ResetLevel(int level)
        {
            levels[level].timeout = counter + 10 * (long)Math.Pow(2, level);
        }

        public int GetBackjump()
        {
            if (levels == null)
                levels = new List<Level>();
            // Find first non-expired level
            int i;
            for (i = 0; i < levels.Count; i++)
            {
                if (levels[i].timeout > counter)
                    break;
            }
            // Lazily add higher levels as needed
            if (levels.Count <= i)
            {
                levels.Add(CreateLevel(i));
            }
            if (i == 0)
            {
                return 1;
            }

            // Backjump to highest expired level
            var depthDelta = depth - levels[i - 1].depth;

            // Reset any expired levels
            for (var j = 0; j < i; j++)
            {
                ResetLevel(j);
            }

            return depth - levels[i - 1].depth;
        }

        private class Level
        {
            public int depth;
            public long timeout;
        }
    }
}
