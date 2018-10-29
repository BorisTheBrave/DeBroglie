using System;
using System.Collections.Generic;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Restrict how many consecutive tiles can contain the target tile on one 
    /// row of an axis. For instance, you might use this constraint to limit how 
    /// long a ladder can be in a platformer.
    /// </summary>
    public class MaxConsecutiveTilesConstraint : ITileConstraint
    {
        private int[] lengths;
        private int beforeDirection;
        private int afterDirection;

        public Tile Tile { get; set; }

        public int MaxLength { get; set; }

        public enum AxisType
        {
            X,
            Y,
            Z,
        }

        public AxisType Axis { get; set; }

        public Resolution Init(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            
            switch (Axis)
            {
                case AxisType.X:
                    beforeDirection = 1;
                    afterDirection = 0;
                    break;

                case AxisType.Y:
                    beforeDirection = 3;
                    afterDirection = 2;
                    break;

                case AxisType.Z:
                    if (topology.Directions.Count <= 4) 
                    {
                        return Resolution.Contradiction;
                    }

                    beforeDirection = 5;
                    afterDirection = 4;
                    break;
            }

            return DeBroglie.Resolution.Undecided;
        }

        public Resolution Check(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            var indices = topology.Width * topology.Height * topology.Depth;

            if (lengths == null || lengths.Length != indices) 
            {
                lengths = new int[indices];
            }
            
            for (int i = 0; i < indices; i++) 
            {
                topology.GetCoord(i, out var x, out var y, out var z);
                propagator.GetBannedSelected(x, y, z, Tile, out _, out var isSelected);

                lengths[i] = isSelected ? 1 : 0;
            }

            for (int i = 0; i < indices; i++) 
            {
                if (lengths[i] == 0) 
                {
                    continue;
                }

                if (!topology.TryMove(i, beforeDirection, out int beforeIndex))
                {
                    continue;
                }

                lengths[i] += lengths[beforeIndex];

                if (lengths[i] > MaxLength && topology.TryMove(i, afterDirection, out int afterIndex))
                {
                    topology.GetCoord(afterIndex, out int x, out int y, out int z);

                    var status = propagator.Ban(x, y, z, Tile);
                    if (status != DeBroglie.Resolution.Undecided) 
                    {
                        return status;
                    }
                }
            }

            return Resolution.Undecided;
        }
    }
}
