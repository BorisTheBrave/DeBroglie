using DeBroglie.Models;
using DeBroglie.Topo;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Test
{
    [TestFixture]
    public class TilePropagatorTest
    {
        [Test]
        public void TestToTopArray()
        {
            var a = new int[,]{
                { 1, 0 },
                { 0, 1 },
            };
            var model = OverlappingModel.Create(a, 2, false, 8);

            var propagator = new TilePropagator(model, new GridTopology(4, 4, false));

            propagator.Select(0, 0, 0, new Tile(1));
            var status = propagator.Run();

            Assert.AreEqual(Resolution.Decided, status);

            var result = propagator.ToValueArray<int>().ToArray2d();

            Assert.AreEqual(4, result.GetLength(0));
            Assert.AreEqual(4, result.GetLength(1));

            Assert.AreEqual(1, result[0, 0]);
            Assert.AreEqual(1, result[3, 3]);
        }

        [Test]
        public void TestMask()
        {
            var a = new int[,]{
                { 1, 0 },
                { 0, 1 },
            };
            var model = new AdjacentModel();
            model.AddSample(TopoArray.Create(a, true).ToTiles());

            var mask = new bool[5 * 5];
            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    if (x == 2 || y == 2)
                    {
                        mask[x + y * 5] = false;
                    }
                    else
                    {
                        mask[x + y * 5] = true;
                    }
                }
            }
            var topology = new GridTopology(5, 5, true).WithMask(mask);

            var propagator = new TilePropagator(model, topology);

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);
        }

        [Test]
        public void TestMaskWithOverlapping()
        {

            var a = new int[,]{
                { 1, 0 },
                { 0, 1 },
            };
            var model = OverlappingModel.Create(a, 2, false, 8);

            var mask = new bool[4 * 5];
            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    if (x == 2 || x == 3)
                    {
                        mask[x + y * 5] = false;
                    }
                    else
                    {
                        mask[x + y * 5] = true;
                    }
                }
            }
            var topology = new GridTopology(5, 4, false).WithMask(mask);

            var propagator = new TilePropagator(model, topology);

            propagator.Select(0, 0, 0, new Tile(1));
            propagator.Select(4, 0, 0, new Tile(0));

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);
        }

        // This test illustrates a problem with how masks interact with the overlapping model.
        // The two select calls are not possible to fulfill with one pattern across the entire
        // output.
        // But cut the output region in two using a mask, and the overlap rectangle is 2x2 and so
        // not wide enough to cause interactions across the divide. So this should give Resolution.Decided,
        // Filling each half of the output with a chess pattern.
        //
        // But at the moment, it gives contradiction. The implementation doesn't handle masks properly,
        // and errs on the side of caution, basically ignoring the mask entirely.
        //
        // I hope to resolve this with https://github.com/BorisTheBrave/DeBroglie/issues/7
        [Test]
        [Ignore("Overlapping masks don't work ideally at the moment")]
        public void TestMaskWithThinOverlapping()
        {

            var a = new int[,]{
                { 1, 0 },
                { 0, 1 },
            };
            var model = OverlappingModel.Create(a, 2, false, 8);

            var mask = new bool[4 * 5];
            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 4; y++)
                {
                    if (x == 2)
                    {
                        mask[x + y * 5] = false;
                    }
                    else
                    {
                        mask[x + y * 5] = true;
                    }
                }
            }
            var topology = new GridTopology(5, 4, false).WithMask(mask);

            var propagator = new TilePropagator(model, topology);

            propagator.Select(0, 0, 0, new Tile(1));
            propagator.Select(4, 0, 0, new Tile(0));

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);
        }

        [Test]
        public void TestBannedSelected()
        {
            var a = new int[,]{
                { 1, 2, 3 },
                { 3, 1, 2 },
                { 2, 3, 1 },
            };
            var model = AdjacentModel.Create(a, true);
            var topology = new GridTopology(10, 10, false);
            var propagator = new TilePropagator(model, topology);

            var tile1 = new Tile(1);
            var set1 = propagator.CreateTileSet(new[] { new Tile(1) });
            var set12 = propagator.CreateTileSet(new[] { new Tile(1), new Tile(2) });

            {
                propagator.GetBannedSelected(0, 0, 0, tile1, out var isBanned1, out var isSelected1);
                propagator.GetBannedSelected(0, 0, 0, set1, out var isBanned2, out var isSelected2);
                propagator.GetBannedSelected(0, 0, 0, set12, out var isBanned3, out var isSelected3);

                Assert.AreEqual(false, isBanned1);
                Assert.AreEqual(false, isBanned2);
                Assert.AreEqual(false, isBanned3);
                Assert.AreEqual(false, isSelected1);
                Assert.AreEqual(false, isSelected2);
                Assert.AreEqual(false, isSelected3);
            }

            propagator.Ban(0, 0, 0, new Tile(3));

            {
                propagator.GetBannedSelected(0, 0, 0, tile1, out var isBanned1, out var isSelected1);
                propagator.GetBannedSelected(0, 0, 0, set1, out var isBanned2, out var isSelected2);
                propagator.GetBannedSelected(0, 0, 0, set12, out var isBanned3, out var isSelected3);

                Assert.AreEqual(false, isBanned1);
                Assert.AreEqual(false, isBanned2);
                Assert.AreEqual(false, isBanned3);
                Assert.AreEqual(false, isSelected1);
                Assert.AreEqual(false, isSelected2);
                Assert.AreEqual(true, isSelected3);
            }

            propagator.Ban(0, 0, 0, new Tile(1));

            {
                propagator.GetBannedSelected(0, 0, 0, tile1, out var isBanned1, out var isSelected1);
                propagator.GetBannedSelected(0, 0, 0, set1, out var isBanned2, out var isSelected2);
                propagator.GetBannedSelected(0, 0, 0, set12, out var isBanned3, out var isSelected3);

                Assert.AreEqual(true, isBanned1);
                Assert.AreEqual(true, isBanned2);
                Assert.AreEqual(false, isBanned3);
                Assert.AreEqual(false, isSelected1);
                Assert.AreEqual(false, isSelected2);
                Assert.AreEqual(true, isSelected3);
            }
        }

        [Test]
        public void TestPriority()
        {
            var t1 = new Tile(1);
            var t2 = new Tile(2);
            var t3 = new Tile(3);
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            model.AddAdjacency(t1, t1, Direction.XPlus);
            model.AddAdjacency(t1, t2, Direction.XPlus);
            model.AddAdjacency(t2, t2, Direction.XPlus);
            model.AddAdjacency(t2, t3, Direction.XPlus);
            model.AddAdjacency(t3, t3, Direction.XPlus);

            model.SetUniformFrequency();

            var topology = new GridTopology(6, 1, false).WithMask(new bool[] { true, true, true, true, true, false });

            IDictionary<Tile, PriorityAndWeight> weights = new Dictionary<Tile, PriorityAndWeight>
            {
                {t1, new PriorityAndWeight{Priority=0, Weight = 1} },
                {t2, new PriorityAndWeight{Priority=1, Weight = 1} },
                {t3, new PriorityAndWeight{Priority=2, Weight = 1} },
            };

            var weightsArray = TopoArray.CreateByIndex(_ => weights, topology);

            var propagator = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                IndexPickerType = IndexPickerType.ArrayPriorityMinEntropy,
                Weights = weightsArray,
            });

            propagator.Select(0, 0, 0, t1);

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);

            var r = propagator.ToValueArray<int>();
            Assert.AreEqual(1, r.Get(0, 0));
            Assert.AreEqual(2, r.Get(1, 0));
            Assert.AreEqual(3, r.Get(2, 0));
            Assert.AreEqual(3, r.Get(3, 0));

        }

    }
}
