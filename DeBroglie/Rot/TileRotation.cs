using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Rot
{

    /// <summary>
    /// Describes which rotations and reflections are allowed, and
    /// and stores how to process each tile during a rotation.
    /// These are constructed with a <see cref="TileRotationBuilder"/>
    /// </summary>
    public class TileRotation
    {
        private readonly IDictionary<Tile, IDictionary<Rotation, Tile>> rotations;
        private readonly IDictionary<Tile, TileRotationTreatment> treatments;
        private readonly TileRotationTreatment defaultTreatment;
        private readonly RotationGroup rotationGroup;

        // Used by TileRotationBuilder
        internal TileRotation(
            IDictionary<Tile, IDictionary<Rotation, Tile>> rotations,
            IDictionary<Tile, TileRotationTreatment> treatments,
            TileRotationTreatment defaultTreatment, 
            RotationGroup rotationGroup)
        {
            this.rotations = rotations;
            this.treatments = treatments;
            this.defaultTreatment = defaultTreatment;
            this.rotationGroup = rotationGroup;
        }

        /// <summary>
        /// Constructs a TileRotation that allows rotations and reflections as passed in,
        /// but leaves all tiles unchanged when rotating.
        /// <paramref name="rotationalSymmetry"></paramref>
        /// </summary>
        /// <param name="rotationalSymmetry">Permits rotations of 360 / rotationalSymmetry</param>
        /// <param name="reflectionalSymmetry">If true, reflections in the x-axis are permited</param>
        public TileRotation(int rotationalSymmetry, bool reflectionalSymmetry)
        {
            this.treatments = new Dictionary<Tile, TileRotationTreatment>();
            this.defaultTreatment = TileRotationTreatment.Unchanged;
            this.rotationGroup = new RotationGroup(rotationalSymmetry, reflectionalSymmetry);
        }

        /// <summary>
        /// A TileRotation that permits no rotation at all.
        /// </summary>
        public TileRotation():this(1, false)
        {

        }

        public RotationGroup RotationGroup => rotationGroup;

        /// <summary>
        /// Attempts to reflect, then rotate clockwise, a given Tile.
        /// If there is a corresponding tile (possibly the same one), then it is set to result.
        /// Otherwise, false is returned.
        /// </summary>
        public bool Rotate(Tile tile, Rotation rotation, out Tile result)
        {
            if(rotationGroup != null && tile.Value is RotatedTile rt)
            {
                rotation = rt.Rotation * rotation;
                tile = rt.Tile;
            }

            if(rotations != null && rotations.TryGetValue(tile, out var d))
            {
                if (d.TryGetValue(rotation, out result))
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
                    if (rotation.IsIdentity)
                        result = tile;
                    else
                        result = new Tile(new RotatedTile { Rotation = rotation, Tile = tile });
                    return true;
                default:
                    throw new Exception($"Unknown treatment {treatment}");
            }
        }

        /// <summary>
        /// Convenience method for calling Rotate on each tile in a list, skipping any that cannot be rotated.
        /// </summary>
        public IEnumerable<Tile> Rotate(IEnumerable<Tile> tiles, Rotation rotation)
        {
            foreach(var tile in tiles)
            {
                if(Rotate(tile, rotation, out var tile2))
                {
                    yield return tile2;
                }
            }
        }

        public IEnumerable<Tile> RotateAll(Tile tile)
        {
            foreach (var rotation in RotationGroup)
            {
                if (Rotate(tile, rotation, out var tile2))
                {
                    yield return tile2;
                }
            }
        }

        public IEnumerable<Tile> RotateAll(IEnumerable<Tile> tiles)
        {
            return tiles.SelectMany(tile => RotateAll(tile));
        }

        /// <summary>
        /// For a rotated tile, finds the canonical representation.
        /// Leaves all other tiles unchanged.
        /// </summary>
        public Tile Canonicalize(Tile t)
        {
            if(t.Value is RotatedTile rt)
            {
                if (!Rotate(rt.Tile, rt.Rotation, out var result))
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
