using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Test.Constraints
{

    [TestFixture]
    public class ParityConstraintTest
    {
        [Test]
        public void TestParityConstraint()
        {
            var w = 10;
            var h = 10;
            var topology = new GridTopology(10, 10, false);

            var pathModel = new PathModel(forks: false);

            var constraint = new ParityConstraint
            {
                PathSpec = new EdgedPathSpec { Exits = pathModel.Exits },
            };

            var options = new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { constraint },
            };

            var propagator = new TilePropagator(pathModel.Model, topology, options);

            for(var x=0;x<w;x++)
            {
                for (var y = 0; y < h; y++)
                {
                    void Select(Tile t) => propagator.Select(x, y, 0, t);
                    if(x == 0 && y == 1)
                    {
                        Select(pathModel.Straight2);
                        continue;
                    }
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    {
                        Select(pathModel.Empty);
                    }
                }
            }

            propagator.Step();

            ClassicAssert.AreEqual(Resolution.Contradiction, propagator.Status);
        }
    }
}
