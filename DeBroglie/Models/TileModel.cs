using DeBroglie.Wfc;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Models
{
    /**
     * A TileModel is a model with a well defined mapping from 
     * "tiles" (arbitrary identifiers of distinct tiles)
     * with patterns (dense integers that correspond to particular
     * arrangements of tiles).
     */
    public abstract class TileModel
    {
        internal abstract PatternModel GetPatternModel();

        public abstract IReadOnlyDictionary<int, Tile> PatternsToTiles { get; }
        public abstract ILookup<Tile, int> TilesToPatterns { get; }

        public abstract void ChangeFrequency(Tile tile, double relativeChange);
    }
}
