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
        public virtual IReadOnlyDictionary<int, T> PatternsToTiles { get; }
        public virtual ILookup<T, int> TilesToPatterns { get; }
    }
}
