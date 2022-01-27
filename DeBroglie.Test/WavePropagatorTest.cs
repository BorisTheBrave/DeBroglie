using DeBroglie.Topo;
using DeBroglie.Trackers;
using DeBroglie.Wfc;
using NUnit.Framework;
using System;
using System.Linq;

namespace DeBroglie.Test
{
    [TestFixture]
    public class WavePropagatorTest
    {
        public static readonly ModelConstraintAlgorithm[] Algorithms = new[]
        {
            ModelConstraintAlgorithm.Ac4,
            ModelConstraintAlgorithm.Ac3,
            ModelConstraintAlgorithm.OneStep,
        };

        [Test]
        [TestCaseSource(nameof(Algorithms))]
        public void TestChessboard(ModelConstraintAlgorithm algorithm)
        {
            var model = new PatternModel
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
            var topology = new GridTopology(width, height, true);
            var options = new WavePropagatorOptions { ModelConstraintAlgorithm = algorithm };
            var propagator = new WavePropagator(model, topology, options);
            var status = propagator.Run();
            Assert.AreEqual(Resolution.Decided, status);
            var a = propagator.ToTopoArray().ToArray2d();
            var topLeft = a[0, 0];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    Assert.IsTrue((a[x, y] == topLeft) ^ (x % 2 == 0) ^ (y % 2 == 0));
                }
            }

            // Should be impossible with an odd sized region
            topology = new GridTopology(width + 1, height + 1, true);
            propagator = new WavePropagator(model, topology, options);
            status = propagator.Run();
            Assert.AreEqual(Resolution.Contradiction, status);

            // Should be possible with an odd sized region, if we have the right mask
            var mask = new bool[(width + 1) * (height + 1)];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    mask[x + y * (width + 1)] = true;
                }
            }
            topology = new GridTopology(width + 1, height + 1, true).WithMask(mask);
            propagator = new WavePropagator(model, topology, options);
            status = propagator.Run();
            Assert.AreEqual(Resolution.Decided, status);

        }

        [Test]
        [TestCaseSource(nameof(Algorithms))]
        public void TestChessboard3d(ModelConstraintAlgorithm algorithm)
        {
            var model = new PatternModel
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
            var topology = new GridTopology(width, height, depth, true);
            var options = new WavePropagatorOptions { ModelConstraintAlgorithm = algorithm };
            var propagator = new WavePropagator(model, topology, options);
            var status = propagator.Run();
            Assert.AreEqual(Resolution.Decided, status);
            var a = propagator.ToTopoArray();
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
            topology = new GridTopology(width + 1, height + 1, depth + 1, true);
            propagator = new WavePropagator(model, topology, options);
            status = propagator.Run();
            Assert.AreEqual(Resolution.Contradiction, status);
        }


        // Mark a contradiciton if pattern 1 is ever banned.
        // This forces backtracking, as the constraint doesn't 
        // give out any useful information.
        class DontBanOneConstraint : IWaveConstraint
        {
            public void Check(WavePropagator wavePropagator)
            {
                foreach(var i in Enumerable.Range(0, wavePropagator.Wave.Indicies))
                {
                    if(wavePropagator.Wave.Get(i, 1) == false)
                    {
                        wavePropagator.SetContradiction();
                    }
                }
            }

            public void Init(WavePropagator wavePropagator)
            {
                Check(wavePropagator);
            }
        }

        public class CustomIndexPicker : IIndexPicker
        {
            public int Count { get; set; }
            void IIndexPicker.Init(WavePropagator wavePropagator)
            {
            }

            public int GetRandomIndex(Func<double> randomDouble)
            {
                return Count++;
            }

        }


        [Test]
        [TestCaseSource(nameof(Algorithms))]
        public void TestBacktracking(ModelConstraintAlgorithm algorithm)
        {
            TestBacktracking(algorithm == ModelConstraintAlgorithm.OneStep ? 10 : 20, algorithm, new ConstantBacktrackPolicy(1));
        }

        [Test]
        public void TestBackjumping()
        {
            TestBacktracking(20, ModelConstraintAlgorithm.Ac4, new ConstantBacktrackPolicy(2));
        }

        [Test]
        public void TestPatienceBackjumping()
        {
            TestBacktracking(20, ModelConstraintAlgorithm.Ac4, new PatienceBackjumpPolicy());
        }

        public void TestBacktracking(int size, ModelConstraintAlgorithm algorithm, IBacktrackPolicy backtrackPolicy)
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

            var model = new PatternModel
            {
                Frequencies = tileBorders.Select(x=>1.0).ToArray(),
                Propagator = propagator,
            };
            var topology = new GridTopology(size, size, false);

            var seed = Environment.TickCount;
            var r = new Random(seed);
            System.Console.WriteLine("Seed {0}", seed);

            var options = new WavePropagatorOptions
            {
                BacktrackPolicy = backtrackPolicy,
                RandomDouble = r.NextDouble,
                ModelConstraintAlgorithm = algorithm,
            };
            var wavePropagator = new WavePropagator(model, topology, options);

            var status = wavePropagator.Run();

            Assert.AreEqual(Resolution.Decided, status);

            System.Console.WriteLine($"Backtrack Count {wavePropagator.BacktrackCount}");
            System.Console.WriteLine($"Backjump Count {wavePropagator.BackjumpCount}");
        }

        [Test]
        public void TestMemoizeIndices()
        {
            var model = new PatternModel
            {
                Frequencies = new double[] { 1, 1 },
                // Free model
                Propagator = new int[][][]
                {
                    new int[][]{ new int[] { 0, 1 }, new int[] { 0, 1 }, new int[] { 0, 1 }, new int[] { 0, 1 }, },
                    new int[][]{ new int[] { 0, 1 }, new int[] { 0, 1 }, new int[] { 0, 1 }, new int[] { 0, 1 }, },
                }
            };
            var width = 10;
            var height = 10;
            var topology = new GridTopology(width, height, true);
            var indexPicker = new CustomIndexPicker();
            var options = new WavePropagatorOptions { 
                MemoizeIndices = true,
                BacktrackPolicy = new ConstantBacktrackPolicy(1),
                IndexPicker = indexPicker,
                PatternPicker = new SimpleOrderedPatternPicker(),
                Constraints = new[] {new  DontBanOneConstraint()},
            };
            var propagator = new WavePropagator(model, topology, options);

            // Attempts to pick pattern 0 at index 0, should contradict and backtrack
            var status = propagator.Step();
            Assert.AreEqual(Resolution.Undecided, status);
            Assert.AreEqual(1, propagator.BacktrackCount);
            CollectionAssert.AreEqual(propagator.GetPossiblePatterns(0), new[] { 1 });
            Assert.AreEqual(1, indexPicker.Count);
            // Should re-attempt index zero, with no effect.
            propagator.Step();
            Assert.AreEqual(Resolution.Undecided, status);
            Assert.AreEqual(1, propagator.BacktrackCount);
            CollectionAssert.AreEqual(propagator.GetPossiblePatterns(0), new[] { 1 });
            Assert.AreEqual(1, indexPicker.Count);
            // Attempts to pick pattern 0 at index 1, should contradict and backtrack
            propagator.Step();
            Assert.AreEqual(Resolution.Undecided, status);
            Assert.AreEqual(2, propagator.BacktrackCount);
            CollectionAssert.AreEqual(propagator.GetPossiblePatterns(1), new[] { 1 });
            Assert.AreEqual(2, indexPicker.Count);
            // etc

        }
    }
}
