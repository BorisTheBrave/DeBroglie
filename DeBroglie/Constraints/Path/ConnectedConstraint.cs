using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Constraints
{
    public class ConnectedConstraint : ITileConstraint
    {
        private IPathView pathView;

        public IPathSpec PathSpec { get; set; }

        public void Init(TilePropagator propagator)
        {
            pathView = PathSpec.MakeView(propagator);
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
    }
}
