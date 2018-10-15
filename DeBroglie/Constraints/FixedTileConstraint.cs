using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Constraints
{
    public class FixedTileConstraint : ITileConstraint
    {
        public Tile Tile { get; set; }

        public int? X { get; set; }

        public int? Y { get; set; }

        public int? Z { get; set; }

        public Resolution Check(TilePropagator propagator)
        {
            return Resolution.Undecided;
        }

        public Resolution Init(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            var points = new List<Point>();
            for (var z = 0; z < topology.Depth; z++)
            {
                if (Z != null && z != Z)
                    continue;
                for (var y = 0; y < topology.Height; y++)
                {
                    if (Y != null && y != Y)
                        continue;
                    for (var x = 0; x < topology.Width; x++)
                    {
                        if (X != null && x != X)
                            continue;

                        if (topology.Mask != null)
                        {
                            var index = topology.GetIndex(x, y, z);
                            if (!topology.Mask[index])
                                continue;
                        }

                        if (propagator.IsBanned(x, y, z, Tile))
                            continue;

                        points.Add(new Point(x, y, z));
                    }
                }
            }

            // Choose a random point to select
            if (points.Count == 0)
                throw new System.Exception($"No legal placement of {Tile}");

            var i = (int)(propagator.Random.NextDouble() * points.Count);

            var point = points[i];

            propagator.Select(point.X, point.Y, point.Z, Tile);

            return Resolution.Undecided;
        }
    }
}
