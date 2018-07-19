using System;
using System.Collections.Generic;

namespace DeBroglie
{
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
