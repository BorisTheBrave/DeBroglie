using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Test.Constraints
{
    [TestFixture]
    public class PairSeparationConstraintTest
    {
        [Test]
        public void TestPairSeparationConstraint()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            var tile3 = new Tile(3);
            var tiles = new[] { tile1, tile2, tile3 };
            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);
            model.SetUniformFrequency();

            var pairSeparationConstraint = new PairSeparationConstraint
            {
                Tiles1 = new[] { tile1 }.ToHashSet(),
                Tiles2 = new[] { tile3 }.ToHashSet(),
                MinDistance = 3,
            };

            var countConstraint1 = new CountConstraint
            {
                Tiles = new[] { tile1 }.ToHashSet(),
                Count = 1,
                Comparison = CountComparison.Exactly,
            };
            var countConstraint2 = new CountConstraint
            {
                Tiles = new[] { tile3 }.ToHashSet(),
                Count = 1,
                Comparison = CountComparison.Exactly,
            };

            var topology = new GridTopology(4, 1, false);

            var options = new TilePropagatorOptions
            {
                Constraints = new ITileConstraint[] { pairSeparationConstraint, countConstraint1, countConstraint2 },
                BacktrackType = BacktrackType.Backtrack,
            };
            var propagator = new TilePropagator(model, topology, options);

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);

            var r = propagator.ToArray();

            // Only two possible solution given the constraints
            if (r.Get(0) == tile1)
            {
                Assert.AreEqual(tile1, r.Get(0));
                Assert.AreEqual(tile2, r.Get(1));
                Assert.AreEqual(tile2, r.Get(2));
                Assert.AreEqual(tile3, r.Get(3));
            }
            else
            {

                Assert.AreEqual(tile3, r.Get(0));
                Assert.AreEqual(tile2, r.Get(1));
                Assert.AreEqual(tile2, r.Get(2));
                Assert.AreEqual(tile1, r.Get(3));
            }
        }
    }
}
