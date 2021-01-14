using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie.Trackers;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    public class ConnectedConstraint : ITileConstraint
    {
        private IPathView pathView;
        private bool pathViewIsFresh;

        public IPathSpec PathSpec { get; set; }

        /// <summary>
        /// If set, configures the propagator to choose tiles that lie on the path first.
        /// This can help avoid contradictions in many cases
        /// </summary>
        public bool UsePickHeuristic { get; set; }

        public void Init(TilePropagator propagator)
        {
            if (pathViewIsFresh)
            {
                pathViewIsFresh = false;
            }
            else
            {
                pathView = PathSpec.MakeView(propagator);
            }
            pathView.Init();
        }

        public void Check(TilePropagator propagator)
        {
            pathView.Update();

            var info = PathConstraintUtils.GetArticulationPoints(pathView.Graph, pathView.CouldBePath, pathView.MustBeRelevant);
            var isArticulation = info.IsArticulation;

            if (info.ComponentCount > 1)
            {
                propagator.SetContradiction();
                return;
            }

            // All articulation points must be paths,
            // So ban any other possibilities
            for (var i = 0; i < pathView.Graph.NodeCount; i++)
            {
                if (isArticulation[i] && !pathView.MustBePath[i])
                {
                    pathView.SelectPath(i);
                }
            }

            // Any path tiles / EndPointTiles not in the connected component aren't safe to add.
            // Disabled for now, unclear exactly when it is needed
            if (info.ComponentCount > 0)
            {
                var component = info.Component;
                for (int i = 0; i < pathView.Graph.NodeCount; i++)
                {
                    if (component[i] == null && pathView.CouldBeRelevant[i])
                    {
                        pathView.BanRelevant(i);
                    }
                }
            }
        }

        internal IPickHeuristic GetHeuristic(
            IRandomPicker randomPicker,
            Func<double> randomDouble,
            TilePropagator propagator,
            TileModelMapping tileModelMapping,
            IPickHeuristic fallbackHeuristic)
        {
            pathView = PathSpec.MakeView(propagator);
            pathViewIsFresh = true;
            if (pathView is EdgedPathView epv)
            {
                return new FollowPathHeuristic(
                    randomPicker, randomDouble, propagator, tileModelMapping, fallbackHeuristic, epv);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private class FollowPathHeuristic : IPickHeuristic
        {
            private readonly IRandomPicker randomPicker;

            private readonly Func<double> randomDouble;

            private readonly TilePropagator propagator;

            private readonly TileModelMapping tileModelMapping;

            private readonly IPickHeuristic fallbackHeuristic;

            private readonly EdgedPathView edgedPathView;

            public FollowPathHeuristic(
                IRandomPicker randomPicker,
                Func<double> randomDouble,
                TilePropagator propagator,
                TileModelMapping tileModelMapping,
                IPickHeuristic fallbackHeuristic,
                EdgedPathView edgedPathView)
            {
                this.randomPicker = randomPicker;
                this.randomDouble = randomDouble;
                this.propagator = propagator;
                this.tileModelMapping = tileModelMapping;
                this.fallbackHeuristic = fallbackHeuristic;
                this.edgedPathView = edgedPathView;
            }

            public void PickObservation(out int index, out int pattern)
            {
                var topology = propagator.Topology;
                var t = edgedPathView.PathSelectedTracker;
                // Find cells that could potentially be paths, and are next to 
                // already selected path. In tileSpace
                var tilePriority = topology.GetIndices().Select(i =>
                {
                    var qs = t.GetQuadstate(i);
                    if (qs.IsYes())
                    {
                        return 2;
                    }
                    if (qs.IsNo())
                    {
                        return 0;
                    }
                    // Determine if any neighbours exit onto this tile
                    for (var d = 0; d < topology.DirectionsCount; d++)
                    {
                        if (topology.TryMove(i, (Direction)d, out var i2, out var inverseDirection, out var _))
                        {
                            if (edgedPathView.TrackerByExit.TryGetValue(inverseDirection, out var tracker))
                            {
                                var s2 = tracker.GetQuadstate(i2);
                                if (s2.IsYes())
                                {
                                    return 1;
                                }
                            }
                        }
                    }
                    return 0;
                }).ToArray();

                var patternPriority = tileModelMapping.PatternCoordToTileCoordIndexAndOffset == null ? tilePriority : throw new NotImplementedException();

                index = randomPicker.GetRandomIndex(randomDouble, patternPriority);

                if (index == -1)
                {
                    fallbackHeuristic.PickObservation(out index, out pattern);
                    propagator.Topology.GetCoord(index, out var x, out var y, out var z);
                }
                else
                {
                    propagator.Topology.GetCoord(index, out var x, out var y, out var z);
                    pattern = randomPicker.GetRandomPossiblePatternAt(index, randomDouble);
                }
            }
        }

    }
}
