using DeBroglie.Topo;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Test
{
    [TestFixture]
    class TopoArrayUtilsTest
    {
        [Test]
        public void TestRotate()
        {
            var a = new int[2, 2];
            a[0, 0] = 1; a[1, 0] = 2;
            a[0, 1] = 3; a[1, 1] = 4;

            var ta = TopoArray.Create(a, new Topology(2, 2, false));

            var r1 = TopoArrayUtils.Rotate(ta, 3);
            Assert.AreEqual(2, r1.Get(0, 0)); Assert.AreEqual(4, r1.Get(1, 0));
            Assert.AreEqual(1, r1.Get(0, 1)); Assert.AreEqual(3, r1.Get(1, 1));

            var r3 = TopoArrayUtils.Rotate(ta, 1);
            Assert.AreEqual(3, r3.Get(0, 0)); Assert.AreEqual(1, r3.Get(1, 0));
            Assert.AreEqual(4, r3.Get(0, 1)); Assert.AreEqual(2, r3.Get(1, 1));

            var refl = TopoArrayUtils.Rotate(ta, 0, true);
            Assert.AreEqual(2, refl.Get(0, 0)); Assert.AreEqual(1, refl.Get(1, 0));
            Assert.AreEqual(4, refl.Get(0, 1)); Assert.AreEqual(3, refl.Get(1, 1));
        }

        [Test]
        public void TestHexRotate()
        {
            var a = new int[2, 2];
                a[0, 0] = 1; a[1, 0] = 2;
            a[0, 1] = 3; a[1, 1] = 4;

            var ta = TopoArray.Create(a, new Topology(Directions.Hexagonal2d, 2, 2, false, false));

            var r5 = TopoArrayUtils.HexRotate(ta, 5, false);
                Assert.AreEqual(2, r5.Get(0, 0)); 
            Assert.AreEqual(1, r5.Get(0, 1)); Assert.AreEqual(4, r5.Get(1, 1));
                Assert.AreEqual(3, r5.Get(1, 2));

            var r1 = TopoArrayUtils.HexRotate(ta, 1, false);
            Assert.AreEqual(3, r1.Get(0, 0)); Assert.AreEqual(1, r1.Get(1, 0));
                Assert.AreEqual(4, r1.Get(1, 1)); Assert.AreEqual(2, r1.Get(2, 1));

            var r2 = TopoArrayUtils.HexRotate(ta, 2, false);
                Assert.AreEqual(3, r2.Get(0, 0));
            Assert.AreEqual(4, r2.Get(0, 1)); Assert.AreEqual(1, r2.Get(1, 1));
                Assert.AreEqual(2, r2.Get(1, 2));

            var r3 = TopoArrayUtils.HexRotate(ta, 3, false);
                Assert.AreEqual(4, r3.Get(0, 0)); Assert.AreEqual(3, r3.Get(1, 0));
            Assert.AreEqual(2, r3.Get(0, 1)); Assert.AreEqual(1, r3.Get(1, 1));


            var refl = TopoArrayUtils.HexRotate(ta, 0, true);
            Assert.AreEqual(2, refl.Get(0, 0)); Assert.AreEqual(1, refl.Get(1, 0));
                Assert.AreEqual(4, refl.Get(1, 1)); Assert.AreEqual(3, refl.Get(2, 1));

        }
    }
}
