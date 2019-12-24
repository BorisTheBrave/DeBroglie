using DeBroglie.Constraints;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Test.Constraints
{
    [TestFixture]
    public class PathConstraintUtilsTest
    {
        [Test]
        public void TestWithoutRelevant()
        {
            // G is
            // 0-1-2    3-4-5
            var g = new PathConstraintUtils.SimpleGraph
            {
                NodeCount = 6,
                Neighbours = new []
                {
                    new [] { 1 },
                    new [] { 0, 2 },
                    new [] { 1 },
                    new [] { 4 },
                    new [] { 3, 5 },
                    new [] { 4 },
                }
            };

            var walkable = new bool[6];
            for (var i = 0; i < 6; i++) walkable[i] = true;

            var art = PathConstraintUtils.GetArticulationPoints(g, walkable);
            Assert.AreEqual(false, art[0]);
            Assert.AreEqual(true, art[1]);
            Assert.AreEqual(false, art[2]);
            Assert.AreEqual(false, art[3]);
            Assert.AreEqual(true, art[4]);
            Assert.AreEqual(false, art[5]);
        }
    }
}
