using System;
using System.Collections.Generic;

namespace DeBroglie
{

    /// <summary>
    /// Stores how tiles transform to each other via rotations and reflections.
    /// These are constructed with a <see cref="TileRotationBuilder"/>
    /// </summary>
    public class TileRotation
    {
        private readonly IDictionary<Tile, IDictionary<Transform, Tile>> transforms;
        private readonly IDictionary<Tile, TileRotationTreatment> treatments;
        private readonly TileRotationTreatment defaultTreatment;
        private readonly TransformGroup tg;

        internal TileRotation(
            IDictionary<Tile, IDictionary<Transform, Tile>> transforms,
            IDictionary<Tile, TileRotationTreatment> treatments,
            TileRotationTreatment defaultTreatment, 
            TransformGroup tg)
        {
            this.transforms = transforms;
            this.treatments = treatments;
            this.defaultTreatment = defaultTreatment;
            this.tg = tg;
        }

        internal TileRotation(TileRotationTreatment defaultTreatment = TileRotationTreatment.Unchanged)
        {
            this.treatments = new Dictionary<Tile, TileRotationTreatment>();
            this.defaultTreatment = defaultTreatment;
        }

        /// <summary>
        /// Attempts to reflect, then rotate clockwise, a given Tile.
        /// If there is a corresponding tile (possibly the same one), then it is set to result.
        /// Otherwise, false is returned.
        /// </summary>
        public bool Rotate(Tile tile, int rotateCw, bool reflectX, out Tile result)
        {
            Transform tf;
            if(tg != null && tile.Value is RotatedTile rt)
            {
                tf = tg.Mul(
                    new Transform { RotateCw = rt.RotateCw, ReflectX = rt.ReflectX },
                    new Transform { RotateCw = rotateCw, ReflectX = reflectX });
                tile = rt.Tile;
            }
            else
            {
                tf = new Transform { RotateCw = rotateCw, ReflectX = reflectX };
            }

            if(transforms != null && transforms.TryGetValue(tile, out var d))
            {
                if (d.TryGetValue(tf, out result))
                    return true;
            }

            // Transform not found, apply treatment
            if (!treatments.TryGetValue(tile, out var treatment))
                treatment = defaultTreatment;
            switch (treatment)
            {
                case TileRotationTreatment.Missing:
                    result = default(Tile);
                    return false;
                case TileRotationTreatment.Unchanged:
                    result = tile;
                    return true;
                case TileRotationTreatment.Generated:
                    if (tf.ReflectX == false && tf.RotateCw == 0)
                        result = tile;
                    else
                        result = new Tile(new RotatedTile { RotateCw = tf.RotateCw, ReflectX = tf.ReflectX, Tile = tile });
                    return true;
                default:
                    throw new Exception($"Unknown treatment {treatment}");
            }
        }

        /// <summary>
        /// Convenience method for calling Rotate on each tile in a list, skipping any that cannot be rotated.
        /// </summary>
        public IEnumerable<Tile> Rotate(IEnumerable<Tile> tiles, int rotateCw, bool reflectX)
        {
            foreach(var tile in tiles)
            {
                if(Rotate(tile, rotateCw, reflectX, out var tile2))
                {
                    yield return tile2;
                }
            }
        }

        /// <summary>
        /// For a rotated tile, finds the canonical representation.
        /// Leaves all other tiles unchanged.
        /// </summary>
        public Tile Canonicalize(Tile t)
        {
            if(t.Value is RotatedTile rt)
            {
                if (!Rotate(rt.Tile, rt.RotateCw, rt.ReflectX, out var result))
                    throw new Exception($"No tile corresponds to {t}");
                return result;
            }
            else
            {
                return t;
            }
        }

    }
}
