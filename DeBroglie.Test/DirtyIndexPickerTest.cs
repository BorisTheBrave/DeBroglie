using DeBroglie.Models;
using DeBroglie.Topo;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Test
{
    class DirtyIndexPickerTest
    {
        [Test]
        public void TestDirtyIndexPicker()
        {
            var t1 = new Tile(1);
            var t2 = new Tile(2);
            var t3 = new Tile(3);
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            model.AddAdjacency(t1, t1, Direction.XPlus);
            model.AddAdjacency(t1, t2, Direction.XPlus);
            model.AddAdjacency(t2, t2, Direction.XPlus);
            model.AddAdjacency(t2, t3, Direction.XPlus);
            model.AddAdjacency(t3, t3, Direction.XPlus);
            model.AddAdjacency(t3, t2, Direction.XPlus);
            model.AddAdjacency(t2, t1, Direction.XPlus);

            model.SetUniformFrequency();

            var topology = new GridTopology(6, 1, false);

            var options = new TilePropagatorOptions
            {
                IndexPickerType = IndexPickerType.Dirty,
                TilePickerType = TilePickerType.Ordered,
                CleanTiles = TopoArray.FromConstant(t1, topology),
            };

            var propagator = new TilePropagator(model, topology, options);

            propagator.Select(3, 0, 0, t3);

            propagator.Run();

            var a = propagator.ToValueArray<int?>();
            Assert.AreEqual(null, a.Get(0, 0));
            Assert.AreEqual(null, a.Get(1, 0));
            Assert.AreEqual(2, a.Get(2, 0));
            Assert.AreEqual(3, a.Get(3, 0));
            Assert.AreEqual(2, a.Get(4, 0));
            Assert.AreEqual(null, a.Get(5, 0));
        }

    }
}
