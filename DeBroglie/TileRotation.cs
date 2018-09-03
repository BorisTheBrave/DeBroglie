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
        private readonly TransformGroup tg;

        internal TileRotation(IDictionary<Tile, IDictionary<Transform, Tile>> transforms, TransformGroup tg)
        {
            this.transforms = transforms;
            this.tg = tg;
        }

        internal TileRotation()
        {
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

            if(transforms.TryGetValue(tile, out var d))
            {
                return d.TryGetValue(tf, out result);
            }
            else
            {
                result = tile;
                return true;
            }
        }
    }
}
