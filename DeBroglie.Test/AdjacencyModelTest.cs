using DeBroglie.Models;
using DeBroglie.Topo;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Test
{
    [TestFixture]
    class AdjacentModelTest
    {
        [Test]
        public void TestSimpleAddAdjacencies()
        {
            var model = new AdjacentModel(Directions.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            model.AddTile(tile1);
            model.AddTile(tile2, 5);
            model.AddAdjacency(tile1, tile2, 1, 0, 0);
            model.AddAdjacency(tile1, tile2, 0, 1, 0);
            model.AddAdjacency(tile2, tile1, 1, 0, 0);
            model.AddAdjacency(tile2, tile1, 0, 1, 0);

            var patternModel = model.GetPatternModel();
            Assert.AreEqual(1, patternModel.Frequencies[0]);
            Assert.AreEqual(5, patternModel.Frequencies[1]);

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
            var model = new AdjacentModel(Directions.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);

            model.AddTile(tile1);
            model.AddTile(tile2, 5);

            model.AddAdjacency(new[] { tile1 }, new[] { tile2 }, 1, 0, 0, 4, false);

            var patternModel = model.GetPatternModel();
            Assert.AreEqual(1, patternModel.Frequencies[0]);
            Assert.AreEqual(5, patternModel.Frequencies[1]);

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
            var model = new AdjacentModel(Directions.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            var tile3 = new Tile(3);
            var tile4 = new Tile(4);

            var rotationBuilder = new TileRotationBuilder(TileRotationTreatment.Missing);
            rotationBuilder.Add(tile1, 1, false, tile3);
            rotationBuilder.Add(tile2, 1, false, tile4);
            var rotations = rotationBuilder.Build();

            model.AddTile(tile1);
            model.AddTile(tile2);
            model.AddTile(tile3);
            model.AddTile(tile4);

            model.AddAdjacency(new[] { tile1 }, new[] { tile2 }, 1, 0, 0, 4, false, rotations);

            var patternModel = model.GetPatternModel();

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
    }
}
