using System.Collections.Generic;

namespace DeBroglie
{
    internal class TransformGroup
    {
        private readonly int rotationalSymmetry;

        public TransformGroup(int rotationalSymmetry = 4)
        {
            this.rotationalSymmetry = rotationalSymmetry;
            Transforms = new List<Transform>();
            for (var refl = 0; refl < 2; refl++)
            {
                for (var rot = 0; rot < rotationalSymmetry; rot++)
                {
                    Transforms.Add(new Transform { RotateCw = rot, ReflectX = refl > 0 });
                }
            }
        }

        public int RotationalSymmetry => rotationalSymmetry;

        public Transform Mul(Transform a, Transform b)
        {
            var r = new Transform
            {
                RotateCw = (b.ReflectX ? -a.RotateCw : a.RotateCw) + b.RotateCw,
                ReflectX = a.ReflectX ^ b.ReflectX,
            };
            r.RotateCw = (r.RotateCw + rotationalSymmetry) % rotationalSymmetry;
            return r;
        }

        public Transform Mul(Transform a, Transform b, Transform c)
        {
            return Mul(Mul(a, b), c);
        }

        public Transform Mul(Transform a, Transform b, Transform c, Transform d)
        {
            return Mul(Mul(Mul(a, b), c), d);
        }

        public Transform Inverse(Transform tf)
        {
            return new Transform
            {
                RotateCw = tf.ReflectX ? tf.RotateCw : -tf.RotateCw,
                ReflectX = tf.ReflectX,
            };
        }

        public List<Transform> Transforms { get; }
    }
}
