using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Test
{
    [TestFixture]
    public class WavePropagatorTest
    {
        [Test]
        public void TestChessboard()
        {
            var model = new Model
            {
                Frequencies = new double[] { 1, 1 },
                Propagator = new int[][][]
                {
                    new int[][]{ new int[] { 1 }, new int[] { 1 }, new int[] { 1 }, new int[] { 1 }, },
                    new int[][]{ new int[] { 0 }, new int[] { 0 }, new int[] { 0 }, new int[] { 0 }, },
                }
            };
            var width = 10;
            var height = 10;
            var topology = new Topology(Directions.Cartesian2d, width, height, true);
            var propagator = new WavePropagator(model, topology);
            var status = propagator.Run();
            Assert.AreEqual(CellStatus.Decided, status);
            var a = propagator.ToTopArray().ToArray2d();
            var topLeft = a[0, 0];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    Assert.IsTrue((a[x, y] == topLeft) ^ (x % 2 == 0) ^ (y % 2 == 0));
                }
            }

            // Should be impossible with an odd sized region
            topology = new Topology(Directions.Cartesian2d, width + 1, height + 1, true);
            propagator = new WavePropagator(model, topology);
            status = propagator.Run();
            Assert.AreEqual(CellStatus.Contradiction, status);
        }

        [Test]
        public void TestChessboard3d()
        {
            var model = new Model
            {
                Frequencies = new double[] { 1, 1 },
                Propagator = new int[][][]
                {
                    new int[][]{ new int[] { 1 }, new int[] { 1 }, new int[] { 1 }, new int[] { 1 }, new int[] { 1 }, new int[] { 1 }, },
                    new int[][]{ new int[] { 0 }, new int[] { 0 }, new int[] { 0 }, new int[] { 0 }, new int[] { 0 }, new int[] { 0 }, },
                }
            };
            var width = 4;
            var height = 4;
            var depth = 4;
            var topology = new Topology(Directions.Cartesian3d, width, height, depth, true);
            var propagator = new WavePropagator(model, topology);
            var status = propagator.Run();
            Assert.AreEqual(CellStatus.Decided, status);
            var a = propagator.ToTopArray();
            var topLeft = a.Get(0, 0, 0);
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        Assert.IsFalse((a.Get(x, y, z) == topLeft) ^ (x % 2 == 0) ^ (y % 2 == 0) ^ (z % 2 == 0));
                    }
                }
            }

            // Should be impossible with an odd sized region
            topology = new Topology(Directions.Cartesian3d, width + 1, height + 1, depth + 1, true);
            propagator = new WavePropagator(model, topology);
            status = propagator.Run();
            Assert.AreEqual(CellStatus.Contradiction, status);
        }

        [Test]
        public void TestBacktracking()
        {
            // Reproduces the wang tiles found at
            // https://en.wikipedia.org/wiki/Wang_tile
            // They only have aperiodic tiling, so they are a hard set to put down.
            // Clockwise from top
            var tileBorders = new[] 
            {
                "rrrg",
                "brbg",
                "rggg",
                "wbrb",
                "bbwb",
                "wwrw",
                "rgbw",
                "bwbr",
                "brwr",
                "ggbr",
                "rwrg",
            };
            var propagator = tileBorders.Select(tile1 =>

                tile1.Select((c, i) =>
                {
                    var d = new[] { 3, 0, 2, 1 }[i];
                    var o = (i + 2) % 4;
                    return Tuple.Create(d, tileBorders
                        .Select((tile2, i2) => Tuple.Create(tile2, i2))
                        .Where(t => t.Item1[o] == c)
                        .Select(t => t.Item2)
                        .ToArray());
                })
                .OrderBy(x => x.Item1)
                .Select(x => x.Item2)
                .ToArray()
            ).ToArray();

            var model = new Model
            {
                Frequencies = tileBorders.Select(x=>1.0).ToArray(),
                Propagator = propagator,
            };
            var topology = new Topology(Directions.Cartesian2d, 10, 10, false);

            var wavePropagator = new WavePropagator(model, topology, true);

            var status = wavePropagator.Run();

            Assert.AreEqual(CellStatus.Decided, status);

            Console.WriteLine($"Backtrack Count {wavePropagator.BacktrackCount}");
        }

        [Test]
        public void TestImpossibleConstraints()
        {
            var a = new int[,]{
                { 0, 0, 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 0, 1, 0, 0, 0 },
                { 0, 0, 0, 0, 1, 1, 1, 1 },
                { 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0 },
                { 2, 2, 2, 2, 0, 0, 0, 0 },
                { 0, 0, 0, 2, 0, 0, 0, 0 },
                { 0, 0, 0, 2, 0, 0, 0, 0 },
                { 0, 0, 0, 2, 0, 0, 0, 0 },
            };
            var model = new OverlappingModel<int>(a, 3, false, 8);

            var width = 10;
            var height = 10;

            var pathConstraint1 = PathConstraint.Create(model, new[] { 1 }, new[]
            {
                new Point(0, 0),
                new Point(width - 1, height - 1),
            });
            var pathConstraint2 = PathConstraint.Create(model, new[] { 2 }, new[]
            {
                new Point(0, height - 1),
                new Point(width - 1, 0),
            });

            var propagator = new WavePropagator(model, width, height, false, true);
            var status = propagator.Run();
            Assert.AreEqual(CellStatus.Decided, status);

            propagator = new WavePropagator(model, width, height, false, true, new[] { pathConstraint1 });
            status = propagator.Run();
            Assert.AreEqual(CellStatus.Decided, status);

            propagator = new WavePropagator(model, width, height, false, true, new[] { pathConstraint2 });
            status = propagator.Run();
            Assert.AreEqual(CellStatus.Decided, status);

            propagator = new WavePropagator(model, width, height, false, true, new[] { pathConstraint1, pathConstraint2 });
            status = propagator.Run();
            Assert.AreEqual(CellStatus.Contradiction, status);
        }
    }
}
