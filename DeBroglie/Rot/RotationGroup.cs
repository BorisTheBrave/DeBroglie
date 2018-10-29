using System.Collections.Generic;

namespace DeBroglie.Rot
{
    internal class RotationGroup
    {
        private readonly int rotationalSymmetry;
        private readonly bool reflectionalSymmetry;

        public RotationGroup(int rotationalSymmetry, bool reflectionalSymmetry)
        {
            this.rotationalSymmetry = rotationalSymmetry;
            this.reflectionalSymmetry = reflectionalSymmetry;
            Rotations = new List<Rotation>();
            for (var refl = 0; refl < (reflectionalSymmetry ? 2 : 1); refl++)
            {
                for (var rot = 0; rot < rotationalSymmetry; rot++)
                {
                    Rotations.Add(new Rotation { RotateCw = rot, ReflectX = refl > 0 });
                }
            }
        }

        public int RotationalSymmetry => rotationalSymmetry;
        public bool ReflectionalSymmetry => reflectionalSymmetry;

        public List<Rotation> Rotations { get; }

        public Rotation Mul(Rotation a, Rotation b)
        {
            var r = new Rotation(
                ((b.ReflectX ? -a.RotateCw : a.RotateCw) + b.RotateCw + rotationalSymmetry) % rotationalSymmetry,
                a.ReflectX ^ b.ReflectX);
            return r;
        }

        public Rotation Mul(Rotation a, Rotation b, Rotation c)
        {
            return Mul(Mul(a, b), c);
        }

        public Rotation Mul(Rotation a, Rotation b, Rotation c, Rotation d)
        {
            return Mul(Mul(Mul(a, b), c), d);
        }

        public Rotation Inverse(Rotation tf)
        {
            return new Rotation
            {
                RotateCw = tf.ReflectX ? tf.RotateCw : (rotationalSymmetry - tf.RotateCw),
                ReflectX = tf.ReflectX,
            };
        }

    }
}
