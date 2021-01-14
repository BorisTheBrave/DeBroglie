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
    class EdgedPathConstraintTest
    {

        // Test cloned from PathConstraintTest
        // EdgedPath should behave identically if set up correctly.
        [Test]
        public void TestEdgedPathConstraint()
        {
            var a = new int[,]{
                 {0, 0, 0, 1, 0, 0},
                 {0, 0, 0, 1, 0, 0},
                 {0, 0, 0, 1, 0, 0},
                 {1, 1, 1, 1, 0, 0},
                 {0, 0, 0, 0, 0, 0},
                 {0, 0, 0, 0, 0, 0}
            };

            var allDirections = DirectionSet.Cartesian2d.ToHashSet();

            var exits = new Dictionary<Tile, ISet<Direction>>()
            {
                { new Tile(1), allDirections },
            };

            var seed = Environment.TickCount;
            var r = new Random(seed);
            Console.WriteLine("Seed {0}", seed);

            var model = OverlappingModel.Create(a, 3, false, 8);
            var propagator = new TilePropagator(model, new GridTopology(10, 10, false), new TilePropagatorOptions
            {
                BackTrackDepth = -1,
                Constraints = new[] {
#pragma warning disable CS0618 // Type or member is obsolete
                    new EdgedPathConstraint(exits, new []{new Point(0,0), new Point(9, 9) })
#pragma warning restore CS0618 // Type or member is obsolete
                },
                RandomDouble = r.NextDouble
            });
            var status = propagator.Run();
            Assert.AreEqual(Resolution.Decided, status);
            var result = propagator.ToValueArray<int>().ToArray2d();
            // Write out result for debugging
            for (var y = 0; y < 10; y++)
            {
                for (var x = 0; x < 10; x++)
                {
                    Console.Write(result[x, y]);
                }
                Console.WriteLine();
            }
            // Simple flood fill algorithm to determine we have in fact got a path
            var stack = new Stack<ValueTuple<int, int>>();
            var visited = new bool[10, 10];
            stack.Push((0, 0));
            while (stack.TryPop(out var current))
            {
                var (x, y) = current;
                if (x < 0 || x >= 10 || y < 0 || y >= 10)
                    continue;
                if (visited[x, y])
                    continue;
                visited[x, y] = true;
                if (result[x, y] == 1)
                {
                    if (x == 9 && y == 9)
                        return;
                    stack.Push((x + 1, y));
                    stack.Push((x - 1, y));
                    stack.Push((x, y + 1));
                    stack.Push((x, y - 1));
                }
            }
            Assert.Fail();
        }

        [Test]
        public void TestPathPickHeuristic()
        {
            var topology = new GridTopology(15, 15, false);

            var model = new AdjacentModel(DirectionSet.Cartesian2d);

            var empty = new Tile(" ");
            var straight1 = new Tile("║");
            var straight2 = new Tile("═");
            var corner1 = new Tile("╚");
            var corner2 = new Tile("╔");
            var corner3 = new Tile("╗");
            var corner4 = new Tile("╝");
            var fork1 = new Tile("╠");
            var fork2 = new Tile("╦");
            var fork3 = new Tile("╣");
            var fork4 = new Tile("╩");

            model.AddAdjacency(
                new[] { empty, straight1, corner3, corner4, fork3 },
                new[] { empty, straight1, corner1, corner2, fork1 },
                Direction.XPlus);

            model.AddAdjacency(
                new[] { straight2, corner1, corner2, fork1, fork2, fork4 },
                new[] { straight2, corner3, corner4, fork2, fork3, fork4 },
                Direction.XPlus);

            model.AddAdjacency(
                new[] { empty, straight2, corner1, corner4, fork4 },
                new[] { empty, straight2, corner2, corner3, fork2 },
                Direction.YPlus);

            model.AddAdjacency(
                new[] { straight1, corner2, corner3, fork1, fork2, fork3 },
                new[] { straight1, corner1, corner4, fork1, fork3, fork4 },
                Direction.YPlus);

            model.SetUniformFrequency();

            var exits = new Dictionary<Tile, ISet<Direction>>
            {
                {straight1, new []{Direction.YMinus, Direction.YPlus}.ToHashSet() },
                {straight2, new []{Direction.XMinus, Direction.XPlus}.ToHashSet() },
                {corner1, new []{Direction.YMinus, Direction.XPlus}.ToHashSet() },
                {corner2, new []{Direction.YPlus, Direction.XPlus}.ToHashSet() },
                {corner3, new []{Direction.YPlus, Direction.XMinus}.ToHashSet() },
                {corner4, new []{Direction.YMinus, Direction.XMinus}.ToHashSet() },
                {fork1, new []{ Direction.YMinus, Direction.XPlus, Direction.YPlus}.ToHashSet() },
                {fork2, new []{ Direction.XPlus, Direction.YPlus, Direction.XMinus}.ToHashSet() },
                {fork3, new []{ Direction.YPlus, Direction.XMinus, Direction.YMinus}.ToHashSet() },
                {fork4, new []{ Direction.XMinus, Direction.YMinus, Direction.XPlus}.ToHashSet() },
            };

#pragma warning disable CS0618 // Type or member is obsolete
            var pathConstraint = new EdgedPathConstraint(exits)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                UsePickHeuristic = true
            };

            var propagator = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BackTrackDepth = -1,
                Constraints = new[] { pathConstraint },
            });

            propagator.Run();

            Assert.AreEqual(propagator.Status, Resolution.Decided);
        }

        [Test]
        public void TestDirectionality()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            model.AddAdjacency(new Tile(1), new Tile(2), 1, 0, 0);
            model.SetUniformFrequency();

            var topology = new GridTopology(2, 1, false);

            var up = Direction.YPlus;
            var down = Direction.YMinus;

#pragma warning disable CS0618 // Type or member is obsolete
            var edgedPathConstraint = new EdgedPathConstraint(
#pragma warning restore CS0618 // Type or member is obsolete
                new Dictionary<Tile, ISet<Direction>>()
                {
                   { new Tile(1), new[]{ up, down }.ToHashSet() },
                   { new Tile(2), new[]{ up, down }.ToHashSet() },
                }
                );

            var propagator = new TilePropagator(model, topology, constraints: new[] { edgedPathConstraint });

            propagator.Run();

            Assert.AreEqual(Resolution.Contradiction, propagator.Status);
        }

        [Test]
        public void TestDirectionality2()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            model.AddAdjacency(new Tile(1), new Tile(2), 1, 0, 0);
            model.SetUniformFrequency();

            var topology = new GridTopology(2, 1, false);

            var left = Direction.XMinus;
            var right = Direction.XPlus;

#pragma warning disable CS0618 // Type or member is obsolete
            var edgedPathConstraint = new EdgedPathConstraint(
#pragma warning restore CS0618 // Type or member is obsolete
                new Dictionary<Tile, ISet<Direction>>()
                {
                   { new Tile(1), new[]{ left, right }.ToHashSet() },
                   { new Tile(2), new[]{ left, right }.ToHashSet() },
                }
                );

            var propagator = new TilePropagator(model, topology, constraints: new[] { edgedPathConstraint });

            propagator.Run();
        }
    }
}
