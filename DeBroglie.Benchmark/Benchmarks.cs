using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Benchmark
{
    //[EtwProfiler] // Or use -p ETW on command line
    public class Benchmarks
    {
        private TilePropagator propagatorFree;
        private TilePropagator propagatorChess;
        private TilePropagator propagatorCastle;
        private TilePropagator propagatorWang;
        private TilePropagator propagatorEdgedPath;
        private TilePropagator propagatorEdgedPath2;
        private TilePropagator propagatorPath;
        private TilePropagator propagatorPath2;
        private TilePropagator propagatorCount;
        private TilePropagator propagatorMirror;

        [GlobalSetup]
        public void Setup()
        {
            FreeSetup();
            ChessSetup();
            CastleSetup();
            EdgedPathSetup();
            EdgedPath2Setup();
            PathSetup();
            Path2Setup();
            CountSetup();
            MirrorSetup();
            WangSetup();
        }

        private void Check(TilePropagator p)
        {
            if (p.Status != Resolution.Decided)
                throw new System.Exception("Propagator contradiction");
        }

        public void FreeSetup()
        {
            var tileCount = 10;
            var topology = new GridTopology(10, 10, 10, false);

            var model = new AdjacentModel(DirectionSet.Cartesian3d);

            var tiles = Enumerable.Range(0, tileCount).Select(x => new Tile(x)).ToList(); ;

            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);
            model.AddAdjacency(tiles, tiles, Direction.ZPlus);

            model.SetUniformFrequency();

            propagatorFree = new TilePropagator(model, topology, new TilePropagatorOptions { });
        }

        [Benchmark]
        public void Free()
        {
            propagatorFree.Clear();
            propagatorFree.Run();
        }


        public void ChessSetup()
        {
            var topology = new GridTopology(10, 10, 10, false);

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

            propagatorChess = new TilePropagator(model, topology, new TilePropagatorOptions { });
        }

        [Benchmark]
        public void Chess()
        {
            propagatorChess.Clear();
            propagatorChess.Run();
        }

        // Inspired by Tessera's Castle scene
        public void CastleSetup()
        {
            var topology = new GridTopology(10, 10, 10, false);

            var model = CastleModel.Get();

            propagatorCastle = new TilePropagator(model, topology, new TilePropagatorOptions { });
        }


        [Benchmark]
        public void Castle()
        {
            propagatorCastle.Clear();
            propagatorCastle.Run();
        }

        public void WangSetup()
        {

            // Reproduces the wang tiles found at
            // https://en.wikipedia.org/wiki/Wang_tile
            // They only have aperiodic tiling, so they are a hard set to put down.
            // Clockwise from top
            var tileBorders = new[]
            {
                "rrrg",
                "brbg",
                "rggg",
                "wbrb",
                "bbwb",
                "wwrw",
                "rgbw",
                "bwbr",
                "brwr",
                "ggbr",
                "rwrg",
            };
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            for (var tile1 = 0; tile1 < tileBorders.Length; tile1++)
            {
                var tile1Border = tileBorders[tile1];
                for (var i = 0; i < 4; i++)
                {
                    var d = new[] { 3, 0, 2, 1 }[i];
                    var o = (i + 2) % 4;
                    for (var tile2 = 0; tile2 < tileBorders.Length; tile2++)
                    {
                        var tile2Border = tileBorders[tile2];
                        if (tile2Border[o] != tile1Border[i])
                            continue;
                        model.AddAdjacency(new Tile(tile1), new Tile(tile2), (Direction)d);
                    }
                }
            }
            model.SetUniformFrequency();

            var topology = new GridTopology(15, 15, false);

            var options = new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
            };

            propagatorWang = new TilePropagator(model, topology, options);
        }

        [Benchmark]
        public void Wang()
        {
            propagatorWang.Clear();
            propagatorWang.Run();
        }

        public void EdgedPathSetup()
        {
            var topology = new GridTopology(15, 15, false);

            var model = new AdjacentModel(DirectionSet.Cartesian2d);

            var empty = new Tile(" ");
            var straight1 = new Tile("║");
            var straight2 = new Tile("═");
            var corner1 = new Tile("╚");
            var corner2 = new Tile("╔");
            var corner3 = new Tile("╗");
            var corner4 = new Tile("╝");
            var fork1 = new Tile("╠");
            var fork2 = new Tile("╦");
            var fork3 = new Tile("╣");
            var fork4 = new Tile("╩");

            model.AddAdjacency(
                new[] { empty, straight1, corner3, corner4, fork3 },
                new[] { empty, straight1, corner1, corner2, fork1 },
                Direction.XPlus);

            model.AddAdjacency(
                new[] { straight2, corner1, corner2, fork1, fork2, fork4 },
                new[] { straight2, corner3, corner4, fork2, fork3, fork4 },
                Direction.XPlus);

            model.AddAdjacency(
                new[] { empty, straight2, corner1, corner4, fork4 },
                new[] { empty, straight2, corner2, corner3, fork2 },
                Direction.YPlus);

            model.AddAdjacency(
                new[] { straight1, corner2, corner3, fork1, fork2, fork3 },
                new[] { straight1, corner1, corner4, fork1, fork3, fork4 },
                Direction.YPlus);

            model.SetUniformFrequency();

            var exits = new Dictionary<Tile, ISet<Direction>>
            {
                {straight1, new []{Direction.YMinus, Direction.YPlus}.ToHashSet() },
                {straight2, new []{Direction.XMinus, Direction.XPlus}.ToHashSet() },
                {corner1, new []{Direction.YMinus, Direction.XPlus}.ToHashSet() },
                {corner2, new []{Direction.YPlus, Direction.XPlus}.ToHashSet() },
                {corner3, new []{Direction.YPlus, Direction.XMinus}.ToHashSet() },
                {corner4, new []{Direction.YMinus, Direction.XMinus}.ToHashSet() },
                {fork1, new []{ Direction.YMinus, Direction.XPlus, Direction.YPlus}.ToHashSet() },
                {fork2, new []{ Direction.XPlus, Direction.YPlus, Direction.XMinus}.ToHashSet() },
                {fork3, new []{ Direction.YPlus, Direction.XMinus, Direction.YMinus}.ToHashSet() },
                {fork4, new []{ Direction.XMinus, Direction.YMinus, Direction.XPlus}.ToHashSet() },
            };

#pragma warning disable CS0618 // Type or member is obsolete
            var pathConstraint = new EdgedPathConstraint(exits);
#pragma warning restore CS0618 // Type or member is obsolete

            propagatorEdgedPath = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { pathConstraint },
            });
        }

        [Benchmark]
        public void EdgedPath()
        {
            propagatorEdgedPath.Clear();
            propagatorEdgedPath.Run();

            if (false)
            {
                var v = propagatorEdgedPath.ToValueArray<string>();
                for (var y = 0; y < v.Topology.Height; y++)
                {
                    for (var x = 0; x < v.Topology.Width; x++)
                    {
                        System.Console.Write(v.Get(x, y));
                    }
                    System.Console.WriteLine();
                }
            }

            Check(propagatorEdgedPath);
        }



        public void EdgedPath2Setup()
        {
            var topology = new GridTopology(15, 15, false);

            var model = new AdjacentModel(DirectionSet.Cartesian2d);

            var empty = new Tile(" ");
            var straight1 = new Tile("║");
            var straight2 = new Tile("═");
            var corner1 = new Tile("╚");
            var corner2 = new Tile("╔");
            var corner3 = new Tile("╗");
            var corner4 = new Tile("╝");
            var fork1 = new Tile("╠");
            var fork2 = new Tile("╦");
            var fork3 = new Tile("╣");
            var fork4 = new Tile("╩");

            model.AddAdjacency(
                new[] { empty, straight1, corner3, corner4, fork3 },
                new[] { empty, straight1, corner1, corner2, fork1 },
                Direction.XPlus);

            model.AddAdjacency(
                new[] { straight2, corner1, corner2, fork1, fork2, fork4 },
                new[] { straight2, corner3, corner4, fork2, fork3, fork4 },
                Direction.XPlus);

            model.AddAdjacency(
                new[] { empty, straight2, corner1, corner4, fork4 },
                new[] { empty, straight2, corner2, corner3, fork2 },
                Direction.YPlus);

            model.AddAdjacency(
                new[] { straight1, corner2, corner3, fork1, fork2, fork3 },
                new[] { straight1, corner1, corner4, fork1, fork3, fork4 },
                Direction.YPlus);

            model.SetUniformFrequency();

            var exits = new Dictionary<Tile, ISet<Direction>>
            {
                {straight1, new []{Direction.YMinus, Direction.YPlus}.ToHashSet() },
                {straight2, new []{Direction.XMinus, Direction.XPlus}.ToHashSet() },
                {corner1, new []{Direction.YMinus, Direction.XPlus}.ToHashSet() },
                {corner2, new []{Direction.YPlus, Direction.XPlus}.ToHashSet() },
                {corner3, new []{Direction.YPlus, Direction.XMinus}.ToHashSet() },
                {corner4, new []{Direction.YMinus, Direction.XMinus}.ToHashSet() },
                {fork1, new []{ Direction.YMinus, Direction.XPlus, Direction.YPlus}.ToHashSet() },
                {fork2, new []{ Direction.XPlus, Direction.YPlus, Direction.XMinus}.ToHashSet() },
                {fork3, new []{ Direction.YPlus, Direction.XMinus, Direction.YMinus}.ToHashSet() },
                {fork4, new []{ Direction.XMinus, Direction.YMinus, Direction.XPlus}.ToHashSet() },
            };

            var pathConstraint = new ConnectedConstraint { PathSpec = new EdgedPathSpec { Exits = exits } };

            propagatorEdgedPath2 = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { pathConstraint },
            });
        }

        [Benchmark]
        public void EdgedPath2()
        {
            propagatorEdgedPath2.Clear();
            propagatorEdgedPath2.Run();

            if (false)
            {
                var v = propagatorEdgedPath2.ToValueArray<string>();
                for (var y = 0; y < v.Topology.Height; y++)
                {
                    for (var x = 0; x < v.Topology.Width; x++)
                    {
                        System.Console.Write(v.Get(x, y));
                    }
                    System.Console.WriteLine();
                }
            }

            Check(propagatorEdgedPath2);
        }


        public void PathSetup()
        {

            var tileCount = 10;
            var topology = new GridTopology(20, 20, false);

            var model = new AdjacentModel(DirectionSet.Cartesian2d);

            var tiles = Enumerable.Range(0, tileCount).Select(x => new Tile(x)).ToList(); ;

            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);

            model.SetUniformFrequency();
