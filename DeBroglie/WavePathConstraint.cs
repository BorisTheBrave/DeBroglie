using DeBroglie.Constraints;
using System;
using System.Linq;

namespace DeBroglie
{

    [Obsolete]
    public class WavePathConstraint : IWaveConstraint
    {
        // Set of patterns that are considered on the path
        public int[] PathPatterns { get; set; }

        // Set of points that must be connected by paths.
        // If null, then PathConstraint ensures that all path cells
        // are connected.
        public Point[] EndPoints { get; set; }

        public WavePathConstraint(int[] pathPatterns, Point[] endPoints = null)
        {
            this.PathPatterns = pathPatterns;
            this.EndPoints = endPoints;
        }

        public CellStatus Init(WavePropagator wp)
        {
            return Check(wp);
        }


        public CellStatus Check(WavePropagator wp)
        {
            var wave = wp.Wave;
            var indices = wp.Indices;
            // Initialize couldBePath and mustBePath based on wave possibilities
            var couldBePath = new bool[indices];
            var mustBePath = new bool[indices];
            for (int i = 0; i < indices; i++)
            {
                var couldBe = false;
                var mustBe = true;
                for (int p = 0; p < wp.PatternCount; p++)
                {
                    if (wave.Get(i, p))
                    {
                        if (PathPatterns.Contains(p))
                            couldBe = true;
                        if (!PathPatterns.Contains(p))
                            mustBe = false;
                    }
                }
                couldBePath[i] = couldBe;
                mustBePath[i] = mustBe;
            }

            // Select relevant cells, i.e. those that must be connected.
            bool[] relevant;
            if(EndPoints == null)
            {
                relevant = mustBePath;
            }
            else
            {
                relevant = new bool[indices];
                if (EndPoints.Length == 0)
                    return CellStatus.Undecided;
                foreach(var endPoint in EndPoints)
                {
                    var index = wp.Topology.GetIndex(endPoint.X, endPoint.Y, endPoint.Z);
                    relevant[index] = true;
                }
            }
            var walkable = couldBePath;

            var isArticulation = PathConstraintUtils.GetArticulationPoints(wp.Topology, walkable, relevant);

            if(isArticulation == null)
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
                for (int p = 0; p < wp.PatternCount; p++)
                {
                    if (!PathPatterns.Contains(p) && wave.Get(i, p))
                    {
                        wp.InternalBan(i, p);
                    }
                }
            }

            return CellStatus.Undecided;
        }

        public static WavePathConstraint Create(TileModel overlappingModel, Tile[] pathTiles, Point[] endPoints)
        {
            var pathPatterns = pathTiles
                .SelectMany(t => overlappingModel.TilesToPatterns[t])
                .ToArray();
            return new WavePathConstraint(pathPatterns, endPoints);
        }
    }
}
