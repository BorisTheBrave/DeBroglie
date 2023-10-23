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
    public class ConnectedConstraintTest
    {
        [Test]
        public void TestConnectedConstraint()
        {
            var a = new int[,]{
                 {0, 0, 0, 1, 0, 0},
                 {0, 0, 0, 1, 0, 0},
                 {0, 0, 0, 1, 0, 0},
                 {1, 1, 1, 1, 0, 0},
                 {0, 0, 0, 0, 0, 0},
                 {0, 0, 0, 0, 0, 0}
            };

            var seed = Environment.TickCount;
            var r = new Random(seed);
            System.Console.WriteLine("Seed {0}", seed);

            var model = OverlappingModel.Create(a, 3, false, 8);

            var constraint = new ConnectedConstraint
            {
                PathSpec = new PathSpec
                {
                    Tiles = new HashSet<Tile> { new Tile(1) },
                    RelevantCells = new[] { new Point(0, 0), new Point(9, 9) },
                }
            };

            var propagator = new TilePropagator(model, new GridTopology(10, 10, false), new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { constraint },
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
                    System.Console.Write(result[x, y]);
                }
                System.Console.WriteLine();
            }
            // Simple flood fill algorithm to determine we have in fact got a path
            var stack = new Stack<ValueTuple<int, int>>();
            var visited = new bool[10, 10];
            stack.Push((0, 0));
            while(stack.TryPop(out var current))
            {
                var (x, y) = current;
                if (x < 0 || x >= 10 || y < 0 || y >= 10)
                    continue;
                if (visited[x, y])
                    continue;
                visited[x, y] = true;
                if(result[x, y] == 1)
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

        // Near identical to above, case should work the same
        [Test]
        public void TestConnectedConstraintWithEdged()
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
            System.Console.WriteLine("Seed {0}", seed);

            var model = OverlappingModel.Create(a, 3, false, 8);

            var constraint = new ConnectedConstraint
            {
                PathSpec = new EdgedPathSpec
                {
                    Exits = exits,
                    RelevantCells = new[] { new Point(0, 0), new Point(9, 9) },
                }
            };

            var propagator = new TilePropagator(model, new GridTopology(10, 10, false), new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { constraint },
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
                    System.Console.Write(result[x, y]);
                }
                System.Console.WriteLine();
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

            var pathConstraint = new ConnectedConstraint
            {
                PathSpec = new EdgedPathSpec { Exits = exits },
                UsePickHeuristic = true
            };

            var propagator = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
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

            var seed = Environment.TickCount;
            var r = new Random(seed);
            System.Console.WriteLine("Seed {0}", seed);

            var constraint = new ConnectedConstraint
            {
                PathSpec = new EdgedPathSpec
                {
                    Exits = new Dictionary<Tile, ISet<Direction>>()
                    {
                       { new Tile(1), new[]{ up, down }.ToHashSet() },
                       { new Tile(2), new[]{ up, down }.ToHashSet() },
                    }
                }
            };

            var propagator = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                RandomDouble = r.NextDouble,
                Constraints = new[] { constraint }
            });

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

            var constraint = new ConnectedConstraint
            {
                PathSpec = new EdgedPathSpec
                {
                    Exits = new Dictionary<Tile, ISet<Direction>>()
                    {
                       { new Tile(1), new[]{ left, right }.ToHashSet() },
                       { new Tile(2), new[]{ left, right }.ToHashSet() },
                    }
                }
            };
                

            var propagator = new TilePropagator(model, topology, constraints: new[] { constraint });

            propagator.Run();
        }

        [Test]
        public void TestConnectedConstraintWithPairwiseConstraint()
        {
            var allDirections = DirectionSet.Cartesian2d.ToHashSet();

            var seed = Environment.TickCount;
            var r = new Random(seed);
            System.Console.WriteLine("Seed {0}", seed);

            var allTiles = new[] { new Tile(1), new Tile(2), new Tile(3), new Tile(4) };

            // Model is completely unconstrainted
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            model.AddAdjacency(allTiles, allTiles, Direction.XPlus);
            model.AddAdjacency(allTiles, allTiles, Direction.YPlus);
            model.SetUniformFrequency();
            model.SetFrequency(allTiles[0], 1000);
            model.SetFrequency(allTiles.Last(), 1000);


            var constraint = new ConnectedConstraint
            {
                PathSpec = new PairwisePathSpec
                {
                    Pairs = Enumerable.Range(1, 4)
                    .SelectMany(x => new[] { (x, x - 1), (x, x), (x, x + 1) })
                    .Where(t => 1 <= t.Item2 && t.Item2 <= 4)
                    .Select(t => (new Tile(t.Item1), new Tile(t.Item2)))
                    .ToArray(),
                }
            };

            var width = 5;
            var height = 5;

            var propagator = new TilePropagator(model, new GridTopology(width, height, false), new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { constraint },
                RandomDouble = r.NextDouble
            });
            propagator.Select(0, 0, 0, allTiles.First());
            propagator.Select(width - 1, height - 1, 0, allTiles.Last());
            var status = propagator.Run();
            Assert.AreEqual(Resolution.Decided, status);
            var result = propagator.ToValueArray<int>().ToArray2d();
            // Write out result for debugging
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    System.Console.Write(result[x, y]);
                }
                System.Console.WriteLine();
            }

            // Simple flood fill algorithm to determine we have in fact got a path
            var stack = new Stack<(int, int, int, int)>();
            var visited = new bool[width, height];
            stack.Push((0, 0, 0, 0));
            while (stack.TryPop(out var current))
            {
                var (from_x, from_y, x, y) = current;
                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;
                if (visited[x, y])
                    continue;
                if (Math.Abs(result[from_x, from_y] - result[x, y]) > 1)
                    continue;
                visited[x, y] = true;
                {
                    if (x == width - 1 && y == height - 1)
                        return;
                    stack.Push((x, y, x + 1, y));
                    stack.Push((x, y, x - 1, y));
                    stack.Push((x, y, x, y + 1));
                    stack.Push((x, y, x, y - 1));
                }
            }
            Assert.Fail();
        }
    }
}
