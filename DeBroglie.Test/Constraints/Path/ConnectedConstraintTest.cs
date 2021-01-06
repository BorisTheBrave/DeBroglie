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
            Console.WriteLine("Seed {0}", seed);

            var model = OverlappingModel.Create(a, 3, false, 8);

            var constraint = new ConnectedConstraint
            {
                PathSpec = new PathSpec
                {
                    Tiles = new HashSet<Tile> { new Tile(1) },
                    EndPoints = new[] { new Point(0, 0), new Point(9, 9) },
                }
            };

            var propagator = new TilePropagator(model, new GridTopology(10, 10, false), new TilePropagatorOptions
            {
                BackTrackDepth = -1,
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
                    Console.Write(result[x, y]);
                }
                Console.WriteLine();
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
            Console.WriteLine("Seed {0}", seed);

            var model = OverlappingModel.Create(a, 3, false, 8);

            var constraint = new ConnectedConstraint
            {
                PathSpec = new EdgedPathSpec
                {
                    Exits = exits,
                    EndPoints = new[] { new Point(0, 0), new Point(9, 9) },
                }
            };

            var propagator = new TilePropagator(model, new GridTopology(10, 10, false), new TilePropagatorOptions
            {
                BackTrackDepth = -1,
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
        public void TestDirectionality()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            model.AddAdjacency(new Tile(1), new Tile(2), 1, 0, 0);
            model.SetUniformFrequency();

            var topology = new GridTopology(2, 1, false);

            var up = Direction.YPlus;
            var down = Direction.YMinus;

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

            var propagator = new TilePropagator(model, topology, constraints: new[] { constraint });

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
    }
}
