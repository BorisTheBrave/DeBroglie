﻿using DeBroglie.Console.Import;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Tiled;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeBroglie.Console.Config
{
    /// <summary>
    /// Utility for constructing DeBroglie classes from the coresponding config classes.
    /// </summary>
    public class Factory
    {
        public DeBroglieConfig Config { get; set; }

        public IDictionary<string, Tile> TilesByName { get; set; }

        public Func<string, Tile> TileParser { get; set; }

        private Direction ParseDirection(string s)
        {
            switch (s.ToLower())
            {
                case "x+": return Direction.XPlus;
                case "x-": return Direction.XMinus;
                case "y+": return Direction.YPlus;
                case "y-": return Direction.YMinus;
                case "z+": return Direction.ZPlus;
                case "z-": return Direction.ZMinus;
                case "w+": return Direction.WPlus;
                case "w-": return Direction.WMinus;
            }

            if (!Enum.TryParse(s, true, out Direction r))
            {
                throw new ConfigurationException($"Unable to parse direction \"{s}\"");
            }
            return r;
        }

        private Axis ParseAxis(string s)
        {
            if (!Enum.TryParse(s, true, out Axis r))
            {
                throw new ConfigurationException($"Unable to parse axis \"{s}\"");
            }
            return r;
        }

        public Tile Parse(string s)
        {
            if (s.Contains("!"))
            {
                // TODO: Cleanup and validate
                var a = s.Split('!');
                var b = a[1];
                var refl = false;
                if (b.StartsWith("x"))
                {
                    refl = true;
                    b = b.Substring(1);
                }
                var rotateCw = (int.Parse(b) + 360) % 360;
                return new Tile(new RotatedTile
                {
                    Tile = Parse(a[0]),
                    Rotation = new Rotation(rotateCw, refl),
                });
            }

            if (TilesByName.TryGetValue(s, out var tile))
            {
                return tile;
            }
            if (TileParser != null)
            {
                return TileParser(s);
            }
            else
            {
                return new Tile(s);
            }
        }

        public GridTopology GetOutputTopology(DirectionSet directions)
        {
            var is3d = directions.Type == DirectionSetType.Cartesian3d;
            return new GridTopology(directions, Config.Width, Config.Height, is3d ? Config.Depth : 1, Config.PeriodicX, Config.PeriodicY, Config.PeriodicZ);
        }

        public TileRotation GetTileRotation(TileRotationTreatment? rotationTreatment, GridTopology topology)
        {
            var tileData = Config.Tiles;

            var tileRotationBuilder = new TileRotationBuilder(Config.RotationalSymmetry, Config.ReflectionalSymmetry, rotationTreatment ?? TileRotationTreatment.Unchanged);
            var rotationGroup = tileRotationBuilder.RotationGroup;

            // Setup tiles
            if (tileData != null)
            {
                foreach (var td in tileData)
                {
                    var tile = Parse(td.Value);
                    if (td.TileSymmetry != null)
                    {
                        if (!rotationGroup.ReflectionalSymmetry && rotationGroup.RotationalSymmetry == 1)
                            throw new Exception("Must set symmetry to use tile symmetry");
                        var ts = TileSymmetryUtils.Parse(td.TileSymmetry);
                        tileRotationBuilder.AddSymmetry(tile, ts);
                    }
                    if (td.ReflectX != null)
                    {
                        if (!rotationGroup.ReflectionalSymmetry)
                            throw new Exception("Must set reflection symmetry to use tile reflections");
                        tileRotationBuilder.Add(tile, new Rotation(0, true), Parse(td.ReflectX));
                    }
                    if (td.ReflectY != null)
                    {
                        if (!rotationGroup.ReflectionalSymmetry)
                            throw new Exception("Must set reflection symmetry to use tile reflections");
                        tileRotationBuilder.Add(tile, new Rotation(180, true), Parse(td.ReflectY));
                    }
                    if (td.RotateCw != null)
                    {
                        if (rotationGroup.RotationalSymmetry == 1)
                            throw new Exception("Must set rotation symmetry to use tile rotations");
                        tileRotationBuilder.Add(tile, new Rotation(rotationGroup.SmallestAngle, false), Parse(td.RotateCw));
                    }
                    if (td.RotateCcw != null)
                    {
                        if (rotationGroup.RotationalSymmetry == 1)
                            throw new Exception("Must set rotation symmetry to use tile rotations");
                        tileRotationBuilder.Add(tile, new Rotation(360 - rotationGroup.SmallestAngle, false), Parse(td.RotateCcw));
                    }
                    if (td.RotationTreatment != null)
                    {
                        tileRotationBuilder.SetTreatment(tile, td.RotationTreatment.Value);
                    }
                }
            }

            return tileRotationBuilder.Build();
        }

        private IList<AdjacentModel.Adjacency> GetManualAdjacencies(DirectionSet directions, TileRotation tileRotation)
        {
            if (Config.Adjacencies == null)
                return new AdjacentModel.Adjacency[0];

            AdjacentModel.Adjacency Convert(AdjacencyData a)
            {
                return new AdjacentModel.Adjacency
                {
                    Src = a.Src.Select(Parse).Select(tileRotation.Canonicalize).ToArray(),
                    Dest = a.Dest.Select(Parse).Select(tileRotation.Canonicalize).ToArray(),
                    Direction = a.Direction,
                };
            }

            return AdjacencyUtils.Rotate(
                Config.Adjacencies.Select(Convert).ToList(),
                tileRotation.RotationGroup,
                directions,
                tileRotation);
        }

        private void SetupAdjacencies(TileModel model, TileRotation tileRotation, IList<AdjacentModel.Adjacency> adjacencies)
        {
            if (Config.PadTile != null)
                adjacencies = AdjacencyUtils.ForcePadding(adjacencies, Parse(Config.PadTile));

            if (adjacencies.Count > 0)
            {
                var adjacentModel = model as AdjacentModel;
                if (adjacentModel == null)
                {
                    throw new ConfigurationException("Setting adjacencies is only supported for the \"adjacent\" model.");
                }
                foreach (var a in adjacencies)
                {
                    adjacentModel.AddAdjacency(a);
                }

                // If there are no samples, set frequency to 1 for everything mentioned in this block
                foreach (var tile in adjacentModel.Tiles.ToList())
                {
                    adjacentModel.SetFrequency(tile, 1, tileRotation);
                }
            }
        }

        private void SetupTiles(TileModel model, TileRotation tileRotation)
        {
            if (Config.Tiles != null)
            {
                foreach (var tile in Config.Tiles)
                {
                    var value = Parse(tile.Value);
                    if (tile.MultiplyFrequency != null)
                    {
                        var cf = tile.MultiplyFrequency.Trim();
                        double cfd;
                        if (cf.EndsWith("%"))
                        {
                            cfd = double.Parse(cf.TrimEnd('%'), CultureInfo.InvariantCulture) / 100;
                        }
                        else
                        {
                            cfd = double.Parse(cf, CultureInfo.InvariantCulture);
                        }
                        model.MultiplyFrequency(value, cfd, tileRotation);
                    }
                }
            }
        }

        public TileModel GetModel(DirectionSet directions, SampleSet sampleSet, TileRotation tileRotation)
        {
            var samples = sampleSet.Samples;
            var modelConfig = Config.Model ?? new Adjacent();
            TileModel tileModel;
            if (modelConfig is Overlapping overlapping)
            {
                var model = new OverlappingModel(overlapping.NX, overlapping.NY, overlapping.NZ);
                foreach (var sample in samples)
                {
                    model.AddSample(sample, tileRotation);
                }
                tileModel = model;
            }
            else if (modelConfig is Adjacent adjacent)
            {
                var model = new AdjacentModel(directions);
                foreach (var sample in samples)
                {
                    model.AddSample(sample, tileRotation);
                }
                tileModel = model;
            }
            else
            {
                throw new ConfigurationException($"Unrecognized model type {modelConfig.GetType()}");
            }

            var autoAdjacencies = Config.AutoAdjacency 
                ? AdjacencyUtils.GetAutoAdjacencies(sampleSet, tileRotation, Config.AutoAdjacencyTolerance)
                : new AdjacentModel.Adjacency[0];
            var manualAdjacencies = GetManualAdjacencies(sampleSet.Directions, tileRotation);

            SetupAdjacencies(tileModel, tileRotation, autoAdjacencies.Concat(manualAdjacencies).ToList());
            SetupTiles(tileModel, tileRotation);

            return tileModel;
        }

        public IPathSpec GetPathSpec(AbstractPathSpecConfig abstractPathSpecConfig)
        {
            if(abstractPathSpecConfig is PathSpecConfig pathSpecConfig)
            {
                return new PathSpec
                {
                    Tiles = pathSpecConfig.Tiles.Select(Parse).ToHashSet(),
                    RelevantTiles = pathSpecConfig.RelevantTiles == null ? null : pathSpecConfig.RelevantTiles.Select(Parse).ToHashSet(),
                    RelevantCells = pathSpecConfig.RelevantCells,
                };
            }
            else if(abstractPathSpecConfig is EdgedPathSpecConfig edgedPathSpecConfig)
            {
                return new EdgedPathSpec
                {
                    Exits = edgedPathSpecConfig.Exits.ToDictionary(
                            kv => Parse(kv.Key), x => (ISet<Direction>)new HashSet<Direction>(x.Value.Select(ParseDirection))),
                    RelevantTiles = edgedPathSpecConfig.RelevantTiles == null ? null : edgedPathSpecConfig.RelevantTiles.Select(Parse).ToHashSet(),
                    RelevantCells = edgedPathSpecConfig.RelevantCells,
                };
            }
            else
            {
                throw new Exception($"Unrecognized PathSpec type {abstractPathSpecConfig.GetType()}");
            }
        }

        public List<ITileConstraint> GetConstraints(DirectionSet directions, TileRotation tileRotation)
        {
            var is3d = directions.Type == DirectionSetType.Cartesian3d;

            var constraints = new List<ITileConstraint>();
            if (Config.Ground != null)
            {
                var groundTile = Parse(Config.Ground);
                constraints.Add(new BorderConstraint
                {
                    Sides = is3d ? BorderSides.ZMin : BorderSides.YMax,
                    Tiles = new[] { groundTile },
                });
                constraints.Add(new BorderConstraint
                {
                    Sides = is3d ? BorderSides.ZMin : BorderSides.YMax,
                    Tiles = new[] { groundTile },
                    InvertArea = true,
                    Ban = true,
                });
            }

            if (Config.Constraints != null)
            {
                foreach (var constraint in Config.Constraints)
                {
                    if (constraint is PathConfig pathData)
                    {
                        var tiles = new HashSet<Tile>(pathData.Tiles.Select(Parse));
                        var p = new ConnectedConstraint
                        {
                            PathSpec = new PathSpec
                            {
                                Tiles = tiles,
                                RelevantCells = pathData.EndPoints,
                                RelevantTiles = pathData.EndPointTiles == null ? null : new HashSet<Tile>(pathData.EndPointTiles.Select(Parse)),
                                TileRotation = tileRotation,
                            }
                        };
                        constraints.Add(p);
                    }
                    else if (constraint is EdgedPathConfig edgedPathData)
                    {
                        var exits = edgedPathData.Exits.ToDictionary(
                            kv => Parse(kv.Key), x => (ISet<Direction>)new HashSet<Direction>(x.Value.Select(ParseDirection)));
                        var p = new ConnectedConstraint
                        {
                            PathSpec = new EdgedPathSpec
                            {
                                Exits = exits,
                                RelevantCells = edgedPathData.EndPoints,
                                RelevantTiles = edgedPathData.EndPointTiles == null ? null : new HashSet<Tile>(edgedPathData.EndPointTiles.Select(Parse)),
                                TileRotation = tileRotation,
                            }
                        };
                        constraints.Add(p);
                    }
                    else if (constraint is BorderConfig borderData)
                    {
                        var tiles = borderData.Tiles.Select(Parse).ToArray();
                        var sides = borderData.Sides == null ? BorderSides.All : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.Sides, true);
                        var excludeSides = borderData.ExcludeSides == null ? BorderSides.None : (BorderSides)Enum.Parse(typeof(BorderSides), borderData.ExcludeSides, true);
                        if (!is3d)
                        {
                            sides = sides & ~BorderSides.ZMin & ~BorderSides.ZMax;
                            excludeSides = excludeSides & ~BorderSides.ZMin & ~BorderSides.ZMax;
                        }
                        constraints.Add(new BorderConstraint
                        {
                            Tiles = tiles,
                            Sides = sides,
                            ExcludeSides = excludeSides,
                            InvertArea = borderData.InvertArea,
                            Ban = borderData.Ban,
                        });
                    }
                    else if (constraint is FixedTileConfig fixedTileConfig)
                    {
                        constraints.Add(new FixedTileConstraint
                        {
                            Tiles = fixedTileConfig.Tiles.Select(Parse).ToArray(),
                            Point = fixedTileConfig.Point,
                        });
                    }
                    else if (constraint is MaxConsecutiveConfig maxConsecutiveConfig)
                    {
                        var axes = maxConsecutiveConfig.Axes?.Select(ParseAxis);
                        constraints.Add(new MaxConsecutiveConstraint
                        {
                            Tiles = new HashSet<Tile>(maxConsecutiveConfig.Tiles.Select(Parse)),
                            MaxCount = maxConsecutiveConfig.MaxCount,
                            Axes = axes == null ? null : new HashSet<Axis>(axes),
                        });
                    }
                    else if (constraint is MirrorXConfig mirrorYConfig)
                    {
                        constraints.Add(new MirrorXConstraint
                        {
                            TileRotation = tileRotation,
                        });
                    }
                    else if (constraint is MirrorYConfig mirrorXConfig)
                    {
                        constraints.Add(new MirrorYConstraint
                        {
                            TileRotation = tileRotation,
                        });
                    }
                    else if (constraint is CountConfig countConfig)
                    {
                        constraints.Add(new CountConstraint
                        {
                            Tiles = new HashSet<Tile>(countConfig.Tiles.Select(Parse)),
                            Comparison = countConfig.Comparison,
                            Count = countConfig.Count,
                            Eager = countConfig.Eager,
                        });
                    }
                    else if (constraint is SeparationConfig separationConfig)
                    {
                        constraints.Add(new SeparationConstraint
                        {
                            Tiles = new HashSet<Tile>(separationConfig.Tiles.Select(Parse)),
                            MinDistance = separationConfig.MinDistance,
                        });
                    }
                    else if (constraint is PairSeparationConfig pairSeparationConfig)
                    {
                        constraints.Add(new PairSeparationConstraint
                        {
                            Tiles1 = new HashSet<Tile>(pairSeparationConfig.Tiles1.Select(Parse)),
                            Tiles2 = new HashSet<Tile>(pairSeparationConfig.Tiles2.Select(Parse)),
                            MinDistance = pairSeparationConfig.MinDistance,
                        });
                    }
                    else if (constraint is ConnectedConfig connectedConfig)
                    {
                        constraints.Add(new ConnectedConstraint
                        {
                            PathSpec = GetPathSpec(connectedConfig.PathSpec),
                        });
                    }
                    else if (constraint is LoopConfig loopConfig)
                    {
                        constraints.Add(new LoopConstraint
                        {
                            PathSpec = GetPathSpec(loopConfig.PathSpec),
                        });
                    }
                    else if (constraint is AcyclicConfig acyclicConfig)
                    {
                        constraints.Add(new AcyclicConstraint
                        {
                            PathSpec = GetPathSpec(acyclicConfig.PathSpec),
                        });
                    }
                    else
                    {
                        throw new NotImplementedException($"Unknown constraint type {constraint.GetType()}");
                    }
                }
            }

            return constraints;
        }

    }
}
