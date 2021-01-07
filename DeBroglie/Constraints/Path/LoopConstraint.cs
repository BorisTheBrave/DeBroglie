using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Constraints
{
    public class LoopConstraint : ITileConstraint
    {
        private IPathView pathView;

        public IPathSpec PathSpec { get; set; }

        public void Init(TilePropagator propagator)
        {
            if (PathSpec is PathSpec pathSpec)
            {
                // Convert PathSpec to EdgedPathSpec
                // As we have a bug with PathSpec ignoring paths of length 2.
                ISet<Direction>  allDirections = new HashSet<Direction>(Enumerable.Range(0, propagator.Topology.DirectionsCount).Cast<Direction>());
                var edgedPathSpec = new EdgedPathSpec
                {
                    Exits = pathSpec.Tiles.ToDictionary(x => x, _ => allDirections),
                    EndPoints = pathSpec.EndPoints,
                    EndPointTiles = pathSpec.EndPointTiles,
                    TileRotation = pathSpec.TileRotation,
                };
                pathView = edgedPathSpec.MakeView(propagator);
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

            var isArticulation = PathConstraintUtils.GetArticulationPoints(pathView.Graph, pathView.CouldBePath, pathView.MustBeRelevant);

            for(var i=0;i<pathView.Graph.NodeCount;i++)
            {
                if(isArticulation[i])
                {
                    propagator.SetContradiction();
                    return;
                }
            }

        }
    }
}
