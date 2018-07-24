using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Constraints
{
    public class PathConstraint : ITileConstraint
    {
        // Set of patterns that are considered on the path
        public ISet<Tile> PathTiles { get; set; }

        // Set of points that must be connected by paths.
        // If null, then PathConstraint ensures that all path cells
        // are connected.
        public Point[] EndPoints { get; set; }

        public PathConstraint(ISet<Tile> pathTiles, Point[] endPoints = null)
        {
            this.PathTiles = pathTiles;
            this.EndPoints = endPoints;
        }

        public CellStatus Init(TilePropagator propagator)
        {
            return CellStatus.Undecided;
        }

        public CellStatus Check(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            var indices = topology.Width * topology.Height * topology.Depth;
            // Initialize couldBePath and mustBePath based on wave possibilities
            var couldBePath = new bool[indices];
            var mustBePath = new bool[indices];
            for (int i = 0; i < indices; i++)
            {
                topology.GetCoord(i, out var x, out var y, out var z);
                propagator.GetBannedSelected(x, y, z, PathTiles, out var isBanned, out var isSelected);
                couldBePath[i] = !isBanned;
                mustBePath[i] = isSelected;
            }

            // Select relevant cells, i.e. those that must be connected.
            bool[] relevant;
            if (EndPoints == null)
            {
                relevant = mustBePath;
            }
            else
            {
                relevant = new bool[indices];
                if (EndPoints.Length == 0)
                    return CellStatus.Undecided;
                foreach (var endPoint in EndPoints)
                {
                    var index = topology.GetIndex(endPoint.X, endPoint.Y, endPoint.Z);
                    relevant[index] = true;
                }
            }
            var walkable = couldBePath;

            var isArticulation = PathConstraintUtils.GetArticulationPoints(topology, walkable, relevant);

            if (isArticulation == null)
            {
                return CellStatus.Contradiction;
            }


            // All articulation points must be paths,
            // So ban any other possibilities
            for (var i = 0; i < indices; i++)
            {
                if (!isArticulation[i])
                {
                    continue;
                }
                foreach(var tile in propagator.TileModel.TilesToPatterns.Select(x=>x.Key))
                {
                    if (PathTiles.Contains(tile))
                        continue;
                    topology.GetCoord(i, out var x, out var y, out var z);
                    propagator.Ban(x, y, z, tile);
                }
            }

            return CellStatus.Undecided;
        }
    }
}
