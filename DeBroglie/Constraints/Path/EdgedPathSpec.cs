using DeBroglie.Rot;
using DeBroglie.Topo;
using System.Collections.Generic;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Configures pathing where a given set of tiles form the path, and connect adjacently if both have appropriate "exits".
    /// </summary>
    public class EdgedPathSpec : IPathSpec
    {
        /// <summary>
        /// For each tile on the path, the set of direction values that paths exit out of this tile.
        /// </summary>
        public IDictionary<Tile, ISet<Direction>> Exits { get; set; }
        
        /// <summary>
        /// Set of points that must be connected by paths.
        /// If RelevantCells and RelevantTiles are null, then RelevantTiles defaults to the tiles in Exits
        /// </summary>
        public Point[] RelevantCells { get; set; }

        /// <summary>
        /// Set of tiles that must be connected by paths.
        /// If RelevantCells and RelevantTiles are null, then RelevantTiles defaults to the tiles in Exits
        /// </summary>
        public ISet<Tile> RelevantTiles { get; set; }

        /// <summary>
        /// If set, Tiles is augmented with extra copies as dictated by the tile rotations
        /// </summary>
        public TileRotation TileRotation { get; set; }

        public IPathView MakeView(TilePropagator tilePropagator)
        {
            return new EdgedPathView(this, tilePropagator);
        }
    }
}
