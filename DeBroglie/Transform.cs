namespace DeBroglie
{
    internal struct Transform
    {
        public int RotateCw { get; set; }
        public bool ReflectX { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Transform other)
            {
                return RotateCw == other.RotateCw && ReflectX == other.ReflectX;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return RotateCw * 2 + (ReflectX ? 1 : 0);
        }

        public override string ToString()
        {
            return $"({RotateCw}, {ReflectX})";
        }
    }
}
