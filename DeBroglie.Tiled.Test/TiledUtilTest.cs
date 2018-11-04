using DeBroglie;
using DeBroglie.Rot;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Tiled.Test
{
    [TestFixture]
    public class TiledUtilTest
    {
        [Test]
        [TestCase(1 * 90, false)]
        [TestCase(2 * 90, false)]
        [TestCase(3 * 90, false)]
        [TestCase(0 * 90, true)]
        [TestCase(1 * 90, true)]
        [TestCase(2 * 90, true)]
        [TestCase(3 * 90, true)]
        public void TestOrthogonalRoundTrip(int rotateCw, bool reflectX)
        {
            var tile = new Tile(123);
            var gid = TiledUtil.TileToGid(new Tile(new RotatedTile { Rotation = new Rotation(rotateCw, reflectX), Tile = tile }));
            var tile2 = (RotatedTile)TiledUtil.GidToTile(gid, TiledLib.Orientation.orthogonal).Value;
            Assert.AreEqual(tile, tile2.Tile);
            Assert.AreEqual(rotateCw, tile2.Rotation.RotateCw);
            Assert.AreEqual(reflectX, tile2.Rotation.ReflectX);
        }

        [Test]
        [TestCase(1 * 60, false)]
        [TestCase(2 * 60, false)]
        [TestCase(3 * 60, false)]
        [TestCase(4 * 60, false)]
        [TestCase(5 * 60, false)]
        [TestCase(0 * 60, true)]
        [TestCase(1 * 60, true)]
        [TestCase(2 * 60, true)]
        [TestCase(3 * 60, true)]
        [TestCase(4 * 60, true)]
        [TestCase(5 * 60, true)]
        public void TestHexagonalRoundTrip(int rotateCw, bool reflectX)
        {
            var tile = new Tile(123);
            var gid = TiledUtil.TileToGid(new Tile(new RotatedTile { Rotation = new Rotation(rotateCw, reflectX), Tile = tile }), TiledLib.Orientation.hexagonal);
            var tile2 = (RotatedTile)TiledUtil.GidToTile(gid, TiledLib.Orientation.hexagonal).Value;
            Assert.AreEqual(tile, tile2.Tile);
            Assert.AreEqual(rotateCw, tile2.Rotation.RotateCw);
            Assert.AreEqual(reflectX, tile2.Rotation.ReflectX);
        }
    }
}
