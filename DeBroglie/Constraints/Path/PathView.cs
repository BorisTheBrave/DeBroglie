using DeBroglie.Trackers;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Constraints
{
    internal class PathView : IPathView
    {
        private readonly ISet<Tile> tiles;
        private readonly ISet<Tile> endPointTiles;
        private readonly TilePropagatorTileSet tileSet;
        private readonly SelectedTracker selectedTracker;

        private bool hasEndPoints;
        private readonly List<int> endPointIndices;
        private readonly TilePropagatorTileSet endPointTileSet;
        private readonly SelectedTracker endPointSelectedTracker;

        private readonly TilePropagator propagator;

        public PathView(PathSpec spec, TilePropagator propagator)
        {

            if (spec.TileRotation != null)
            {
                tiles = new HashSet<Tile>(spec.TileRotation.RotateAll(spec.Tiles));
                endPointTiles = spec.RelevantTiles == null ? null : new HashSet<Tile>(spec.TileRotation.RotateAll(spec.RelevantTiles));
            }
            else
            {
                tiles = spec.Tiles;
                endPointTiles = spec.RelevantTiles;
            }

            tileSet = propagator.CreateTileSet(tiles);
            selectedTracker = propagator.CreateSelectedTracker(tileSet);

            Graph = PathConstraintUtils.CreateGraph(propagator.Topology);
            this.propagator = propagator;

            CouldBePath = new bool[propagator.Topology.IndexCount];
            MustBePath = new bool[propagator.Topology.IndexCount];

            hasEndPoints = spec.RelevantCells != null || spec.RelevantTiles != null;

            if (hasEndPoints)
            {
                CouldBeRelevant = new bool[propagator.Topology.IndexCount];
                MustBeRelevant = new bool[propagator.Topology.IndexCount];
                endPointIndices = spec.RelevantCells == null ? null :
                    spec.RelevantCells.Select(p => propagator.Topology.GetIndex(p.X, p.Y, p.Z)).ToList();
                endPointTileSet = spec.RelevantTiles != null ? propagator.CreateTileSet(endPointTiles) : null;
                endPointSelectedTracker = spec.RelevantTiles != null ? propagator.CreateSelectedTracker(endPointTileSet) : null;
            }
            else
            {
                CouldBeRelevant = CouldBePath;
                MustBeRelevant = MustBePath;
                endPointTileSet = tileSet;
            }

        }

        public PathConstraintUtils.SimpleGraph Graph { get; }

        public bool[] CouldBePath { get; }
        public bool[] MustBePath { get; }


        public bool[] CouldBeRelevant { get; }
        public bool[] MustBeRelevant { get; }


        public void Update()
        {
            var topology = propagator.Topology;
            var indexCount = topology.IndexCount;
            for (int i = 0; i < indexCount; i++)
            {
                var ts = selectedTracker.GetQuadstate(i);
                CouldBePath[i] = ts.Possible();
                MustBePath[i] = ts.IsYes();
            }

            if (hasEndPoints)
            {
                if (endPointIndices != null)
                {
                    foreach (var index in endPointIndices)
                    {
                        CouldBeRelevant[index] = MustBeRelevant[index] = true;
                    }
                }
                if (endPointSelectedTracker != null)
                {
                    for (int i = 0; i < indexCount; i++)
                    {
                        var ts = endPointSelectedTracker.GetQuadstate(i);

                        CouldBeRelevant[i] = ts.Possible();
                        MustBeRelevant[i] = ts.IsYes();
                    }
                }
            }
        }

        public void SelectPath(int index)
        {
            propagator.Topology.GetCoord(index, out var x, out var y, out var z);
            propagator.Select(x, y, z, tileSet);
        }

        public void BanPath(int index)
        {
            propagator.Topology.GetCoord(index, out var x, out var y, out var z);
            propagator.Ban(x, y, z, tileSet);
        }

        public void BanRelevant(int index)
        {
            propagator.Topology.GetCoord(index, out var x, out var y, out var z);
            propagator.Ban(x, y, z, endPointTileSet);
        }
    }
}
