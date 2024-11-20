﻿using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Test
{
    [TestFixture]
    class AdjacentModelTest
    {
        [Test]
        public void TestSimpleAddAdjacencies()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            model.SetFrequency(tile1, 1);
            model.SetFrequency(tile2, 5);
            model.AddAdjacency(tile1, tile2, 1, 0, 0);
            model.AddAdjacency(tile1, tile2, 0, 1, 0);
            model.AddAdjacency(tile2, tile1, 1, 0, 0);
            model.AddAdjacency(tile2, tile1, 0, 1, 0);

            var patternModel = model.GetTileModelMapping(new GridTopology(10, 10, false)).PatternModel;
            ClassicAssert.AreEqual(1, patternModel.Frequencies[0]);
            ClassicAssert.AreEqual(5, patternModel.Frequencies[1]);

            CollectionAssert.AreEquivalent(new[] { 1 }, patternModel.Propagator[0][0]);
            CollectionAssert.AreEquivalent(new[] { 1 }, patternModel.Propagator[0][1]);
            CollectionAssert.AreEquivalent(new[] { 1 }, patternModel.Propagator[0][2]);
            CollectionAssert.AreEquivalent(new[] { 1 }, patternModel.Propagator[0][3]);
            CollectionAssert.AreEquivalent(new[] { 0 }, patternModel.Propagator[1][0]);
            CollectionAssert.AreEquivalent(new[] { 0 }, patternModel.Propagator[1][1]);
            CollectionAssert.AreEquivalent(new[] { 0 }, patternModel.Propagator[1][2]);
            CollectionAssert.AreEquivalent(new[] { 0 }, patternModel.Propagator[1][3]);
        }

        [Test]
        public void TestRotationalAddAdjacencies()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);

            model.SetFrequency(tile1, 1);
            model.SetFrequency(tile2, 5);

            model.AddAdjacency(new[] { tile1 }, new[] { tile2 }, 1, 0, 0, new TileRotation(4, false));

            var patternModel = model.GetTileModelMapping(new GridTopology(10, 10, false)).PatternModel;
            ClassicAssert.AreEqual(1, patternModel.Frequencies[0]);
            ClassicAssert.AreEqual(5, patternModel.Frequencies[1]);

            CollectionAssert.AreEquivalent(new[] { 1 }, patternModel.Propagator[0][0]);
            CollectionAssert.AreEquivalent(new[] { 1 }, patternModel.Propagator[0][1]);
            CollectionAssert.AreEquivalent(new[] { 1 }, patternModel.Propagator[0][2]);
            CollectionAssert.AreEquivalent(new[] { 1 }, patternModel.Propagator[0][3]);
            CollectionAssert.AreEquivalent(new[] { 0 }, patternModel.Propagator[1][0]);
            CollectionAssert.AreEquivalent(new[] { 0 }, patternModel.Propagator[1][1]);
            CollectionAssert.AreEquivalent(new[] { 0 }, patternModel.Propagator[1][2]);
            CollectionAssert.AreEquivalent(new[] { 0 }, patternModel.Propagator[1][3]);
        }

        [Test]
        public void TestRotationalAddAdjacenciesAdvanced()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            var tile3 = new Tile(3);
            var tile4 = new Tile(4);


            var rotationBuilder = new TileRotationBuilder(4, false, TileRotationTreatment.Missing);
            rotationBuilder.Add(tile1, new Rotation(90), tile3);
            rotationBuilder.Add(tile2, new Rotation(90), tile4);
            var rotations = rotationBuilder.Build();

            model.AddAdjacency(new[] { tile1 }, new[] { tile2 }, 1, 0, 0, rotations);

            model.SetUniformFrequency();

            var patternModel = model.GetTileModelMapping(new GridTopology(10, 10, false)).PatternModel;

            CollectionAssert.AreEquivalent(new int[] { 1 }, patternModel.Propagator[0][0]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[0][1]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[0][2]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[0][3]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[1][0]);
            CollectionAssert.AreEquivalent(new int[] { 0 }, patternModel.Propagator[1][1]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[1][2]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[1][3]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[2][0]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[2][1]);
            CollectionAssert.AreEquivalent(new int[] { 3 }, patternModel.Propagator[2][2]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[2][3]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[3][0]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[3][1]);
            CollectionAssert.AreEquivalent(new int[] {   }, patternModel.Propagator[3][2]);
            CollectionAssert.AreEquivalent(new int[] { 2 }, patternModel.Propagator[3][3]);
        }

        [Test]
        public void TestSetFrequency()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            model.SetFrequency(new Tile(1), 0.5);
            model.SetFrequency(new Tile(2), 2.0);

            var patternModel = model.GetTileModelMapping(new GridTopology(10, 10, false)).PatternModel;

            ClassicAssert.AreEqual(0.5, patternModel.Frequencies[0]);
            ClassicAssert.AreEqual(2.0, patternModel.Frequencies[1]);
        }

        [Test]
        public void TestSetFrequencyWithRotations()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);

            var tile1 = new Tile(1);
            var tile2 = new Tile(2);

            var builder = new TileRotationBuilder(4, true);
            builder.AddSymmetry(tile1, TileSymmetry.T);
            builder.SetTreatment(tile1, TileRotationTreatment.Generated);
            builder.SetTreatment(tile2, TileRotationTreatment.Generated);

            var rotations = builder.Build();

            model.SetFrequency(tile1, 1.0, rotations);
            model.SetFrequency(tile2, 1.0, rotations);

            var tileModelMapping = model.GetTileModelMapping(new GridTopology(10, 10, false));
            var patternModel = tileModelMapping.PatternModel;

            double GetFrequency(Tile tile)
            {
                return patternModel.Frequencies[tileModelMapping.TilesToPatternsByOffset[0][tile].First()];
            }

            ClassicAssert.AreEqual(0.25, GetFrequency(tile1));
            ClassicAssert.AreEqual(0.25, GetFrequency(new Tile(new RotatedTile { Tile = tile1, Rotation = new Rotation(90) })));
            ClassicAssert.AreEqual(0.125, GetFrequency(tile2));
        }
    }
}
