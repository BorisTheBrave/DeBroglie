namespace DeBroglie.Rot
{
    /// <summary>
    /// Represents a tile that has been rotated and reflected in some way.
    /// </summary>
    public struct RotatedTile
    {
        public Tile Tile { get; set; }
        public Rotation Rotation { get; set; }

        public RotatedTile(Tile tile, Rotation rotation)
        {
            Tile = tile;
            Rotation = rotation;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Rotation.GetHashCode();
                hash = hash * 23 + Tile.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is RotatedTile other)
            {
                return Rotation.Equals(other.Rotation) && Tile == other.Tile;
            }
            else {
                return base.Equals(obj);
            }
        }

        public override string ToString()
        {
            return Tile.ToString() + Rotation.ToString();
        }
    }
}
