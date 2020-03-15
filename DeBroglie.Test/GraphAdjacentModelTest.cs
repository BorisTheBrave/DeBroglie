﻿using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using NUnit.Framework;

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

            // Edge label = dir + 4 * clockwise rotation in right angles
            // (e.g. going from face 5 to face 1, is direction XPlus (0), and rotation of 1, so edge label is 4)

            GraphTopology.NeighbourDetails ND(Direction d, int index, Direction inverse, int rot)
            {
                var el = (EdgeLabel)((int)d) + 4 * rot;
                return new GraphTopology.NeighbourDetails
                {
                    Index = index,
                    InverseDirection = inverse,
                    EdgeLabel = el,
                };
            }

            //YMinus,XPlus,YPlus,XMinus

            var topology = new GraphTopology(new GraphTopology.NeighbourDetails[6, 4]
            {
                // Face 0
                {
                    ND(Direction.XPlus,  1, Direction.XMinus, 0),
                    ND(Direction.XMinus, 3, Direction.XPlus, 0),
                    ND(Direction.YPlus,  5, Direction.YMinus, 0),
                    ND(Direction.YMinus, 4, Direction.YPlus, 0),
                },
                // Face 1
                {
                    ND(Direction.XPlus,  2, Direction.XMinus, 0),
                    ND(Direction.XMinus, 0, Direction.XPlus, 0),
                    ND(Direction.YPlus,  5, Direction.XPlus, 3),
                    ND(Direction.YMinus, 4, Direction.XPlus, 1),
                },
                // Face 2
                {
                    ND(Direction.XPlus,  3, Direction.XMinus, 0),
                    ND(Direction.XMinus, 1, Direction.XPlus, 0),
                    ND(Direction.YPlus,  5, Direction.YPlus, 2),
                    ND(Direction.YMinus, 4, Direction.YMinus, 2),
                },
                // Face 3
                {
                    ND(Direction.XPlus,  0, Direction.XMinus, 0),
                    ND(Direction.XMinus, 2, Direction.XPlus, 0),
                    ND(Direction.YPlus,  5, Direction.XMinus, 1),
                    ND(Direction.YMinus, 4, Direction.XMinus, 3),
                },
                // Face 4
                {
                    ND(Direction.XPlus,  1, Direction.YMinus, 3),
                    ND(Direction.XMinus, 3, Direction.YMinus, 1),
                    ND(Direction.YPlus,  0, Direction.YMinus, 0),
                    ND(Direction.YMinus, 2, Direction.YMinus, 2),
                },
                // Face 5
                {
                    ND(Direction.XPlus,  1, Direction.YPlus, 1),
                    ND(Direction.XMinus, 3, Direction.YPlus, 3),
                    ND(Direction.YPlus,  2, Direction.YPlus, 2),
                    ND(Direction.YMinus, 0, Direction.YPlus, 0),
                },
            });

            var model = new GraphAdjacentModel(4, 16);

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

            void AddAdjacency(Tile[] src, Tile[] dest, Direction direction)
            {
                AddAdjacency2(src, dest, direction);
                AddAdjacency2(dest, src, DirectionSet.Cartesian2d.Inverse(direction));
            }

            void AddAdjacency2(Tile[] src, Tile[] dest, Direction direction)
            {
                foreach(var s in src)
                {
                    foreach (var d in dest)
                    {
                        for (var r = 0; r < 4; r++)
                        {
                            var rotation = new Rotation(r == 0 ? 0 : 360 - 90 * r);
                            if (tileRotation.Rotate(d, rotation, out var rd))
                            {
                                var el = (EdgeLabel)(((int)direction) + 4 * r);
                                model.AddAdjacency(s, rd, el);
                            }
                        }
                    }
                }
            }

            /*
            if A  -> B under XPlus and rotation 0
            then can conclude A -> Rotate(B, 270) under rotation 90

            */
            AddAdjacency(
                new[] { empty, straight1, corner3, corner4 },
                new[] { empty, straight1, corner1, corner2 },
                Direction.XPlus);

            AddAdjacency(
                new[] { straight2, corner1, corner2 },
                new[] { straight2, corner3, corner4 },
                Direction.XPlus);

            AddAdjacency(
                new[] { empty, straight2, corner1, corner4 },
                new[] { empty, straight2, corner2, corner3 },
                Direction.YPlus);

            AddAdjacency(
                new[] { straight1, corner2, corner3 },
                new[] { straight1, corner1, corner4 },
                Direction.YPlus);

            model.SetUniformFrequency();

            var propagator = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BackTrackDepth = -1,
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

            Assert.AreEqual(Resolution.Decided, propagator.Status);
        }
    }
}
