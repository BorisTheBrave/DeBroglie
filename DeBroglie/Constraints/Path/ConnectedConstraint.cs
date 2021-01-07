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

            pathView.Update();
            for (var i = 0; i < pathView.Graph.NodeCount; i++)
            {
                if (pathView.MustBeRelevant[i])
                {
                    pathView.SelectPath(i);
                }
            }
        }

        public void Check(TilePropagator propagator)
        {
            pathView.Update();

            bool[] component = new bool[pathView.Graph.NodeCount];

            var isArticulation = PathConstraintUtils.GetArticulationPoints(pathView.Graph, pathView.CouldBePath, pathView.MustBeRelevant, component);

            if (isArticulation == null)
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
            if (false)
            {
                for (int i = 0; i < pathView.Graph.NodeCount; i++)
                {
                    if (!component[i])
                    {
                        pathView.BanRelevant(i);
                    }
                }
            }
        }
    }
}
