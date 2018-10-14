using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie.Wfc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie
{

    // Implemenation wise, this wraps a WavePropagator to do the majority of the work.
    // The only thing this class handles is conversion of tile objects into sets of patterns
    // And co-ordinate conversion.
    /// <summary>
    /// TilePropagator is the main entrypoint to the DeBroglie library. 
    /// It takes a <see cref="TileModel"/> and an output <see cref="Topology"/> and generates
    /// an output array using those parameters.
    /// </summary>
    public class TilePropagator
    {
        private readonly WavePropagator wavePropagator;

        private readonly Topology topology;

        private readonly TileModel tileModel;

        private readonly IDictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> tilesToPatternsByOffset;
        private readonly IDictionary<int, IReadOnlyDictionary<int, Tile>> patternsToTilesByOffset;

        private readonly MappingType mappingType;
        private readonly int mappingNX;
        private readonly int mappingNY;
        private readonly int mappingNZ;

        /// <summary>
        /// Constructs a TilePropagator.
        /// </summary>
        /// <param name="tileModel">The model to guide the generation.</param>
        /// <param name="topology">The dimensions of the output to generate</param>
        /// <param name="backtrack">If true, store additional information to allow rolling back choices that lead to a contradiction.</param>
        /// <param name="constraints">Extra constraints to control the generation process.</param>
        /// <param name="random">Source of randomness</param>
        public TilePropagator(TileModel tileModel, Topology topology, bool backtrack = false,
            ITileConstraint[] constraints = null,
            Random random = null)
        {
            this.tileModel = tileModel;
            this.topology = topology;

            var patternTopology = topology;
            if(!(topology.PeriodicX && topology.PeriodicY && topology.PeriodicZ) && tileModel is OverlappingModel overlapping)
            {
                // Shrink the topology as patterns can cover multiple tiles.
                patternTopology = topology.WithSize(
                    topology.PeriodicX ? topology.Width : topology.Width - overlapping.NX + 1,
                    topology.PeriodicY ? topology.Height : topology.Height - overlapping.NY + 1,
                    topology.PeriodicZ ? topology.Depth : topology.Depth - overlapping.NZ + 1);

                mappingType = MappingType.Overlapping;
                mappingNX = overlapping.NX;
                mappingNY = overlapping.NY;
                mappingNZ = overlapping.NZ;

                // Compute tilesToPatterns and patternsToTiles
                var patternArrays = overlapping.PatternArrays;
                tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>>();
                patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>>();
                for (int ox = 0; ox < overlapping.NX; ox++)
                {
                    for (int oy = 0; oy < overlapping.NY; oy++)
                    {
                        for (int oz = 0; oz < overlapping.NZ; oz++)
                        {
                            var o = CombineOffsets(ox, oy, oz);
                            var tilesToPatterns = new Dictionary<Tile, ISet<int>>();
                            tilesToPatternsByOffset[o] = tilesToPatterns;
                            var patternsToTiles = new Dictionary<int, Tile>();
                            patternsToTilesByOffset[o] = patternsToTiles;
                            for(var pattern = 0; pattern<patternArrays.Count; pattern++)
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
                tilesToPatternsByOffset = new Dictionary<int, IReadOnlyDictionary<Tile, ISet<int>>>()
                {
                    {0, tileModel.TilesToPatterns.ToDictionary(g=>g.Key, g=>(ISet<int>)new HashSet<int>(g)) }
                };
                patternsToTilesByOffset = new Dictionary<int, IReadOnlyDictionary<int, Tile>>
                {
                    {0, tileModel.PatternsToTiles},
                };
            }

            var waveConstraints =
                (constraints?.Select(x => new TileConstraintAdaptor(x, this)).ToArray() ?? Enumerable.Empty<IWaveConstraint>())
                .ToArray();

            this.wavePropagator = new WavePropagator(tileModel.GetPatternModel(), patternTopology, backtrack, waveConstraints, random, clear: false);
            wavePropagator.Clear();

        }

        private static ISet<int> Empty = new HashSet<int>();
        private static ISet<int> GetPatterns(IReadOnlyDictionary<Tile, ISet<int>> tilesToPatterns, Tile tile)
        {
            return tilesToPatterns.TryGetValue(tile, out var ps) ? ps : Empty;
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
            return ox + oy * mappingNX + oz * mappingNX * mappingNY;
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

        /// <summary>
        /// The topology of the output.
        /// </summary>
        public Topology Topology => topology;

        /// <summary>
        /// The model to use when generating.
        /// </summary>
        public TileModel TileModel => tileModel;

        /// <summary>
        /// The overall resolution of the generated array.
        /// This will be <see cref="Resolution.Contradiction"/> if at least one location is in contradiction (has no possible tiles)
        /// otherwilse will be <see cref="Resolution.Undecided"/> if at least one location is undecided (has multiple possible tiles)
        /// and will be <see cref="Resolution.Decided"/> otherwise (exactly one tile valid for each location).
        /// </summary>
        public Resolution Status => wavePropagator.Status;

        /// <summary>
        /// This is incremented each time it is necessary to backtrack
        /// a tile while generating results.
        /// It is reset when <see cref="Clear"/> is called.
        /// </summary>
        public int BacktrackCount => wavePropagator.BacktrackCount;

        /// <summary>
        /// Resets the TilePropagator to the state it was in at construction.
        /// </summary>
        /// <returns>The current <see cref="Status"/> (usually <see cref="Resolution.Undecided"/> unless there are very specific initial conditions)</returns>
        public Resolution Clear()
        {
            return wavePropagator.Clear();
        }

        /// <summary>
        /// Marks the given tile as not being a valid choice at a given location.
        /// Then it propogates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Ban(int x, int y, int z, Tile tile)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
            var patterns = GetPatterns(tilesToPatternsByOffset[CombineOffsets(ox, oy, oz)], tile);
            foreach(var p in patterns)
            {
                var status = wavePropagator.Ban(px, py, pz, p);
                if (status != Resolution.Undecided)
                    return status;
            }
            return Resolution.Undecided;
        }


        /// <summary>
        /// Marks the given tile as the only valid choice at a given location.
        /// This is equivalent to banning all other tiles.
        /// Then it propogates that information to other nearby tiles.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Select(int x, int y, int z, Tile tile)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
            var patterns = GetPatterns(tilesToPatternsByOffset[CombineOffsets(ox, oy, oz)], tile);
            for (var p = 0; p < wavePropagator.PatternCount; p++)
            {
                if (patterns.Contains(p))
                    continue;
                var status = wavePropagator.Ban(px, py, pz, p);
                if (status != Resolution.Undecided)
                    return status;
            }
            return Resolution.Undecided;
        }

        /// <summary>
        /// Makes a single tile selection.
        /// Then it propogates that information to other nearby tiles.
        /// If backtracking is enabled a single step can include several backtracks,.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Step()
        {
            return wavePropagator.Step();
        }

        /// <summary>
        /// Repeatedly Steps until the status is Decided or Contradiction.
        /// </summary>
        /// <returns>The current <see cref="Status"/></returns>
        public Resolution Run()
        {
            return wavePropagator.Run();
        }

        /// <summary>
        /// Returns true if this tile is the only valid selection for a given location.
        /// </summary>
        public bool IsSelected(int x, int y, int z, Tile tile)
        {
            GetBannedSelected(x, y, z, tile, out var isBanned, out var isSelected);
            return isSelected;
        }

        /// <summary>
        /// Returns true if this tile is the not a valid selection for a given location.
        /// </summary>
        public bool IsBanned(int x, int y, int z, Tile tile)
        {
            GetBannedSelected(x, y, z, tile, out var isBanned, out var isSelected);
            return isBanned;
        }

        /// <summary>
        /// Gets the results of both IsBanned and IsSelected
        /// </summary>
        public void GetBannedSelected(int x, int y, int z, Tile tile, out bool isBanned, out bool isSelected)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
            var patterns = tilesToPatternsByOffset[CombineOffsets(ox, oy, oz)][tile];
            GetBannedSelectedInternal(px, py, pz, patterns, out isBanned, out isSelected);
        }

        /// <summary>
        /// isBanned is set to true if all the tiles are not valid in the location.
        /// isSelected is set to true if no other the tiles are valid in the location.
        /// </summary>
        public void GetBannedSelected(int x, int y, int z, IEnumerable<Tile> tiles, out bool isBanned, out bool isSelected)
        {
            TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
            var tilesToPatterns = tilesToPatternsByOffset[CombineOffsets(ox, oy, oz)];
            var patterns = new HashSet<int>(tiles.SelectMany(tile => GetPatterns(tilesToPatterns, tile)));
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

        /// <summary>
        /// Converts the generated results to an <see cref="ITopoArray{Tile}"/>,
        /// using specific tiles for locations that have not been decided or are in contradiction.
        /// The arguments are not relevant if the <see cref="Status"/> is <see cref="Resolution.Decided"/>.
        /// </summary>
        public ITopoArray<Tile> ToArray(Tile undecided = default(Tile), Tile contradiction = default(Tile))
        {
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopoArray();

            var result = new Tile[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
                        var pattern = patternArray.Get(px, py, pz);
                        Tile tile;
                        if (pattern == (int)Resolution.Undecided)
                        {
                            tile = undecided;
                        }else if (pattern == (int) Resolution.Contradiction)
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
            return new TopoArray3D<Tile>(result, topology);
        }

        /// <summary>
        /// Converts the generated results to an <see cref="ITopoArray{T}"/>,
        /// by extracting the value of each decided tile and
        /// using specific values for locations that have not been decided or are in contradiction.
        /// This is simply a convenience over 
        /// The arguments are not relevant if the <see cref="Status"/> is <see cref="Resolution.Decided"/>.
        /// </summary>
        public ITopoArray<T> ToValueArray<T>(T undecided = default(T), T contradiction = default(T))
        {
            // TODO: Just call ToArray() ?
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopoArray();

            var result = new T[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
                        var pattern = patternArray.Get(px, py, pz);
                        T value;
                        if (pattern == (int)Resolution.Undecided)
                        {
                            value = undecided;
                        }
                        else if (pattern == (int)Resolution.Contradiction)
                        {
                            value = contradiction;
                        }
                        else
                        {
                            value = (T)patternsToTilesByOffset[CombineOffsets(ox, oy, oz)][pattern].Value;
                        }
                        result[x, y, z] = value;
                    }
                }
            }
            return new TopoArray3D<T>(result, topology);
        }

        /// <summary>
        /// Convert the generated result to an array of sets, where each set
        /// indicates the tiles that are still valid at the location.
        /// The size of the set indicates the resolution of that location:
        /// * Greater than 1: <see cref="Resolution.Undecided"/>
        /// * Exactly 1: <see cref="Resolution.Decided"/>
        /// * Exactly 0: <see cref="Resolution.Contradiction"/>
        /// </summary>
        public ITopoArray<ISet<Tile>> ToArraySets()
        {
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopoArraySets();

            var result = new ISet<Tile>[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
                        var patterns = patternArray.Get(px, py, pz);
                        var hs = new HashSet<Tile>();
                        var patternToTiles = patternsToTilesByOffset[CombineOffsets(ox, oy, oz)];
                        foreach(var pattern in patterns)
                        {
                            hs.Add(patternToTiles[pattern]);
                        }
                        result[x, y, z] = hs;
                    }
                }
            }
            return new TopoArray3D<ISet<Tile>>(result, topology);
        }

        /// <summary>
        /// Convert the generated result to an array of sets, where each set
        /// indicates the values of tiles that are still valid at the location.
        /// The size of the set indicates the resolution of that location:
        /// * Greater than 1: <see cref="Resolution.Undecided"/>
        /// * Exactly 1: <see cref="Resolution.Decided"/>
        /// * Exactly 0: <see cref="Resolution.Contradiction"/>
        /// </summary>
        public ITopoArray<ISet<T>> ToValueSets<T>()
        {
            var width = topology.Width;
            var height = topology.Height;
            var depth = topology.Depth;

            var patternArray = wavePropagator.ToTopoArraySets();

            var result = new ISet<T>[width, height, depth];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        TileCoordToPatternCoord(x, y, z, out var px, out var py, out var pz, out var ox, out var oy, out var oz);
                        var patterns = patternArray.Get(px, py, pz);
                        var hs = new HashSet<T>();
                        var patternToTiles = patternsToTilesByOffset[CombineOffsets(ox, oy, oz)];
                        foreach (var pattern in patterns)
                        {
                            hs.Add((T)patternToTiles[pattern].Value);
                        }
                        result[x, y, z] = hs;
                    }
                }
            }
            return new TopoArray3D<ISet<T>>(result, topology);
        }

        private enum MappingType
        {
            OneToOne,
            Overlapping,
        }
    }
}
