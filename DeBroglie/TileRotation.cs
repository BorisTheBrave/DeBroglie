using System;
using System.Collections.Generic;

namespace DeBroglie
{
    /// <summary>
    /// Stores how tiles transform to each other via rotations and reflections.
    /// These are constructed with a <see cref="TileRotationBuilder"/>
    /// </summary>
    public class TileRotation
    {
        private IDictionary<Tile, IDictionary<Tuple<int, bool>, Tile>> rotations;

        internal TileRotation(IDictionary<Tile, IDictionary<Tuple<int, bool>, Tile>> rotations)
        {
            this.rotations = rotations;
        }

        internal TileRotation()
        {
            this.rotations = new Dictionary<Tile, IDictionary<Tuple<int, bool>, Tile>>();
        }

        /// <summary>
        /// Attempts to reflect, then rotate clockwise, a given Tile.
        /// If there is a corresponding tile (possibly the same one), then it is set to result.
        /// Otherwise, false is returned.
        /// </summary>
        public bool Rotate(Tile tile, int rotate, bool reflectX, out Tile result)
        {
            if(rotations.TryGetValue(tile, out var d))
            {
                return d.TryGetValue(Tuple.Create(rotate, reflectX), out result);
            }
            else
            {
                result = tile;
                return true;
            }
        }
    }
}
