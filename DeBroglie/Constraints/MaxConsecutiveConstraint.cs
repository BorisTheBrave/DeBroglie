using DeBroglie.Topo;
using DeBroglie.Trackers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// The MaxConsecutiveConstraint checks that no more than the specified amount of tiles can be placed
    /// in a row along the given axes.
    /// </summary>
    public class MaxConsecutiveConstraint : ITileConstraint
    {
        private TilePropagatorTileSet tileSet;

        private SelectedTracker selectedTracker;

        public ISet<Tile> Tiles { get; set; }

        public int MaxCount { get; set; }

        public ISet<Axis> Axes { get; set; }

        public void Init(TilePropagator propagator)
        {
            var topology = propagator.Topology as GridTopology;
            if(topology == null ||
                topology.Directions.Type != Topo.DirectionSetType.Cartesian2d &&
                topology.Directions.Type != Topo.DirectionSetType.Cartesian3d)
            {
                // This wouldn't be that hard to fix
                throw new Exception("MaxConsecutiveConstraint only supports cartesian topologies.");
            }
            tileSet = propagator.CreateTileSet(Tiles);
            selectedTracker = propagator.CreateSelectedTracker(tileSet);
        }

        public void Check(TilePropagator propagator)
        {
            var topology = propagator.Topology.AsGridTopology();
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            if (Axes == null || Axes.Contains(Axis.X))
            {
                int y = 0, z = 0;
                var sm = new StateMachine((x) => propagator.Ban(x, y, z, tileSet), topology.PeriodicX, width, MaxCount);

                for (z = 0; z < depth; z++)
                {
                    for (y = 0; y < height; y++)
                    {
                        sm.Reset();
                        for (var x = 0; x < width; x++)
                        {
                            var index = topology.GetIndex(x, y, z);
                            if (sm.Next(x, selectedTracker.GetQuadstate(index)))
                            {
                                propagator.SetContradiction("Max consecutive constraint failed on x-axis", this);
                                return;
                            }
                        }
                        if (topology.PeriodicX)
                        {
                            for (var x = 0; x < MaxCount && x < width; x++)
                            {
                                var index = topology.GetIndex(x, y, z);
                                if (sm.Next(x, selectedTracker.GetQuadstate(index)))
                                {
                                    propagator.SetContradiction("Max consecutive constraint failed on x-axis", this);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // Same thing as XAxis, just swizzled
            if (Axes == null || Axes.Contains(Axis.Y))
            {
                int x = 0, z = 0;
                var sm = new StateMachine((y) => propagator.Ban(x, y, z, tileSet), topology.PeriodicY, height, MaxCount);

                for (z = 0; z < depth; z++)
                {
                    for (x = 0; x < width; x++)
                    {
                        sm.Reset();
                        for (var y = 0; y < height; y++)
                        {
                            var index = topology.GetIndex(x, y, z);
                            if (sm.Next(y, selectedTracker.GetQuadstate(index)))
                            {
                                propagator.SetContradiction("Max consecutive constraint failed on y-axis", this);
                                return;
                            }
                        }
                        if (topology.PeriodicY)
                        {
                            for (var y = 0; y < MaxCount && y < height; y++)
                            {
                                var index = topology.GetIndex(x, y, z);
                                if (sm.Next(y, selectedTracker.GetQuadstate(index)))
                                {
                                    propagator.SetContradiction("Max consecutive constraint failed on y-axis", this);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            // Same thing as XAxis, just swizzled
            if (Axes == null || Axes.Contains(Axis.Z))
            {
                int x = 0, y = 0;
                var sm = new StateMachine((z) => propagator.Ban(x, y, z, tileSet), topology.PeriodicZ, depth, MaxCount);

                for (y = 0; y < height; y++)
                {
                    for (x = 0; x < width; x++)
                    {
                        sm.Reset();
                        for (var z = 0; z < depth; z++)
                        {
                            var index = topology.GetIndex(x, y, z);
                            if (sm.Next(z, selectedTracker.GetQuadstate(index)))
                            {
                                propagator.SetContradiction("Max consecutive constraint failed on x-axis", this);
                                return;
                            }
                        }
                        if (topology.PeriodicZ)
                        {
                            for (var z = 0; z < MaxCount && z < depth; z++)
                            {
                                var index = topology.GetIndex(x, y, z);
                                if (sm.Next(z, selectedTracker.GetQuadstate(index)))
                                {
                                    propagator.SetContradiction("Max consecutive constraint failed on x-axis", this);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        // This class is a bit fiddly, but esentially it looks at at every tile
        // along an axis on-line, and tracks enough information to emit bans stopping the constraint
        // from being violated. It also returns false if the constraint is already violated.
        // There's two cases to consider:
        // 1) A run of contiguous selected tiles of length max. 
        //    Then we want to ban the tiles at either end.
        // 2) Two runs of selected with a total length of at least max-1, separated by a single tile. 
        //    Then we want to ban the center tile.
        // For periodic topologies after running over an axis, the first max tiles need a second iteration
        // to cover all looping cases.
        internal struct StateMachine
        {
            private readonly Action<int> banAt;
            private bool periodic;
            private readonly int indexCount;
            private int max;
            private State state;
            private int runCount;
            private int runStartIndex;
            private int prevRunCount;

            public StateMachine(Action<int> banAt, bool periodic, int indexCount, int max)
            {
                this.banAt = banAt;
                this.periodic = periodic;
                this.indexCount = indexCount;
                this.max = max;
                state = State.Initial;
                runCount = 0;
                runStartIndex = 0;
                prevRunCount = 0;
            }

            public void Reset()
            {
                state = State.Initial;
                runCount = 0;
                runStartIndex = 0;
                prevRunCount = 0;
            }

            public bool Next(int index, Quadstate selected)
            {
                switch (state)
                {
                    case State.Initial:
                        if (selected.IsYes())
                        {
                            state = State.InRun;
                            runCount = 1;
                            runStartIndex = index;
                        }
                        return false;
                    case State.JustAfterRun:
                        if (selected.IsYes())
                        {
                            state = State.InRun;
                            runCount = 1;
                            runStartIndex = index;
                            goto checkCases;
                        }
                        else
                        {
                            state = State.Initial;
                            prevRunCount = 0;
                            runCount = 0;
                        }
                        return false;
                    case State.InRun:
                        if(selected.IsYes())
                        {
                            state = State.InRun;
                            runCount += 1;
                            if(runCount > max)
                            {
                                // Immediate contradiction
                                return true;
                            }
                            goto checkCases;
                        }
                        else
                        {
                            // Also case 1.
                            if (runCount == max)
                            {
                                if (selected.Possible())
                                {
                                    banAt(index);
                                }
                            }
                            state = State.JustAfterRun;
                            prevRunCount = runCount;
                            runCount = 0;
                        }
                        return false;
                }
                // Unreachable
                throw new Exception("Unreachable");
                checkCases:
                    // Have we entered case 1 or 2?
                    if (prevRunCount + runCount == max)
                    {
                        // Ban on the previous end of the run
                        if (runStartIndex == 0)
                        {
                            if (periodic)
                            {
                                banAt(indexCount - 1);
                            }
                        }
                        else
                        {
                            banAt(runStartIndex - 1);
                        }
                    }
                return false;
            }

            enum State
            {
                Initial,
                InRun,
                JustAfterRun,
            }
        }
    }
}
