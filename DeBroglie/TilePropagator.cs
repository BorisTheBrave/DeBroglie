using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie
{

    // Implemenation wise, this wraps a WavePropagator to do the majority of the work.
    // The only thing this class handles is conversion of tile objects into sets of patterns
    // And co-ordinate conversion.
    public class TilePropagator<T>
    {
        private readonly WavePropagator wavePropagator;

        private readonly Topology topology;

        private readonly TileModel<T> tileModel;

        private readonly IDictionary<int, IReadOnlyDictionary<T, ISet<int>>> tilesToPatternsByOffset;
        private readonly IDictionary<int, IReadOnlyDictionary<int, T>> patternsToTilesByOffset;

        private readonly MappingType mappingType;
        private readonly int mappingN;

        public TilePropagator(TileModel<T> tileModel, Topology topology, bool backtrack = false,
            ITileConstraint<T>[] constraints = null,
            IWaveConstraint[] waveConstraints = null,
            Random random = null)
        {
            this.tileModel = tileModel;
            this.topology = topology;

            var patternTopology = topology;
            if(!topology.Periodic && tileModel is OverlappingModel<T> overlapping)
            {
                // Shrink the topology as patterns can cover multiple tiles.
                patternTopology = new Topology(topology.Directions,
                    topology.Width - overlapping.N + 1, 
                    topology.Height - overlapping.N + 1, 
                    topology.Depth == 1 ? 1 : topology.Depth - overlapping.N + 1,
                    topology.Periodic);

                mappingType = MappingType.Overlapping;
                mappingN = overlapping.N;

                // Compute tilesToPatterns and patternsToTiles
                var patternArrays = overlapping.PatternArrays;
                tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<T, ISet<int>>>();
                patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, T>>();
                for (int ox = 0; ox < overlapping.N; ox++)
                {
                    for (int oy = 0; oy < overlapping.N; oy++)
                    {
                        for (int oz = 0; oz < (topology.Depth == 1 ? 1 : overlapping.N); oz++)
                        {
                            var o = CombineOffsets(ox, oy, oz);
                            var tilesToPatterns = new Dictionary<T, ISet<int>>(tileModel.Comparer);
                            tilesToPatternsByOffset[o] = tilesToPatterns;
                            var patternsToTiles = new Dictionary<int, T>();
                            patternsToTilesByOffset[o] = patternsToTiles;
                            for(var pattern =0;pattern<patternArrays.Count;pattern++)
                            {
                                var patternArray = patternArrays[pattern];
                                var tile = patternArray.Values[ox, oy, oz];
                                patternsToTiles[pattern] = tile;
                                if(!tilesToPatterns.TryGetValue(tile, out var patternSet))
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
                mappingType = MappingType.OneToOne;
                tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<T, ISet<int>>>()
                {
                    {0, tileModel.TilesToPatterns.ToDictionary(g=>g.Key, g=>(ISet<int>)new HashSet<int>(g)) }
                };
                patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, T>>
                {
                    {0, tileModel.PatternsToTiles},
                };
            }

            var allWaveConstraints =
                (constraints?.Select(x => new TileConstraintAdaptor<T>(x, this)).ToArray() ?? Enumerable.Empty<IWaveConstraint>())
                .Concat(waveConstraints ?? Enumerable.Empty<IWaveConstraint>())
                .ToArray();

            this.wavePropagator = new WavePropagator(tileModel, patternTopology, backtrack, allWaveConstraints, random, clear: false);
            wavePropagator.Clear();

        }

        private static void OverlapCoord(int x, int width, out int px, out int ox)
        {
            if(x<width)
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

        private int CombineOffsets(int ox, int oy, int oz)
        {
            return ox + oy * mappingN + oz * mappingN * mappingN;
        }

        private void TileCoordToPatternCoord(int x, int y, int z, out int px, out int py, out int pz, out int ox, out int oy, out int oz)
        {
            if(mappingType == MappingType.Overlapping)
            {
                var patternTopology = wavePropagator.Topology;
                OverlapCoord(x, patternTopology.Width,  out px, out ox);
                OverlapCoord(y, patternTopology.Height, out py, out oy);
                OverlapCoord(z, patternTopology.Depth,  out pz, out oz);

            }
            else
            {
                px = x;
                py = y;
                pz = z;
                ox = oy = oz = 0;
            }
        }

        public Topology Topology => topology;
        public TileModel<T> TileModel => tileModel;

        public int BacktrackCount => wavePropagator.BacktrackCount;

        public void Clear()
        {
            wavePropagator.Clear();
        }


        public CellStatus Ban(int x, int y, int z, T tile)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
            var patterns = tilesToPatternsByOffset[CombineOffsets(ox, oy, oz)][tile];
            foreach(var p in patterns)
            {
                var status = wavePropagator.Ban(px, py, pz, p);
                if (status != CellStatus.Undecided)
                    return status;
            }
            return CellStatus.Undecided;
        }

        public CellStatus Select(int x, int y, int z, T tile)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
            var patterns = tilesToPatternsByOffset[CombineOffsets(ox, oy, oz)][tile];
            for (var p = 0; p < wavePropagator.PatternCount; p++)
            {
                if (patterns.Contains(p))
                    continue;
                var status = wavePropagator.Ban(px, py, pz, p);
                if (status != CellStatus.Undecided)
                    return status;
            }
            return CellStatus.Undecided;
        }

        public CellStatus Step()
        {
            return wavePropagator.Step();
        }

        public CellStatus Run()
        {
            return wavePropagator.Run();
        }

        public bool IsSelected(int x, int y, int z, T tile)
        {
            GetBannedSelected(x, y, z, tile, out var isBanned, out var isSelected);
            return isSelected;
        }

        public bool IsBanned(int x, int y, int z, T tile)
        {
            GetBannedSelected(x, y, z, tile, out var isBanned, out var isSelected);
            return isBanned;
        }

        public void GetBannedSelected(int x, int y, int z, T tile, out bool isBanned, out bool isSelected)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
            var patterns = tilesToPatternsByOffset[CombineOffsets(ox, oy, oz)][tile];
            GetBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
        }

        public void GetBannedSelected(int x, int y, int z, IEnumerable<T> tiles, out bool isBanned, out bool isSelected)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
            var tilesToPatterns = tilesToPatternsByOffset[CombineOffsets(ox, oy, oz)];
            var patterns = new HashSet<int>(tiles.SelectMany(tile => tilesToPatterns[tile]));
            GetBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
        }

        private void GetBannedSelectedInternal(int px, int py, int pz, ISet<int> patterns, out bool isBanned, out bool isSelected)
        {
            var index = wavePropagator.Topology.GetIndex(px, py, pz);
            var wave = wavePropagator.Wave;
            var patternCount = wavePropagator.PatternCount;
            isBanned = true;
            isSelected = true;
            for (var p = 0; p < patternCount; p++)
            {
                if (wave.Get(index, p))
                {
                    if (patterns.Contains(p))
                    {
                        isBanned = false;
                    }
                    else
                    {
                        isSelected = false;
                    }
                }
            }
        }

        public ITopArray<T> ToTopArray(T undecided = default(T), T contradiction = default(T))
        {
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopArray();


            var result = new T[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
                        var pattern = patternArray.Get(px, py, pz);
                        T tile;
                        if (pattern == (int)CellStatus.Undecided)
                        {
                            tile = undecided;
                        }else if (pattern == (int) CellStatus.Contradiction)
                        {
                            tile = contradiction;
                        }
                        else
                        {
                            tile = patternsToTilesByOffset[CombineOffsets(ox, oy, oz)][pattern];
                        }
                        result[x, y, z] = tile;
                    }
                }
            }
            return new TopArray3D<T>(result, topology);
        }

        public ITopArray<ISet<T>> ToArraySets()
        {
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopArraySets();

            var result = new ISet<T>[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
                        var patterns = patternArray.Get(px, py, pz);
                        var hs = new HashSet<T>(tileModel.Comparer);
                        var patternToTiles = patternsToTilesByOffset[CombineOffsets(ox, oy, oz)];
                        foreach(var pattern in patterns)
                        {
                            hs.Add(patternToTiles[pattern]);
                        }
                        result[x, y, z] = hs;
                    }
                }
            }
            return new TopArray3D<ISet<T>>(result, topology);
        }

        private enum MappingType
        {
            OneToOne,
            Overlapping,
        }
    }
}
