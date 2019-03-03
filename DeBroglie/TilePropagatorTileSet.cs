using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie
{
    /// <summary>
    /// A set of tiles, specific to a particular TilePropagator.
    /// This class internally caches some computations, making it faster
    /// if you have lots of operations using the same set of tiles.
    /// </summary>
    public class TilePropagatorTileSet
    {
        internal TilePropagatorTileSet(IEnumerable<Tile> tiles)
        {
            Tiles = tiles.ToArray();
            OffsetToPatterns = new Dictionary<int, ISet<int>>();
        }

        public IReadOnlyCollection<Tile> Tiles { get; }

        internal Dictionary<int, ISet<int>> OffsetToPatterns { get; }

        public override string ToString()
        {
            return string.Join(",", Tiles);
        }
    }
}
