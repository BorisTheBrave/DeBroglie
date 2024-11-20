﻿using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace DeBroglie.Test.Constraints
{
    [TestFixture]
    public class BorderConstraintTest
    {
        [Test]
        public void TestBorderConstraint()
        {
            var a = new int[,]{
                 {1, 0, 0},
                 {0, 1, 1},
                 {0, 1, 1},
            };

            var model = AdjacentModel.Create(a, true);
            var propagator = new TilePropagator(model, new GridTopology(10, 10, false), true, constraints: new[] {
                new BorderConstraint{
                    Tiles = new [] { new Tile(0) },
                }
            });
            var status = propagator.Run();
            ClassicAssert.AreEqual(Resolution.Decided, status);
            var result = propagator.ToValueArray<int>().ToArray2d();
            ClassicAssert.AreEqual(0, result[0, 0]);
            ClassicAssert.AreEqual(0, result[9, 0]);
            ClassicAssert.AreEqual(0, result[0, 9]);
            ClassicAssert.AreEqual(0, result[9, 9]);

        }
    }
}
