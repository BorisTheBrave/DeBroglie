using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{
    /**
     * A TileModel is a model with a well defined mapping from 
     * "tiles" (arbitrary identifiers of distinct tiles)
     * with patterns (dense integers that correspond to particular
     * arrangements of tiles).
     */
    public abstract class TileModel<T> : Model
    {
        public abstract IReadOnlyDictionary<int, T> PatternsToTiles { get; }
        public abstract ILookup<T, int> TilesToPatterns { get; }
        public abstract IEqualityComparer<T> Comparer { get; }
    }
}
