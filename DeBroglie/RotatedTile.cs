namespace DeBroglie
{
    /// <summary>
    /// Represents a tile that has been rotated and reflected in some way.
    /// </summary>
    public struct RotatedTile
    {
        public int RotateCw { get; set; }
        public bool ReflectX { get; set; }
        public Tile Tile { get; set; }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + RotateCw.GetHashCode();
                hash = hash * 23 + ReflectX.GetHashCode();
                hash = hash * 23 + Tile.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is RotatedTile other)
            {
                return RotateCw == other.RotateCw && ReflectX == other.ReflectX && Tile == other.Tile;
            }
            else {
                return base.Equals(obj);
            }
        }

        public override string ToString()
        {
            return Tile.ToString() + "!" + (ReflectX ? "x" : "") + (RotateCw * 90);
        }
    }
}
