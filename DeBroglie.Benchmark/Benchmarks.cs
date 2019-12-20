using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using DeBroglie.Models;
using DeBroglie.Topo;
using System.Linq;

namespace DeBroglie.Benchmark
{
    //[EtwProfiler]
    public class Benchmarks
    {
        private TilePropagator propagator1;
        private TilePropagator propagator2;
        private TilePropagator propagator3;

        [GlobalSetup]
        public void Setup()
        {
            FreeSetup();
            ChessSetup();
            CastleSetup();
        }

        public void FreeSetup()
        {
            var tileCount = 10;
            var topology = new Topology(10, 10, 10, false);

            var model = new AdjacentModel(DirectionSet.Cartesian3d);

            var tiles = Enumerable.Range(0, tileCount).Select(x => new Tile(x)).ToList(); ;

            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);
            model.AddAdjacency(tiles, tiles, Direction.ZPlus);

            model.SetUniformFrequency();

            propagator1 = new TilePropagator(model, topology, new TilePropagatorOptions { });
        }

        [Benchmark]
        public void Free()
        {
            propagator1.Clear();
            propagator1.Run();
        }


        public void ChessSetup()
        {
            var topology = new Topology(10, 10, 10, false);

            var model = new AdjacentModel(DirectionSet.Cartesian3d);

            var t1 = new Tile(1);
            var t2 = new Tile(2);

            model.AddAdjacency(t1, t2, Direction.XPlus);
            model.AddAdjacency(t2, t1, Direction.XPlus);
            model.AddAdjacency(t1, t2, Direction.YPlus);
            model.AddAdjacency(t2, t1, Direction.YPlus);
            model.AddAdjacency(t1, t2, Direction.ZPlus);
            model.AddAdjacency(t2, t1, Direction.ZPlus);

            model.SetUniformFrequency();

            propagator2 = new TilePropagator(model, topology, new TilePropagatorOptions { });
        }

        [Benchmark]
        public void Chess()
        {
            propagator2.Clear();
            propagator2.Run();
        }

        // Inspired by Tessera's Castle scene
        public void CastleSetup()
        {
            var topology = new Topology(10, 10, 10, false);

            var model = CastleModel.Get();

            propagator3 = new TilePropagator(model, topology, new TilePropagatorOptions { });
        }


        [Benchmark]
        public void Castle()
        {
            propagator3.Clear();
            propagator3.Run();
        }

    }
}
