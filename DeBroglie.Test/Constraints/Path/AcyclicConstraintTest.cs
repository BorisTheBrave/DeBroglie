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
    public class AcyclicConstraintTest
    {
        [Test]
        public void TestAcyclicConstraint()
        {
            var a = new int[,]{
                 {0, 0, 0, 1, 0, 0},
                 {0, 0, 0, 1, 0, 0},
                 {0, 0, 0, 1, 0, 0},
                 {1, 1, 1, 1, 0, 0},
                 {0, 1, 0, 0, 0, 0},
                 {0, 1, 0, 0, 0, 0}
            };

            var seed = Environment.TickCount;
            var r = new Random(seed);
            System.Console.WriteLine("Seed {0}", seed);

            var model = OverlappingModel.Create(a, 3, false, 8);
            var topology = new GridTopology(10, 10, false);

            /*
            var pathSpec = new PathSpec
            {
                Tiles = new HashSet<Tile> { new Tile(1) },
            };
            */
            var pathSpec = new EdgedPathSpec
            {
                Exits = new Dictionary<Tile, ISet<Direction>> { { new Tile(1), topology.Directions.ToHashSet() } },
            };
            var constraint = new AcyclicConstraint
            {
                PathSpec = pathSpec,
            };

            var propagator = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BackTrackDepth = -1,
                Constraints = new[] { constraint },
                RandomDouble = r.NextDouble
            });
            var status = propagator.Run();
            Assert.AreEqual(Resolution.Decided, status);
            var result = propagator.ToValueArray<int>().ToArray2d();
            // Write out result for debugging
            for (var y = 0; y < topology.Height; y++)
            {
                for (var x = 0; x < topology.Width; x++)
                {
                    System.Console.Write(result[x, y]);
                }
                System.Console.WriteLine();
            }
            var visited = new bool[topology.Width, topology.Height];

            for (var y = 0; y < topology.Height; y++)
            {
                for (var x = 0; x < topology.Width; x++)
                {
                    if (result[x, y] != 1) continue;
                    if (visited[x, y]) continue;
                    void Visit(int x2, int y2, int dir)
                    {
                        if (x2 < 0 || x2 >= topology.Width || y2 < 0 || y2 >= topology.Height) return;
                        if (result[x2, y2] != 1) return;
                        if (visited[x2, y2]) Assert.Fail();
                        visited[x2, y2] = true;
                        if(dir != 0) Visit(x2 - 1, y2, 2);
                        if (dir != 2) Visit(x2 + 1, y2, 0);
                        if (dir != 1) Visit(x2, y2 - 1, 3);
                        if (dir != 3) Visit(x2, y2 + 1, 1);
                    }
                    Visit(x, y, -1);
                }
            }
        }
    }
}
