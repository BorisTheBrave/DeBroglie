using DeBroglie.Topo;
using DeBroglie.Wfc;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DeBroglie.Models
{
    internal struct TileModelMapping
    {
        private static readonly ISet<int> EmptyPatternSet = new HashSet<int>();

        public ITopology PatternTopology { get; set; }

        public PatternModel PatternModel { get; set; }

        public IDictionary<int, IReadOnlyDictionary<Tile, ISet<int>>> TilesToPatternsByOffset { get; set; }

        public IDictionary<int, IReadOnlyDictionary<int, Tile>> PatternsToTilesByOffset { get; set; }

        // Null for 1:1 mappings
        public ITopoArray<(Point, int, int)> TileCoordToPatternCoordIndexAndOffset { get; set; }

        // Null for 1:1 mappings
        public ITopoArray<List<(Point, int, int)>> PatternCoordToTileCoordIndexAndOffset { get; set; }

        public void GetTileCoordToPatternCoord(int x, int y, int z, out int px, out int py, out int pz, out int offset)
        {
            if (TileCoordToPatternCoordIndexAndOffset == null)
            {
                px = x;
                py = y;
                pz = z;
                offset = 0;

                return;
            }

            var (point, index, o) = TileCoordToPatternCoordIndexAndOffset.Get(x, y, z);
            px = point.X;
            py = point.Y;
            pz = point.Z;
            offset = o;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetTileCoordToPatternCoord(int index, out int patternIndex, out int offset)
        {
            if (TileCoordToPatternCoordIndexAndOffset == null)
            {
                patternIndex = index;
                offset = 0;

                return;
            }

            (_, patternIndex, offset) = TileCoordToPatternCoordIndexAndOffset.Get(index);
        }


        /// <summary>
        /// Creates a set of tiles. This set can be used with some operations, and is marginally
        /// faster than passing in a fresh list of tiles ever time.
        /// </summary>
        public TilePropagatorTileSet CreateTileSet(IEnumerable<Tile> tiles)
        {
            var set = new TilePropagatorTileSet(tiles);
            // Quick optimization for size one sets
            if (set.Tiles.Count == 1)
            {
                var tile = set.Tiles.First();
                foreach (var o in TilesToPatternsByOffset.Keys)
                {
                    set.OffsetToPatterns[o] = TilesToPatternsByOffset[o].TryGetValue(tile, out var patterns) ? patterns : EmptyPatternSet;
                }
            }
            return set;
        }

        private static ISet<int> Empty = new HashSet<int>();

        private static ISet<int> GetPatterns(IReadOnlyDictionary<Tile, ISet<int>> tilesToPatterns, Tile tile)
        {
            return tilesToPatterns.TryGetValue(tile, out var ps) ? ps : Empty;
        }

        /// <summary>
        /// Gets the patterns associated with a set of tiles at a given offset.
        /// </summary>
        public ISet<int> GetPatterns(Tile tile, int offset)
        {
            return GetPatterns(TilesToPatternsByOffset[offset], tile);
        }

        /// <summary>
        /// Gets the patterns associated with a set of tiles at a given offset.
        /// </summary>
        public ISet<int> GetPatterns(TilePropagatorTileSet tileSet, int offset)
        {
            if (!tileSet.OffsetToPatterns.TryGetValue(offset, out var patterns))
            {
                var tilesToPatterns = TilesToPatternsByOffset[offset];
                patterns = new HashSet<int>(tileSet.Tiles.SelectMany(tile => GetPatterns(tilesToPatterns, tile)));
                tileSet.OffsetToPatterns[offset] = patterns;
            }
            return patterns;
        }
    }
}
