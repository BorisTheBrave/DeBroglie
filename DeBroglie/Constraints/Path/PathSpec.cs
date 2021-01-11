using DeBroglie.Rot;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Configures pathing where a given set of tiles form the path, and always connect if adjacent to each other.
    /// </summary>
    public class PathSpec : IPathSpec
    {
        /// <summary>
        /// Set of tiles that are considered on the path
        /// </summary>
        public ISet<Tile> Tiles { get; set; }

        /// <summary>
        /// Set of points that must be connected by paths.
        /// If RelevantCells and RelevantTiles are null, then RelevantTiles defaults to Tiles
        /// </summary>
        public Point[] RelevantCells { get; set; }

        /// <summary>
        /// Set of tiles that must be connected by paths.
        /// If RelevantCells and RelevantTiles are null, then RelevantTiles defaults to Tiles
        /// </summary>
        public ISet<Tile> RelevantTiles { get; set; }

        /// <summary>
        /// If set, Tiles is augmented with extra copies as dictated by the tile rotations
        /// </summary>
        public TileRotation TileRotation { get; set; }

        public IPathView MakeView(TilePropagator tilePropagator)
        {
            return new PathView(this, tilePropagator);
        }
    }
}
