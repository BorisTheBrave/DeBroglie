using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Test
{
    [TestFixture]
    public class TiledUtilTest
    {
        [Test]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        public void TestOrthogonalRoundTrip(int rotateCw, bool reflectX)
        {
            var tile = new Tile(123);
            var gid = TiledUtil.TileToGid(new Tile(new RotatedTile { RotateCw = rotateCw, ReflectX = reflectX, Tile = tile }));
            var tile2 = (RotatedTile)TiledUtil.GidToTile(gid, TiledLib.Orientation.orthogonal).Value;
            Assert.AreEqual(tile, tile2.Tile);
            Assert.AreEqual(rotateCw, tile2.RotateCw);
            Assert.AreEqual(reflectX, tile2.ReflectX);
        }

        [Test]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        [TestCase(5, true)]
        public void TestHexagonalRoundTrip(int rotateCw, bool reflectX)
        {
            var tile = new Tile(123);
            var gid = TiledUtil.TileToGid(new Tile(new RotatedTile { RotateCw = rotateCw, ReflectX = reflectX, Tile = tile }), TiledLib.Orientation.hexagonal);
            var tile2 = (RotatedTile)TiledUtil.GidToTile(gid, TiledLib.Orientation.hexagonal).Value;
            Assert.AreEqual(tile, tile2.Tile);
            Assert.AreEqual(rotateCw, tile2.RotateCw);
            Assert.AreEqual(reflectX, tile2.ReflectX);
        }
    }
}
