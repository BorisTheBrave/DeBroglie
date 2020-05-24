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


        [Test]
        public void TestLargeSeparationConstraint()
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
                MinDistance = 10,
            };

            var topology = new GridTopology(100, 100, false);

            var options = new TilePropagatorOptions
            {
                Constraints = new ITileConstraint[] { separationConstraint },
                BackTrackDepth = -1,
            };
            var propagator = new TilePropagator(model, topology, options);

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);

            var r = propagator.ToArray();

            for(var x=0;x<100;x++)
            {
                for(var y =0;y<100;y++)
                {
                    if (r.Get(x, y) != tile1)
                        continue;
                    for(var dx = -1;dx<=1;dx+=2)
                    {
                        for (var dy = -1; dy <= 1; dy += 2)
                        {
                            var x2 = x + dx;
                            var y2 = y + dy;
                            if(x2 >= 0 && x2 < 100 && y2 >= 0 && y2 < 100)
                            {
                                Assert.AreNotEqual(r.Get(x2, y2), tile1);
                            }
                        }
                    }
                }
            }
        }
    }
}
