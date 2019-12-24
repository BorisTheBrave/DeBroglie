using DeBroglie.Models;
using DeBroglie.Topo;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DeBroglie.Constraints;

namespace DeBroglie.Test.Constraints
{
    [TestFixture]
    class CountConstraintTest
    {
        [Test]
        [TestCase(CountComparison.AtMost, true)]
        [TestCase(CountComparison.AtMost, false)]
        [TestCase(CountComparison.AtLeast, true)]
        [TestCase(CountComparison.AtLeast, false)]
        [TestCase(CountComparison.Exactly, true)]
        [TestCase(CountComparison.Exactly, false)]
        public void TestCountConstraint(CountComparison comparison, bool eager)
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            var tiles = new[] { tile1, tile2 };
            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);
            model.SetUniformFrequency();

            var topology = new Topology(10, 10, false);

            var count = 3;

            var options = new TilePropagatorOptions
            {
                Constraints = new[]
                {
                    new CountConstraint
                    {
                        Tiles = new[]{tile1 }.ToHashSet(),
                        Count = count,
                        Comparison = comparison,
                        Eager = eager,
                    }
                }
            };
            var propagator = new TilePropagator(model, topology, options);

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);

            var actualCount = propagator.ToValueArray<int>().ToArray2d().OfType<int>().Count(x => x == 1);

            switch (comparison)
            {
                case CountComparison.AtMost:
                    Assert.LessOrEqual(actualCount, count);
                    break;
                case CountComparison.AtLeast:
                    Assert.GreaterOrEqual(actualCount, count);
                    break;
                case CountComparison.Exactly:
                    Assert.AreEqual(count, actualCount);
                    break;
            }
        }

        // In this model, the counted tiles always come in pairs, which trips up eagerness
        public void TestDoubleCountConstraint()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            var tile3 = new Tile(3);
            var tiles = new[] { tile1, tile2, tile3 };
            model.AddAdjacency(new[] { tile2 }, new[] { tile1 }, Direction.XPlus);
            model.AddAdjacency(new[] { tile1 }, new[] { tile3 }, Direction.XPlus);
            model.AddAdjacency(new[] { tile3 }, new[] { tile3 }, Direction.XPlus);
            model.AddAdjacency(new[] { tile3 }, new[] { tile2 }, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);
            model.SetUniformFrequency();

            var topology = new Topology(10, 10, false);

            var count = 10;

            var options = new TilePropagatorOptions
            {
                Constraints = new[]
                {
                    new CountConstraint
                    {
                        Tiles = new[]{ tile1, tile2 }.ToHashSet(),
                        Count = count,
                        Comparison = CountComparison.Exactly,
                        Eager = true,
                    }
                }
            };
            var propagator = new TilePropagator(model, topology, options);

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);

            var actualCount = propagator.ToValueArray<int>().ToArray2d().OfType<int>().Count(x => x == 1 || x == 2);

            Assert.AreEqual(count, actualCount);
        }
    }
}
