using DeBroglie.Topo;
using DeBroglie.Rot;
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

            var ta = TopoArray.Create(a, new GridTopology(2, 2, false));

            var r1 = TopoArrayUtils.Rotate(ta, new Rotation(3 * 90));
            Assert.AreEqual(2, r1.Get(0, 0)); Assert.AreEqual(4, r1.Get(1, 0));
            Assert.AreEqual(1, r1.Get(0, 1)); Assert.AreEqual(3, r1.Get(1, 1));

            var r3 = TopoArrayUtils.Rotate(ta, new Rotation(1 * 90));
            Assert.AreEqual(3, r3.Get(0, 0)); Assert.AreEqual(1, r3.Get(1, 0));
            Assert.AreEqual(4, r3.Get(0, 1)); Assert.AreEqual(2, r3.Get(1, 1));

            var refl = TopoArrayUtils.Rotate(ta, new Rotation(0 * 90, true));
            Assert.AreEqual(2, refl.Get(0, 0)); Assert.AreEqual(1, refl.Get(1, 0));
            Assert.AreEqual(4, refl.Get(0, 1)); Assert.AreEqual(3, refl.Get(1, 1));
        }

        [Test]
        public void TestHexRotate()
        {
            var a = new int[2, 2];
                a[0, 0] = 1; a[1, 0] = 2;
            a[0, 1] = 3; a[1, 1] = 4;

            var ta = TopoArray.Create(a, new GridTopology(DirectionSet.Hexagonal2d, 2, 2, false, false));

            var r5 = TopoArrayUtils.HexRotate(ta, new Rotation(5 * 60, false));
                Assert.AreEqual(2, r5.Get(0, 0)); 
            Assert.AreEqual(1, r5.Get(0, 1)); Assert.AreEqual(4, r5.Get(1, 1));
                Assert.AreEqual(3, r5.Get(1, 2));

            var r1 = TopoArrayUtils.HexRotate(ta, new Rotation(1 * 60, false));
            Assert.AreEqual(3, r1.Get(0, 0)); Assert.AreEqual(1, r1.Get(1, 0));
                Assert.AreEqual(4, r1.Get(1, 1)); Assert.AreEqual(2, r1.Get(2, 1));

            var r2 = TopoArrayUtils.HexRotate(ta, new Rotation(2 * 60, false));
                Assert.AreEqual(3, r2.Get(0, 0));
            Assert.AreEqual(4, r2.Get(0, 1)); Assert.AreEqual(1, r2.Get(1, 1));
                Assert.AreEqual(2, r2.Get(1, 2));

            var r3 = TopoArrayUtils.HexRotate(ta, new Rotation(3 * 60, false));
                Assert.AreEqual(4, r3.Get(0, 0)); Assert.AreEqual(3, r3.Get(1, 0));
            Assert.AreEqual(2, r3.Get(0, 1)); Assert.AreEqual(1, r3.Get(1, 1));


            var refl = TopoArrayUtils.HexRotate(ta, new Rotation(0 * 60, true));
            Assert.AreEqual(2, refl.Get(0, 0)); Assert.AreEqual(1, refl.Get(1, 0));
                Assert.AreEqual(4, refl.Get(1, 1)); Assert.AreEqual(3, refl.Get(2, 1));

        }
    }
}
