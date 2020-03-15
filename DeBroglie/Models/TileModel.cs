using DeBroglie.Rot;
using DeBroglie.Topo;
using System.Collections.Generic;

namespace DeBroglie.Models
{

    /// <summary>
    /// Base class for the models used in generation.
    /// </summary>
    // A TileModel is a model with a well defined mapping from 
    // "tiles" (arbitrary identifiers of distinct tiles)
    // with patterns (dense integers that correspond to particular
    // arrangements of tiles).
    public abstract class TileModel
    {
        /// <summary>
        /// Extracts the actual model of patterns used.
        /// </summary>
        internal abstract TileModelMapping GetTileModelMapping(ITopology topology);

        public abstract IEnumerable<Tile> Tiles { get; }

        /// <summary>
        /// Scales the the occurency frequency of a given tile by the given multiplier.
        /// </summary>
        public abstract void MultiplyFrequency(Tile tile, double multiplier);

        /// <summary>
        /// Scales the the occurency frequency of a given tile by the given multiplier, 
        /// including other rotations of the tile.
        /// </summary>
        public virtual void MultiplyFrequency(Tile tile, double multiplier, TileRotation tileRotation)
        {
            var rotatedTiles = new HashSet<Tile>();
            foreach (var rotation in tileRotation.RotationGroup)
            {
                if (tileRotation.Rotate(tile, rotation, out var result))
                {
                    if(rotatedTiles.Add(result))
                    {
                        MultiplyFrequency(result, multiplier);
                    }
                }
            }
        }
    }
}
