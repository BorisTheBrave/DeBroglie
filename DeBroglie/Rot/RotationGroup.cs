using System.Collections;
using System.Collections.Generic;

namespace DeBroglie.Rot
{
    /// <summary>
    /// Describes a group of rotations and reflections,
    /// and provides methods for combining them.
    /// </summary>
    public class RotationGroup : IEnumerable<Rotation>
    {
        private readonly int rotationalSymmetry;
        private readonly bool reflectionalSymmetry;
        private List<Rotation> rotations;

        public RotationGroup(int rotationalSymmetry, bool reflectionalSymmetry)
        {
            this.rotationalSymmetry = rotationalSymmetry;
            this.reflectionalSymmetry = reflectionalSymmetry;
            rotations = new List<Rotation>();
            for (var refl = 0; refl < (reflectionalSymmetry ? 2 : 1); refl++)
            {
                for (var rot = 0; rot < rotationalSymmetry; rot++)
                {
                    rotations.Add(new Rotation { RotateCw = rot, ReflectX = refl > 0 });
                }
            }
        }

        public int RotationalSymmetry => rotationalSymmetry;
        public bool ReflectionalSymmetry => reflectionalSymmetry;

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

        public IEnumerator<Rotation> GetEnumerator()
        {
            return rotations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return rotations.GetEnumerator();
        }
    }
}
