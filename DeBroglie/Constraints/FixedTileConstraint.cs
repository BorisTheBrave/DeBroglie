using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Constraints
{
    public class FixedTileConstraint : ITileConstraint
    {
        public Tile[] Tiles { get; set; }

        public Point? Point { get; set; }

        public void Check(TilePropagator propagator)
        {
        }

        public void Init(TilePropagator propagator)
        {
            var tileSet = propagator.CreateTileSet(Tiles);

            var point = Point ?? GetRandomPoint(propagator, tileSet);

            propagator.Select(point.X, point.Y, point.Z, tileSet);
        }

        public Point GetRandomPoint(TilePropagator propagator, TilePropagatorTileSet tileSet)
        {
            var topology = propagator.Topology;

            var points = new List<Point>();
            for (var z = 0; z < topology.Depth; z++)
            {
                for (var y = 0; y < topology.Height; y++)
                {
                    for (var x = 0; x < topology.Width; x++)
                    {
                        if (topology.Mask != null)
                        {
                            var index = topology.GetIndex(x, y, z);
                            if (!topology.Mask[index])
                                continue;
                        }

                        propagator.GetBannedSelected(x, y, z, tileSet, out var isBanned, out var _);
                        if (isBanned)
                            continue;

                        points.Add(new Point(x, y, z));
                    }
                }
            }

            // Choose a random point to select
            if (points.Count == 0)
                throw new System.Exception($"No legal placement of {tileSet}");

            var i = (int)(propagator.RandomDouble() * points.Count);

            return points[i];

        }
    }
}
