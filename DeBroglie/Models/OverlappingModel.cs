using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Models
{

    /// <summary>
    /// OverlappingModel constrains that every n by n rectangle in the output is a copy of a rectangle taken from the sample.
    /// </summary>
    public class OverlappingModel : TileModel
    {
        private int nx;
        private int ny;
        private int nz;

        private Dictionary<PatternArray, int> patternIndices;
        private List<PatternArray> patternArrays;
        private List<double> frequencies;
        private DirectionSet sampleTopologyDirections;
        private List<int[][]> propagator;

        private IReadOnlyDictionary<int, Tile> patternsToTiles;
        private ILookup<Tile, int> tilesToPatterns;

        public static OverlappingModel Create<T>(T[,] sample, int n, bool periodic, int symmetries)
        {
            var topArray = new TopoArray2D<T>(sample, periodic).ToTiles();

            return new OverlappingModel(topArray, n, symmetries > 1 ? symmetries / 2 : 1, symmetries > 1);
        }


        public OverlappingModel(ITopoArray<Tile> sample, int n, int rotationalSymmetry, bool reflectionalSymmetry)
            : this(n)
        {
            AddSample(sample, new TileRotation(rotationalSymmetry, reflectionalSymmetry));
        }

        /// <summary>
        /// Shorthand for constructing an Overlapping model with an n by n square or n by n by cuboid.
        /// </summary>
        /// <param name="n"></param>
        public OverlappingModel(int n)
            : this(n, n, n)
        {

        }

        public OverlappingModel(int nx, int ny, int nz)
        {
            this.nx = nx;
            this.ny = ny;
            this.nz = nz;
            patternIndices = new Dictionary<PatternArray, int>(new PatternArrayComparer());
            frequencies = new List<double>();
            patternArrays = new List<PatternArray>();
            propagator = new List<int[][]>();
        }

        public void AddSample(ITopoArray<Tile> sample, TileRotation tileRotation = null)
        {
            if (sample.Topology.Depth == 1)
                nz = 1;

            var topology = sample.Topology.AsGridTopology();

            var periodicX = topology.PeriodicX;
            var periodicY = topology.PeriodicY;
            var periodicZ = topology.PeriodicZ;

            foreach (var s in OverlappingAnalysis.GetRotatedSamples(sample, tileRotation))
            {
                OverlappingAnalysis.GetPatterns(s, nx, ny, nz, periodicX, periodicY, periodicZ, patternIndices, patternArrays, frequencies);
            }

            sampleTopologyDirections = topology.Directions;
            propagator = null;// Mark as dirty
        }

        public int NX => nx;
        public int NY => ny;
        public int NZ => nz;

        internal IReadOnlyList<PatternArray> PatternArrays => patternArrays;

        public override IEnumerable<Tile> Tiles => tilesToPatterns.Select(x => x.Key);

        private void Build()
        {
            if (propagator != null)
                return;


            // Update the model based on the collected data
            var directions = sampleTopologyDirections;

            // Collect all the pattern edges
            var patternIndicesByEdge = new Dictionary<Direction, Dictionary<PatternArray, int[]>>();
            var edgesByPatternIndex = new Dictionary<(Direction, int), PatternArray>();
            for (var d = 0; d < directions.Count; d++)
            {
                var dx = directions.DX[d];
                var dy = directions.DY[d];
                var dz = directions.DZ[d];
                var edges = new Dictionary<PatternArray, HashSet<int>>(new PatternArrayComparer());
                for (var p = 0; p < patternArrays.Count; p++)
                {
                    var edge = OverlappingAnalysis.PatternEdge(patternArrays[p], dx, dy, dz);
                    if (!edges.TryGetValue(edge, out var l))
                    {
                        l = edges[edge] = new HashSet<int>();
                    }
                    l.Add(p);
                    edgesByPatternIndex[((Direction)d, p)] = edge;
                }
                patternIndicesByEdge[(Direction)d] = edges
                    .ToDictionary(
                        x => x.Key, 
                        x => x.Value.OrderBy(y => y).ToArray(),
                        new PatternArrayComparer());
            }

            // Setup propagator
            var empty = new int[0];
            propagator = new List<int[][]>(patternArrays.Count);
            for (var p = 0; p < patternArrays.Count; p++)
            {
                propagator.Add(new int[directions.Count][]);
                for (var d = 0; d < directions.Count; d++)
                {
                    var dir = (Direction)d;
                    var invDir = directions.Inverse(dir);
                    var edge = edgesByPatternIndex[(dir, p)];
                    if (patternIndicesByEdge[invDir].TryGetValue(edge, out var otherPatterns))
                    {
                        propagator[p][d] = otherPatterns;
                    }
                    else
                    {
                        propagator[p][d] = empty;
                    }
                }
            }

            patternsToTiles = patternArrays
                .Select((x, i) => new KeyValuePair<int, Tile>(i, x.Values[0, 0, 0]))
                .ToDictionary(x => x.Key, x => x.Value);

            tilesToPatterns = patternsToTiles.ToLookup(x => x.Value, x => x.Key);

        }

        internal override TileModelMapping GetTileModelMapping(ITopology topology)
        {
            Build();

            var gridTopology = topology.AsGridTopology();
            var patternModel = new PatternModel
            {
                Propagator = propagator.Select(x => x.Select(y => y).ToArray()).ToArray(),
                Frequencies = frequencies.ToArray(),
            };

            if(frequencies.Count == 0)
            {
                throw new Exception("Attempting to create an overlapping model with zero patterns.");
            }

            GridTopology patternTopology;
            Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> tilesToPatternsByOffset;
            Dictionary<int, IReadOnlyDictionary<int, Tile>> patternsToTilesByOffset;
            ITopoArray<(Point, int, int)> tileCoordToPatternCoordIndexAndOffset;
            ITopoArray<List<(Point, int, int)>> patternCoordToTileCoordIndexAndOffset;
            if (!(gridTopology.PeriodicX && gridTopology.PeriodicY && gridTopology.PeriodicZ))
            {
                // Shrink the topology as patterns can cover multiple tiles.
                patternTopology = gridTopology.WithSize(
                    gridTopology.PeriodicX ? topology.Width : topology.Width - NX + 1,
                    gridTopology.PeriodicY ? topology.Height : topology.Height - NY + 1,
                    gridTopology.PeriodicZ ? topology.Depth : topology.Depth - NZ + 1);

                if (patternTopology.Width <= 0)
                {
                    throw new System.Exception($"Sample width {topology.Width} not wide enough for overlap of {NX}");
                }
                if (patternTopology.Height <= 0)
                {
                    throw new System.Exception($"Sample height {topology.Height} not wide enough for overlap of {NY}");
                }
                if (patternTopology.Depth <= 0)
                {
                    throw new System.Exception($"Sample depth {topology.Depth} not wide enough for overlap of {NZ}");
                }


                void OverlapCoord(int x, int width, out int px, out int ox)
                {
                    if (x < width)
                    {
                        px = x;
                        ox = 0;
                    }
                    else
                    {
                        px = width - 1;
                        ox = x - px;
                    }
                }

                int CombineOffsets(int ox, int oy, int oz)
                {
                    return ox + oy * NX + oz * NX * NY;
                }

                (Point, int, int) Map(Point t)
                {
                    OverlapCoord(t.X, patternTopology.Width, out var px, out var ox);
                    OverlapCoord(t.Y, patternTopology.Height, out var py, out var oy);
                    OverlapCoord(t.Z, patternTopology.Depth, out var pz, out var oz);
                    var patternIndex = patternTopology.GetIndex(px, py, pz);
                    return (new Point(px, py, pz), patternIndex, CombineOffsets(ox, oy, oz));
                }

                /*
                (Point, int, int) RMap(Point t)
                {
                    OverlapCoord(t.X, patternTopology.Width, out var px, out var ox);
                    OverlapCoord(t.Y, patternTopology.Height, out var py, out var oy);
                    OverlapCoord(t.Z, patternTopology.Depth, out var pz, out var oz);
                    var patternIndex = patternTopology.GetIndex(px, py, pz);
                    return (new Point(px, py, pz), patternIndex, CombineOffsets(ox, oy, oz));
                }
                */

                tileCoordToPatternCoordIndexAndOffset = TopoArray.CreateByPoint(Map, gridTopology);
                var patternCoordToTileCoordIndexAndOffsetValues = new List<(Point, int, int)>[patternTopology.Width, patternTopology.Height, patternTopology.Depth];
                foreach (var index in topology.GetIndices())
                {
                    topology.GetCoord(index, out var x, out var y, out var z);
                    var (p, patternIndex, offset) = tileCoordToPatternCoordIndexAndOffset.Get(index);
                    if (patternCoordToTileCoordIndexAndOffsetValues[p.X, p.Y, p.Z] == null)
                    {
                        patternCoordToTileCoordIndexAndOffsetValues[p.X, p.Y, p.Z] = new List<(Point, int, int)>();
                    }
                    patternCoordToTileCoordIndexAndOffsetValues[p.X, p.Y, p.Z].Add((new Point(x, y, z), index, offset));
                }
                patternCoordToTileCoordIndexAndOffset = TopoArray.Create(patternCoordToTileCoordIndexAndOffsetValues, patternTopology);


                // Compute tilesToPatterns and patternsToTiles
                tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>>();
                patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>>();
                for (int ox = 0; ox < NX; ox++)
                {
                    for (int oy = 0; oy < NY; oy++)
                    {
                        for (int oz = 0; oz < NZ; oz++)
                        {
                            var o = CombineOffsets(ox, oy, oz);
                            var tilesToPatterns = new Dictionary<Tile, ISet<int>>();
                            tilesToPatternsByOffset[o] = tilesToPatterns;
                            var patternsToTiles = new Dictionary<int, Tile>();
                            patternsToTilesByOffset[o] = patternsToTiles;
                            for (var pattern = 0; pattern < patternArrays.Count; pattern++)
                            {
                                var patternArray = patternArrays[pattern];
                                var tile = patternArray.Values[ox, oy, oz];
                                patternsToTiles[pattern] = tile;
                                if (!tilesToPatterns.TryGetValue(tile, out var patternSet))
                                {
                                    patternSet = tilesToPatterns[tile] = new HashSet<int>();
                                }
                                patternSet.Add(pattern);
                            }
                        }
                    }
                }
            }
            else
            {

                patternTopology = gridTopology;
                tileCoordToPatternCoordIndexAndOffset = null;
                patternCoordToTileCoordIndexAndOffset = null;
                tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>>()
                {
                    {0, tilesToPatterns.ToDictionary(g=>g.Key, g=>(ISet<int>)new HashSet<int>(g)) }
                };
                patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>>
                {
                    {0, patternsToTiles},
                };
            }

            // Masks interact a bit weirdly with the overlapping model
            // We choose a pattern mask that is a expansion of the topology mask
            // i.e. a pattern location is masked out if all the tile locations it covers is masked out.
            // This makes the propagator a bit conservative - it'll always preserve the overlapping property
            // but might ban some layouts that make sense.
            // The alternative is to contract the mask - that is more permissive, but sometimes will
            // violate the overlapping property.
            // (passing the mask verbatim is unacceptable as does not lead to symmetric behaviour)
            // See TestTileMaskWithThinOverlapping for an example of the problem, and
            // https://github.com/BorisTheBrave/DeBroglie/issues/7 for a possible solution.
            if (topology.Mask != null)
            {
                // TODO: This could probably do with some cleanup
                bool GetTopologyMask(int x, int y, int z)
                {
                    if (!gridTopology.PeriodicX && x >= topology.Width)
                        return false;
                    if (!gridTopology.PeriodicY && y >= topology.Height)
                        return false;
                    if (!gridTopology.PeriodicZ && z >= topology.Depth)
                        return false;
                    x = x % topology.Width;
                    y = y % topology.Height;
                    z = z % topology.Depth;
                    return topology.Mask[topology.GetIndex(x, y, z)];
                }
                bool GetPatternTopologyMask(Point p)
                {
                    for (var oz = 0; oz < NZ; oz++)
                    {
                        for (var oy = 0; oy < NY; oy++)
                        {
                            for (var ox = 0; ox < NX; ox++)
                            {
                                if (GetTopologyMask(p.X + ox, p.Y + oy, p.Z + oz))
                                    return true;
                            }
                        }
                    }
                    return false;
                }

                var patternMask = TopoArray.CreateByPoint(GetPatternTopologyMask, patternTopology);
                patternTopology = patternTopology.WithMask(patternMask);
            }


            return new TileModelMapping
            {
                PatternModel = patternModel,
                PatternsToTilesByOffset = patternsToTilesByOffset,
                TilesToPatternsByOffset = tilesToPatternsByOffset,
                PatternTopology = patternTopology,
                TileCoordToPatternCoordIndexAndOffset = tileCoordToPatternCoordIndexAndOffset,
                PatternCoordToTileCoordIndexAndOffset = patternCoordToTileCoordIndexAndOffset,
            };

        }

        public override void MultiplyFrequency(Tile tile, double multiplier)
        {
            for (var p = 0; p < patternArrays.Count; p++)
            {
                var patternArray = patternArrays[p];
                for (var x = 0; x < patternArray.Width; x++)
                {
                    for (var y = 0; y < patternArray.Height; y++)
                    {
                        for (var z = 0; z < patternArray.Depth; z++)
                        {
                            if (patternArray.Values[x, y, z] == tile)
                            {
                                frequencies[p] *= multiplier;
                            }
                        }
                    }
                }
            }
        }
    }

}
