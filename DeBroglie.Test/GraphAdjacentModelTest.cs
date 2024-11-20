using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System.Linq;

namespace DeBroglie.Test
{
    [TestFixture]
    class GraphAdjacentModelTest
    {
        [Test]
        public void TestGraphAdjacentModel()
        {
            // Define simple cube graph which unfolds and orients as follows
            //
            //    ┌─┐
            //    │4│
            //  ┌─┼─┼─┬─┐
            //  │3│0│1│2│
            //  └─┼─┼─┴─┘
            //    │5│
            //    └─┘
            // Neighbours (from top, clockwise)
            // 0: [4, 1, 5, 3]
            // 1: [4, 2, 5, 0]
            // 2: [4, 3, 5, 1]
            // 3: [4, 0, 5, 2]
            // 4: [2, 1, 0, 3]
            // 5: [0, 1, 2, 3]

            var meshBuilder = new MeshTopologyBuilder(DirectionSet.Cartesian2d);

            meshBuilder.Add(0, 1, Direction.XPlus);
            meshBuilder.Add(0, 3, Direction.XMinus);
            meshBuilder.Add(0, 5, Direction.YPlus);
            meshBuilder.Add(0, 4, Direction.YMinus);
            meshBuilder.Add(1, 2, Direction.XPlus);
            meshBuilder.Add(1, 0, Direction.XMinus);
            meshBuilder.Add(1, 5, Direction.YPlus);
            meshBuilder.Add(1, 4, Direction.YMinus);
            meshBuilder.Add(2, 3, Direction.XPlus);
            meshBuilder.Add(2, 1, Direction.XMinus);
            meshBuilder.Add(2, 5, Direction.YPlus);
            meshBuilder.Add(2, 4, Direction.YMinus);
            meshBuilder.Add(3, 0, Direction.XPlus);
            meshBuilder.Add(3, 2, Direction.XMinus);
            meshBuilder.Add(3, 5, Direction.YPlus);
            meshBuilder.Add(3, 4, Direction.YMinus);
            meshBuilder.Add(4, 1, Direction.XPlus);
            meshBuilder.Add(4, 3, Direction.XMinus);
            meshBuilder.Add(4, 0, Direction.YPlus);
            meshBuilder.Add(4, 2, Direction.YMinus);
            meshBuilder.Add(5, 1, Direction.XPlus);
            meshBuilder.Add(5, 3, Direction.XMinus);
            meshBuilder.Add(5, 2, Direction.YPlus);
            meshBuilder.Add(5, 0, Direction.YMinus);

            var topology = meshBuilder.GetTopology();

            var model = new GraphAdjacentModel(meshBuilder.GetInfo());

            var empty = new Tile(" ");
            var straight1 = new Tile("║");
            var straight2 = new Tile("═");
            var corner1 = new Tile("╚");
            var corner2 = new Tile("╔");
            var corner3 = new Tile("╗");
            var corner4 = new Tile("╝");

            var tileRotationBuilder = new TileRotationBuilder(4, true, TileRotationTreatment.Missing);
            tileRotationBuilder.AddSymmetry(empty, TileSymmetry.X);
            tileRotationBuilder.AddSymmetry(straight1, TileSymmetry.I);
            tileRotationBuilder.AddSymmetry(straight2, TileSymmetry.I);
            tileRotationBuilder.AddSymmetry(corner1, TileSymmetry.L);
            tileRotationBuilder.AddSymmetry(corner2, TileSymmetry.Q);
            tileRotationBuilder.AddSymmetry(corner3, TileSymmetry.L);
            tileRotationBuilder.AddSymmetry(corner4, TileSymmetry.Q);
            tileRotationBuilder.Add(straight1, new Rotation(90), straight2);
            tileRotationBuilder.Add(corner1, new Rotation(90), corner2);
            tileRotationBuilder.Add(corner2, new Rotation(90), corner3);
            tileRotationBuilder.Add(corner3, new Rotation(90), corner4);
            tileRotationBuilder.Add(corner4, new Rotation(90), corner1);

            var tileRotation = tileRotationBuilder.Build();

            model.AddAdjacency(
                new[] { empty, straight1, corner3, corner4 },
                new[] { empty, straight1, corner1, corner2 },
                Direction.XPlus, tileRotation);

            model.AddAdjacency(
                new[] { straight2, corner1, corner2 },
                new[] { straight2, corner3, corner4 },
                Direction.XPlus, tileRotation);

            model.AddAdjacency(
                new[] { empty, straight2, corner1, corner4 },
                new[] { empty, straight2, corner2, corner3 },
                Direction.YPlus, tileRotation);

            model.AddAdjacency(
                new[] { straight1, corner2, corner3 },
                new[] { straight1, corner1, corner4 },
                Direction.YPlus, tileRotation);

            model.SetUniformFrequency();

            var propagator = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
            });

            void PrintPropagator()
            {

                var a = propagator.ToValueArray("?", "!");

                var str = @"
                ┌─┐
                │4│
              ┌─┼─┼─┬─┐
              │3│0│1│2│
              └─┼─┼─┴─┘
                │5│
                └─┘";
                for (var i = 0; i < 6; i++)
                {
                    str = str.Replace(i.ToString(), (string)a.Get(i));
                }
                System.Console.Write(str);
            }

            propagator.Run();
            PrintPropagator();

            ClassicAssert.AreEqual(Resolution.Decided, propagator.Status);
        }
    }
}
