using System;

namespace DeBroglie
{
    public struct Tile
    {
        public Tile(object value)
        {
            this.Value = value;
        }

        public object Value { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is Tile other)
            {
                return object.Equals(Value, other.Value);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value == null ? "null" : Value.ToString();
        }

        public static bool operator ==(Tile a, Tile b)
        {
            return object.Equals(a, b);
        }

        public static bool operator !=(Tile a, Tile b)
        {
            return !(a == b);
        }

    }
}
