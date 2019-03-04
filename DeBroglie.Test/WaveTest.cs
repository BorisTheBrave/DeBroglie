using NUnit.Framework;
using System;
using DeBroglie;
using DeBroglie.Wfc;

namespace DeBroglie.Test
{
    [TestFixture]
    public class WaveTest
    {
        [Test]
        public void TestWave()
        {
            var r = new Random();
            var wave = new Wave(new double[] { 2, 1, 1 }, 2, null);
            Assert.IsFalse(wave.RemovePossibility(0, 0));
            Assert.AreEqual(0, wave.GetRandomMinEntropyIndex(r));
            Assert.IsFalse(wave.RemovePossibility(1, 2));
            Assert.AreEqual(1, wave.GetRandomMinEntropyIndex(r));
            Assert.IsFalse(wave.RemovePossibility(1, 0));
            Assert.AreEqual(0, wave.GetRandomMinEntropyIndex(r));
            Assert.IsFalse(wave.RemovePossibility(0, 1));
            Assert.AreEqual(Wave.AllCellsDecided, wave.GetRandomMinEntropyIndex(r));
            Assert.IsTrue(wave.RemovePossibility(0, 2));

        }
    }
}
