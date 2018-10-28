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

            var propagator = new TilePropagator(model, new Topology(4, 4, false));

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
            var model = OverlappingModel.Create(a, 2, false, 8);

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
            var topology = new Topology(5, 5, true).WithMask(mask);

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

            var mask = new bool[5 * 5];
            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
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
            var topology = new Topology(5, 5, false).WithMask(mask);

            var propagator = new TilePropagator(model, topology);

            propagator.Select(0, 0, 0, new Tile(1));
            propagator.Select(4, 0, 0, new Tile(0));

            propagator.Run();

            Assert.AreEqual(Resolution.Decided, propagator.Status);
        }



        [Test]
        public void TestMaskWithWideOverlapping()
        {

            var a = new int[,]{
                { 1, 0, 1, 0 },
                { 0, 1, 0, 1 },
                { 1, 0, 1, 0 },
            };
            var model = OverlappingModel.Create(a, 3, false, 8);

            var mask = new bool[6 * 5];
            for (var x = 0; x < 6; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    if (x == 2)
                    {
                        mask[x + y * 6] = false;
                    }
                    else
                    {
                        mask[x + y * 6] = true;
                    }
                }
            }
            var topology = new Topology(6, 5, false).WithMask(mask);

            var propagator = new TilePropagator(model, topology);

            propagator.Select(0, 0, 0, new Tile(1));
            propagator.Select(4, 0, 0, new Tile(0));

            propagator.Run();

            var v = propagator.ToValueArray(-1, -2);

            Assert.AreEqual(Resolution.Contradiction, propagator.Status);
        }

    }
}
