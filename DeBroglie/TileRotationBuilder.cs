using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{
    public enum TileRotationTreatment
    {
        Missing,
        Unchanged,
        Generated,
    }


    /// <summary>
    /// Builds a <see cref="TileRotation"/>.
    /// This class lets you specify some transformations between tiles via rotation and reflection.
    /// It then infers the full set of transformations possible, and informs you if there are contradictions.
    /// 
    /// Tiles added here are assumed to have no transformations except those listed here.
    /// Tiles that are unlisted are assumed to always transform to themselves (i.e. fully symmetric).
    /// 
    /// As an example of inference, if a square tile 1 transforms to tile 2 when rotated clockwise, and tile 2 transforms to itself when reflected in the x-axis,
    /// then we can infer that tile 1 must transform to tile 1 when reflected in the y-axis.
    /// </summary>
    public class TileRotationBuilder
    {
        private Dictionary<Tile, RotationGroup> tileToRotationGroup = new Dictionary<Tile, RotationGroup>();

        private TransformGroup tg;

        private TileRotationTreatment defaultTreatment;

        public TileRotationBuilder(TileRotationTreatment defaultTreatment = TileRotationTreatment.Missing)
        {
            this.defaultTreatment = defaultTreatment;
        }

        /// <summary>
        /// Indicates that if you reflect then rotate clockwise the src tile as indicated, then you get the dest tile.
        /// </summary>
        public void Add(Tile src, int rotateCw, bool reflectX, Tile dest)
        {
            var tf = new Transform
            {
                ReflectX = reflectX,
                RotateCw = rotateCw,
            };

            GetGroup(src, out var srcRg);
            GetGroup(dest, out var destRg);
            // Groups need merging
            if(srcRg != destRg)
            {
                var srcR = srcRg.GetTransforms(src)[0];
                var destR = destRg.GetTransforms(dest)[0];

                // Arrange destRG so that it is relatively rotated
                // to srcRG as specified by r.
                destRg.Permute(rot => tg.Mul(rot, tg.Inverse(destR), srcR, tf));

                // Attempt to copy over tiles
                srcRg.Entries.AddRange(destRg.Entries);
                foreach (var kv in destRg.Tiles)
                {
                    Set(srcRg, kv.Key, kv.Value, $"record rotation from {src} to {dest} by {tf}");
                }
            }
            srcRg.Entries.Add(new Entry
            {
                Src = src,
                Tf = tf,
                Dest = dest,
            });
            Expand(srcRg);
        }

        private bool Set(RotationGroup rg, Transform tf, Tile tile, string action)
        {
            if(rg.Tiles.TryGetValue(tf, out var current))
            {
                if(current != tile)
                {
                    throw new Exception($"Cannot {action}: conflict between {current} and {tile}");
                }
                return false;
            }
            rg.Tiles[tf] = tile;
            tileToRotationGroup[tile] = rg;
            return true;
        }

        public void SetTreatment(Tile tile, TileRotationTreatment treatment)
        {
            GetGroup(tile, out var rg);
            if(rg.Treatment != null && rg.Treatment !=treatment)
            {
                throw new Exception($"Cannot set {tile} treatment, inconsistent with {rg.Treatment} of {rg.TreatmentSetBy}");
            }
            rg.Treatment = treatment;
            rg.TreatmentSetBy = tile;
        }

        public void AddSymmetry(Tile tile, TileSymmetry ts)
        {
            // I've listed the subgroups in the order found here:
            // https://groupprops.subwiki.org/wiki/Subgroup_structure_of_dihedral_group:D8
            switch (ts)
            {
                case TileSymmetry.F:
                    break;
                case TileSymmetry.N:
                    Add(tile, 2, false, tile);
                    break;

                case TileSymmetry.T:
                    Add(tile, 0, true, tile);
                    break;
                case TileSymmetry.L:
                    Add(tile, 1, true, tile);
                    break;
                case TileSymmetry.E:
                    Add(tile, 2, true, tile);
                    break;
                case TileSymmetry.Q:
                    Add(tile, 3, true, tile);
                    break;

                case TileSymmetry.I:
                    Add(tile, 0, true, tile);
                    Add(tile, 2, false, tile);
                    break;
                case TileSymmetry.Slash:
                    Add(tile, 1, true, tile);
                    Add(tile, 2, false, tile);
                    break;

                case TileSymmetry.Cyclic:
                    Add(tile, 1, false, tile);
                    break;

                case TileSymmetry.X:
                    Add(tile, 0, true, tile);
                    Add(tile, 1, false, tile);
                    break;
            }
        }

        /// <summary>
        /// Extracts the full set of rotations
        /// </summary>
        /// <returns></returns>
        public TileRotation Build()
        {
            // For a given tile (found in a given rotation group)
            // Find the full set of tiles it rotates to.
            IDictionary<Transform, Tile> GetDict(Tile t, RotationGroup rg)
            {
                var tf = rg.GetTransforms(t)[0];
                var treatment = rg.Treatment ?? defaultTreatment;
                var result = new Dictionary<Transform, Tile>();
                foreach(var tf2 in tg.Transforms)
                {
                    if (!rg.Tiles.TryGetValue(tf2, out var dest))
                    {
                        switch(treatment)
                        {
                            case TileRotationTreatment.Missing: continue;
                            case TileRotationTreatment.Unchanged:
                                dest = t;
                                break;
                            case TileRotationTreatment.Generated:
                                var tf3 = tg.Mul(tg.Inverse(tf), tf2);
                                dest = new Tile(new RotatedTile { RotateCw = tf3.RotateCw, ReflectX = tf3.ReflectX, Tile = t });
                                break;
                        }
                    }
                    result[tg.Mul(tg.Inverse(tf), tf2)] = dest;
                }
                return result;
            }

            return new TileRotation(tileToRotationGroup.ToDictionary(kv => kv.Key, kv => GetDict(kv.Key, kv.Value)), tg);
        }

        private void GetGroup(Tile tile, out RotationGroup rg)
        {
            if(tileToRotationGroup.TryGetValue(tile, out rg))
            {
                return;
            }

            rg = new RotationGroup();
            rg.Tiles[new Transform()] = tile;
            tileToRotationGroup[tile] = rg;
        }

        private void Expand(RotationGroup rg)
        {
            bool expanded;
            do
            {
                expanded = false;
                foreach (var entry in rg.Entries)
                {
                    foreach (var kv in rg.Tiles.ToList())
                    {
                        if (kv.Value == entry.Src)
                        {
                            expanded = expanded || Set(rg, tg.Mul(kv.Key, entry.Tf), entry.Dest, "resolve conflicting rotations");
                        }
                        if (kv.Value == entry.Dest)
                        {
                            expanded = expanded || Set(rg, tg.Mul(kv.Key, tg.Inverse(entry.Tf)), entry.Src, "resolve conflicting rotations");
                        }
                    }
                }
            } while (expanded);
        }




        /// <summary>
        /// Stores a set of tiles related to each other by transformations.
        /// If we have two key value pairs (k1, v1) and (k2, v2) in Tiles, then 
        /// we can apply rortaion (k1.Inverse() * k2) to rotate v1 to v2.
        /// </summary>
        private class RotationGroup
        {
            public List<Entry> Entries { get; set; } = new List<Entry>();
            public Dictionary<Transform, Tile> Tiles { get; set; } = new Dictionary<Transform, Tile>();
            public TileRotationTreatment? Treatment { get; set; }
            public Tile TreatmentSetBy { get; set; }


            // A tile may appear multiple times in a rotation group if it is symmetric in some way.
            public List<Transform> GetTransforms(Tile tile)
            {
                return Tiles.Where(kv => kv.Value == tile).Select(x => x.Key).ToList();
            }
        
            public void Permute(Func<Transform, Transform> f)
            {
                Tiles = Tiles.ToDictionary(kv => f(kv.Key), kv => kv.Value);
            }
        }

        private class Entry
        {
            public Tile Src { get; set; }
            public Transform Tf { get; set; }
            public Tile Dest { get; set; }
        }
    }
}
