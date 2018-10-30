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

        public Tile[] Tiles { get; set; }

        public int MaxLength { get; set; }

        public Point Axis { get; set; }

        public Resolution Init(TilePropagator propagator)
        {
            var topology = propagator.Topology;

            if (Axis.X + Axis.Y + Axis.Z != 1)
            {
                throw new Exception("Axis must either be (1,0,0), (0,1,0), or (0,0,1)");
            }

            if (Axis.Z == 1 && topology.Directions.Count <= 4) 
            {
                throw new Exception("Specified Z axis but topology does not have enough dimensions");
            }

            bool isPeriodic = false;

            if (Axis.X == 1)
            {
                beforeDirection = 1;
                afterDirection = 0;
                isPeriodic = topology.PeriodicX;
            }
            else if (Axis.Y == 1)
            {
                beforeDirection = 3;
                afterDirection = 2;
                isPeriodic = topology.PeriodicY;
            }
            else
            {
                beforeDirection = 5;
                afterDirection = 4;
                isPeriodic = topology.PeriodicZ;
            }

            // Supporting this would require the algorithm to run in N^2...
            if (isPeriodic)
            {
                throw new Exception("Periodicity is not supported yet");
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
            
            // lengths[i] == 1 means the coordinate has an element of Tiles placed.
            for (int i = 0; i < indices; i++) 
            {
                topology.GetCoord(i, out var x, out var y, out var z);

                lengths[i] = 0;
                for (int j = 0; j < Tiles.Length && lengths[i] == 0; j++)
                {
                    lengths[i] = propagator.IsSelected(x, y, z, Tiles[j]) ? 1 : 0;
                }
            }

            // For each coordinate calculate its consecutive length by taking 
            // the previous coordinates value+1. To support periodicity we would 
            // need a new path that calls  TryMove in both directions until it 
            // hits the same coordinate, since we can't do the fast method of 
            // summing from previous cells.
            for (int i = 0; i < indices; i++) 
            {
                if (lengths[i] == 0) 
                {
                    continue;
                }

                if (topology.TryMove(i, beforeDirection, out int beforeIndex))
                {
                    lengths[i] += lengths[beforeIndex];
                }

                if (lengths[i] >= MaxLength)
                {
                    topology.GetCoord(i, out int x, out int y, out int z);

                    // Ban the space after this string of tiles.
                    if (topology.TryMove(x, y, z, afterDirection, out int x2, out int y2, out int z2))
                    {
                        foreach (var tile in Tiles)
                        {
                            var status = propagator.Ban(x2, y2, z2, tile);
                            if (status != DeBroglie.Resolution.Undecided) 
                            {
                                return status;
                            }
                        }
                    }

                    // Backtrack and ban the space before this string of tiles.
                    if (topology.TryMove(x, y, z, beforeDirection, out int x3, out int y3, out int z3, n:lengths[i]))
                    {
                        foreach (var tile in Tiles)
                        {
                            var status = propagator.Ban(x3, y3, z3, tile);
                            if (status != DeBroglie.Resolution.Undecided) 
                            {
                                return status;
                            }
                        }
                    }
                }
            }

            return Resolution.Undecided;
        }
    }
}
