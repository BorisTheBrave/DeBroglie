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
                propagator.SetContradiction("Connected constraint found multiple connected components.", this);
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

        internal IIndexPicker GetHeuristic(
            IFilteredIndexPicker filteredIndexPicker,
            TilePropagator propagator)
        {
            if (PathSpec is EdgedPathSpec eps)
            {
                return new FollowPathHeuristic(
                    filteredIndexPicker, propagator, eps);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private class FollowPathHeuristic : IIndexPicker
        {
            private readonly IFilteredIndexPicker filteredIndexPicker;

            private readonly TilePropagator propagator;

            private readonly EdgedPathSpec edgedPathSpec;
            private EdgedPathView edgedPathView;

            public FollowPathHeuristic(
                IFilteredIndexPicker filteredIndexPicker,
                TilePropagator propagator,
                EdgedPathSpec edgedPathSpec)
            {
                this.filteredIndexPicker = filteredIndexPicker;
                this.propagator = propagator;
                this.edgedPathSpec = edgedPathSpec;
            }

            public void Init(WavePropagator wavePropagator)
            {
                filteredIndexPicker.Init(wavePropagator);
                // TODO: It's a pity this isn't shared with the path constraint
                edgedPathView = (EdgedPathView)edgedPathSpec.MakeView(propagator);
            }

            public int GetRandomIndex(Func<double> randomDouble)
            {
                var topology = propagator.Topology;
                var t = edgedPathView.PathSelectedTracker;
                // Find cells that could potentially be paths, and are next to 
                // already selected path. In tileSpace
                var highPriority = new List<int>();
                var mediumPrioiry = new List<int>();
                var lowPriority = new List<int>();
                foreach(var i in topology.GetIndices())
                {
                    var qs = t.GetQuadstate(i);
                    if (qs.IsYes())
                    {
                        highPriority.Add(i);
                        continue;
                    }
                    if (qs.IsNo())
                    {
                        lowPriority.Add(i);
                        continue;
                    }
                    // Determine if any neighbours exit onto this tile
                    var found = false;
                    for (var d = 0; d < topology.DirectionsCount; d++)
                    {
                        if (topology.TryMove(i, (Direction)d, out var i2, out var inverseDirection, out var _))
                        {
                            if (edgedPathView.TrackerByExit.TryGetValue(inverseDirection, out var tracker))
                            {
                                var s2 = tracker.GetQuadstate(i2);
                                if (s2.IsYes())
                                {
                                    mediumPrioiry.Add(i);
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (!found)
                    {
                        lowPriority.Add(i);
                    }
                }

                var index = filteredIndexPicker.GetRandomIndex(randomDouble, highPriority);
                if (index != -1)
                    return index;
                index = filteredIndexPicker.GetRandomIndex(randomDouble, mediumPrioiry);
                if (index != -1)
                    return index;
                index = filteredIndexPicker.GetRandomIndex(randomDouble, lowPriority);
                return index;
            }
        }
    }
}
