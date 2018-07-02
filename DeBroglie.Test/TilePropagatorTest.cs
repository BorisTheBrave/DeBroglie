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
            var model = new OverlappingModel<int>(a, 2, false, 8);

            var propagator = new TilePropagator<int>(model, 4, 4, false);

            propagator.Select(0, 0, 0, 1);
            var status = propagator.Run();

            Assert.AreEqual(CellStatus.Decided, status);

            var result = propagator.ToTopArray().ToArray2d();

            Assert.AreEqual(4, result.GetLength(0));
            Assert.AreEqual(4, result.GetLength(1));

            Assert.AreEqual(1, result[0, 0]);
            Assert.AreEqual(1, result[3, 3]);
        }
    }
}
