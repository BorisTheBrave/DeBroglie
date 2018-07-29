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

            Assert.AreEqual(CellStatus.Decided, status);

            var result = propagator.ToValueArray<int>().ToArray2d();

            Assert.AreEqual(4, result.GetLength(0));
            Assert.AreEqual(4, result.GetLength(1));

            Assert.AreEqual(1, result[0, 0]);
            Assert.AreEqual(1, result[3, 3]);
        }
    }
}
