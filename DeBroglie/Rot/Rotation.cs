namespace DeBroglie.Rot
{
    public struct Rotation
    {
        public Rotation(int rotateCw = 0, bool reflectX = false)
        {
            this.RotateCw = rotateCw;
            this.ReflectX = reflectX;
        }

        public int RotateCw { get; }
        public bool ReflectX { get; }

        public bool IsIdentity => RotateCw == 0 && !ReflectX;

        public Rotation Inverse()
        {
            return new Rotation(ReflectX ? RotateCw : (360 - RotateCw), ReflectX);
        }

        public static Rotation operator *(Rotation a, Rotation b)
        {
            return new Rotation(
                ((b.ReflectX ? -a.RotateCw : a.RotateCw) + b.RotateCw + 360) % 360,
                a.ReflectX ^ b.ReflectX);
        }

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
            return "!" + (ReflectX ? "x" : "") + (RotateCw);
        }
    }
}
