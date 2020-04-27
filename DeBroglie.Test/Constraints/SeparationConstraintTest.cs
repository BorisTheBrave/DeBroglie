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
    public class SeparationConstraintTest
    {
        [Test]
        public void TestSeparationConstraint()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            var tiles = new[] { tile1, tile2 };
            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);
            model.SetUniformFrequency();

            var separationConstraint = new SeparationConstraint
            {
                Tiles = new[] { tile1 }.ToHashSet(),
                MinDistance = 3,
            };

            var countConstraint = new CountConstraint
            {
                Tiles = new[] { tile1 }.ToHashSet(),
                Count = 2,
                Comparison = CountComparison.Exactly,
            };

            var topology = new GridTopology(4, 1, false);

            var options = new TilePropagatorOptions
            {
                Constraints = new ITileConstraint[] { separationConstraint, countConstraint },
                BackTrackDepth = -1,
            };
            var propagator = new TilePropagator(model, topology, options);

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);

            var r = propagator.ToArray();

            // Only possible solution given the constraints
            Assert.AreEqual(tile1, r.Get(0));
            Assert.AreEqual(tile2, r.Get(1));
            Assert.AreEqual(tile2, r.Get(2));
            Assert.AreEqual(tile1, r.Get(3));


        }
    }
}
