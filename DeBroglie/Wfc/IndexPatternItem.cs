using System;

namespace DeBroglie.Wfc
{
    internal struct IndexPatternItem : IEquatable<IndexPatternItem>
    {
        // Can also take value -1 in some circumstances to indicate that we've saved the state,
        // but no particular choice was made.
        public int Index { get; set; }
        public int Pattern { get; set; }

        public bool Equals(IndexPatternItem other)
        {
            return other.Index == Index && other.Pattern == Pattern;
        }

        public override bool Equals(object obj)
        {
            if (obj is IndexPatternItem other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Index.GetHashCode() * 17 + Pattern.GetHashCode();
            }
        }

        public static bool operator ==(IndexPatternItem a, IndexPatternItem b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(IndexPatternItem a, IndexPatternItem b)
        {
            return !a.Equals(b);
        }
    }
}
