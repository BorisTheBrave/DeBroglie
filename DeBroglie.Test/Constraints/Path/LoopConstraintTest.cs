﻿using DeBroglie.Constraints;
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
    public class LoopConstraintTest
    {
        [Test]
        public void TestLoopConstraint()
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
            // TODO: This seed shows that this constraint can fail occasionally
            //seed = -1847040250;
            var r = new Random(seed);
            System.Console.WriteLine("Seed {0}", seed);

            var model = OverlappingModel.Create(a, 3, false, 8);

            var constraint = new LoopConstraint
            {
                PathSpec = new PathSpec
                {
                    Tiles = new HashSet<Tile> { new Tile(1) },
                }
            };

            var topology = new GridTopology(10, 10, false);
            var propagator = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { constraint },
                RandomDouble = r.NextDouble
            });
            var status = propagator.Run();
            ClassicAssert.AreEqual(Resolution.Decided, status);
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
            // Every cell should have exactly 2 neighbours
            for (var y = 0; y < topology.Height; y++)
            {
                for (var x = 0; x < topology.Width; x++)
                {
                    if (result[x, y] == 1)
                    {
                        var n = 0;
                        if (x > 0) n += result[x - 1, y];
                        if (x < topology.Width - 1) n += result[x + 1, y];
                        if (y > 0) n += result[x, y - 1];
                        if (y < topology.Height - 1) n += result[x, y + 1];
                        ClassicAssert.AreEqual(2, n, $"At {x},{y}");
                    }
                }
            }
        }
    }
}
