namespace DeBroglie.Rot
{
    public struct Rotation
    {
        public Rotation(int rotateCw = 0, bool reflectX = false)
        {
            this.RotateCw = rotateCw;
            this.ReflectX = reflectX;
        }

        public int RotateCw { get; set; }
        public bool ReflectX { get; set; }

        public bool IsIdentity => RotateCw == 0 && !ReflectX;

        public override bool Equals(object obj)
        {
            if (obj is Rotation other)
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
