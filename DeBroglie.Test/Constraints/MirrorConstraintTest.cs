using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Test.Constraints
{
    [TestFixture]
    public class MirrorConstraintTest
    {
        [Test]
        public void TestMirrorConstraint()
        {
            var trb = new TileRotationBuilder(4, true, TileRotationTreatment.Missing);

            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            var tile3 = new Tile(3);
            var tile4 = new Tile(4);
            var tile5 = new Tile(5);

            var tiles = new[] { tile1, tile2, tile3, tile4 };

            var reflectX = new Rotation(0, true);

            trb.Add(tile1, reflectX, tile2);
            trb.Add(tile3, reflectX, tile3);
            trb.Add(tile5, reflectX, tile5);

            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(new[] { tile5 }, tiles, Direction.XPlus);
            model.AddAdjacency(new[] { tile5 }, tiles, Direction.XMinus);

            model.SetUniformFrequency();
            model.SetFrequency(tile5, 0.0);

            var tr = trb.Build();

            var constraints = new[] { new MirrorConstraint { TileRotation = tr } };

            // tile1 reflects to tile 2
            {
                var t2 = new Topology(2, 1, false);
                var p2 = new TilePropagator(model, t2, constraints: constraints);
                p2.Select(0, 0, 0, tile1);
                var status = p2.Run();
                Assert.AreEqual(Resolution.Decided, status);
                Assert.AreEqual(tile2, p2.ToArray().Get(1, 0));
            }

            // tile3 reflects to tile3
            {
                var t2 = new Topology(2, 1, false);
                var p2 = new TilePropagator(model, t2, constraints: constraints);
                p2.Select(0, 0, 0, tile3);
                var status = p2.Run();
                Assert.AreEqual(Resolution.Decided, status);
                Assert.AreEqual(tile3, p2.ToArray().Get(1, 0));
            }

            // tile3 only tile that can go in a central space
            // (tile5 can go, but has zero frequency)
            // So tile3 should be selected reliably
            {
                var t2 = new Topology(3, 1, false);
                var p2 = new TilePropagator(model, t2, constraints: constraints);
                var status = p2.Run();
                Assert.AreEqual(Resolution.Decided, status);
                Assert.AreEqual(tile3, p2.ToArray().Get(1, 0));
            }

            // tile5 can be reflected, but cannot
            // be placed adjacent to it's own reflection
            {
                var t2 = new Topology(2, 1, false);
                var p2 = new TilePropagator(model, t2, constraints: constraints);
                p2.Select(0, 0, 0, tile5);
                var status = p2.Run();
                Assert.AreEqual(Resolution.Contradiction, status);
            }

            {
                var t2 = new Topology(4, 1, false);
                var p2 = new TilePropagator(model, t2, constraints: constraints);
                p2.Select(0, 0, 0, tile5);
                var status = p2.Run();
                Assert.AreEqual(Resolution.Decided, status);
            }
        }
    }
}