#pragma warning disable CS0618 // Type or member is obsolete
            var pathConstraint = new PathConstraint(tiles.Skip(1).ToHashSet());
#pragma warning restore CS0618 // Type or member is obsolete

            propagatorPath = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { pathConstraint },
            });
        }

        [Benchmark]
        public void Path()
        {
            propagatorPath.Clear();
            propagatorPath.Run();

            Check(propagatorPath);

            if (false)
            {
                var v = propagatorPath.ToValueArray<string>();
                for (var y = 0; y < v.Topology.Height; y++)
                {
                    for (var x = 0; x < v.Topology.Width; x++)
                    {
                        System.Console.Write(v.Get(x, y));
                    }
                    System.Console.WriteLine();
                }
            }
        }


        public void Path2Setup()
        {

            var tileCount = 10;
            var topology = new GridTopology(20, 20, false);

            var model = new AdjacentModel(DirectionSet.Cartesian2d);

            var tiles = Enumerable.Range(0, tileCount).Select(x => new Tile(x)).ToList(); ;

            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);

            model.SetUniformFrequency();
            var pathConstraint = new ConnectedConstraint { PathSpec = new PathSpec { Tiles = tiles.Skip(1).ToHashSet() } };

            propagatorPath2 = new TilePropagator(model, topology, new TilePropagatorOptions
            {
                BacktrackType = BacktrackType.Backtrack,
                Constraints = new[] { pathConstraint },
            });
        }

        [Benchmark]
        public void Path2()
        {
            propagatorPath2.Clear();
            propagatorPath2.Run();

            Check(propagatorPath2);

            if (false)
            {
                var v = propagatorPath2.ToValueArray<string>();
                for (var y = 0; y < v.Topology.Height; y++)
                {
                    for (var x = 0; x < v.Topology.Width; x++)
                    {
                        System.Console.Write(v.Get(x, y));
                    }
                    System.Console.WriteLine();
                }
            }
        }


        public void CountSetup()
        {
            var model = new AdjacentModel(DirectionSet.Cartesian2d);
            var tile1 = new Tile(1);
            var tile2 = new Tile(2);
            var tiles = new[] { tile1, tile2 };
            model.AddAdjacency(tiles, tiles, Direction.XPlus);
            model.AddAdjacency(tiles, tiles, Direction.YPlus);
            model.SetUniformFrequency();

            var topology = new GridTopology(100, 100, false);

            var count = 30;

            var options = new TilePropagatorOptions
            {
                Constraints = new[]
                {
                    new CountConstraint
                    {
                        Tiles = new[]{tile1 }.ToHashSet(),
                        Count = count,
                        Comparison = CountComparison.AtMost,
                        Eager = false,
                    }
                }
            };
            propagatorCount = new TilePropagator(model, topology, options);
        }

        [Benchmark]
        public void Count()
        {
            propagatorCount.Clear();
            propagatorCount.Run();

            Check(propagatorCount);
        }

        public void MirrorSetup()
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
            model.AddAdjacency(tiles, tiles, Direction.YPlus);


            model.SetUniformFrequency();
            model.SetFrequency(tile5, 0.0);

            var tr = trb.Build();

            var constraints = new[] { new MirrorXConstraint { TileRotation = tr } };


            // NB: It's important that width is an odd number
            var topology = new GridTopology(31, 31, false);

            var options = new TilePropagatorOptions
            {
                Constraints = constraints,
            };
            propagatorMirror = new TilePropagator(model, topology, options);
        }

        [Benchmark]
        public void Mirror()
        {
            propagatorMirror.Clear();
            propagatorMirror.Run();

            Check(propagatorMirror);
        }
    }
}
