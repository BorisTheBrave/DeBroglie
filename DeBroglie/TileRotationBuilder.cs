﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{
    public enum TileRotationTreatment
    {
        Missing,
        Unchanged,
        /// <summary>
        /// Experimental, this doesn't work properly yet.
        /// </summary>
        Generated,
    }


    /// <summary>
    /// Builds a <see cref="TileRotation"/>.
    /// This class lets you specify some transformations between tiles via rotation and reflection.
    /// It then infers the full set of transformations possible, and informs you if there are contradictions.
    /// 
    /// As an example of inference, if a square tile 1 transforms to tile 2 when rotated clockwise, and tile 2 transforms to itself when reflected in the x-axis,
    /// then we can infer that tile 1 must transform to tile 1 when reflected in the y-axis.
    /// </summary>
    public class TileRotationBuilder
    {
        private Dictionary<Tile, SubGroup> tileToSubGroup = new Dictionary<Tile, SubGroup>();

        private TransformGroup tg;

        private TileRotationTreatment defaultTreatment;

        public TileRotationBuilder(TileRotationTreatment defaultTreatment = TileRotationTreatment.Unchanged)
        {
            this.tg = new TransformGroup();
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

            GetGroup(src, out var srcSg);
            GetGroup(dest, out var destSg);
            // Groups need merging
            if(srcSg != destSg)
            {
                var srcR = srcSg.GetTransforms(src)[0];
                var destR = destSg.GetTransforms(dest)[0];

                // Arrange destRG so that it is relatively rotated
                // to srcRG as specified by r.
                destSg.Permute(rot => tg.Mul(rot, tg.Inverse(destR), srcR, tf));

                // Attempt to copy over tiles
                srcSg.Entries.AddRange(destSg.Entries);
                foreach (var kv in destSg.Tiles)
                {
                    Set(srcSg, kv.Key, kv.Value, $"record rotation from {src} to {dest} by {tf}");
                    tileToSubGroup[kv.Value] = srcSg;
                }
            }
            srcSg.Entries.Add(new Entry
            {
                Src = src,
                Tf = tf,
                Dest = dest,
            });
            Expand(srcSg);
        }

        private bool Set(SubGroup sg, Transform tf, Tile tile, string action)
        {
            if(sg.Tiles.TryGetValue(tf, out var current))
            {
                if(current != tile)
                {
                    throw new Exception($"Cannot {action}: conflict between {current} and {tile}");
                }
                return false;
            }
            sg.Tiles[tf] = tile;
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

        /// <summary>
        /// Declares that a tile is symetric, and therefore transforms to iteself.
        /// This is a shorthand for calling Add(tile,..., tile) with the list of transformations
        /// related to the symmetry.
        /// </summary>
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
            IDictionary<Transform, Tile> GetDict(Tile t, SubGroup sg)
            {
                var treatment = sg.Treatment ?? defaultTreatment;
                if(treatment == TileRotationTreatment.Generated)
                {
                    sg = Clone(sg);
                    Generate(sg);
                }
                var tf = sg.GetTransforms(t)[0];
                var result = new Dictionary<Transform, Tile>();
                foreach(var tf2 in tg.Transforms)
                {
                    if (!sg.Tiles.TryGetValue(tf2, out var dest))
                    {
                        continue;
                    }
                    result[tg.Mul(tg.Inverse(tf), tf2)] = dest;
                }
                return result;
            }
            return new TileRotation(
                tileToSubGroup.ToDictionary(kv => kv.Key, kv => GetDict(kv.Key, kv.Value)),
                tileToSubGroup.Where(kv=>kv.Value.Treatment.HasValue).ToDictionary(kv => kv.Key, kv => kv.Value.Treatment.Value),
                defaultTreatment,
                tg);
        }

        // Gets the rotation group containing Tile, creating it if it doesn't exist
        private void GetGroup(Tile tile, out SubGroup sg)
        {
            if(tileToSubGroup.TryGetValue(tile, out sg))
            {
                return;
            }

            sg = new SubGroup();
            sg.Tiles[new Transform()] = tile;
            tileToSubGroup[tile] = sg;
        }

        // Ensures that rg.Tiles is fully filled in
        // according to rg.Entries.
        private void Expand(SubGroup sg)
        {
            bool expanded;
            do
            {
                expanded = false;
                foreach (var entry in sg.Entries)
                {
                    foreach (var kv in sg.Tiles.ToList())
                    {
                        if (kv.Value == entry.Src)
                        {
                            expanded = expanded || Set(sg, tg.Mul(kv.Key, entry.Tf), entry.Dest, "resolve conflicting rotations");
                        }
                        if (kv.Value == entry.Dest)
                        {
                            expanded = expanded || Set(sg, tg.Mul(kv.Key, tg.Inverse(entry.Tf)), entry.Src, "resolve conflicting rotations");
                        }
                    }
                }
            } while (expanded);
        }

        private SubGroup Clone(SubGroup sg)
        {
            return new SubGroup
            {
                Entries = sg.Entries.ToList(),
                Tiles = sg.Tiles.ToDictionary(x => x.Key, x => x.Value),
                Treatment = sg.Treatment,
                TreatmentSetBy = sg.TreatmentSetBy,
            };
        }

        // Fills all remaining slots with RotatedTile
        // Care is taken that as few distinct RotatedTiles are used as possible
        // If there's two possible choices, prefernce is given to rotations over reflections.
        private void Generate(SubGroup sg)
        {
            start:
            for (var refl = 0; refl < 2; refl++)
            {
                for (var rot = 0; rot < tg.RotationalSymmetry; rot++)
                {
                    var transform = new Transform { ReflectX = refl > 0, RotateCw = rot };
                    if (sg.Tiles.ContainsKey(transform))
                        continue;

                    // Found an empty spot, figure out what to rotate from
                    for (var refl2 = 0; refl2 < 2; refl2++)
                    {
                        for (var rot2 = 0; rot2 < tg.RotationalSymmetry; rot2++)
                        {
                            var transform2 = new Transform { ReflectX = (refl2 > 0) != (refl > 0), RotateCw = rot2 };
                            if (!sg.Tiles.TryGetValue(transform2, out var srcTile))
                                continue;

                            // Don't allow RotatedTiles to nest.
                            if(srcTile.Value is RotatedTile rt)
                            {
                                srcTile = rt.Tile;
                                var rtTransform = new Transform { RotateCw = rt.RotateCw, ReflectX = rt.ReflectX };
                                transform2 = tg.Mul(tg.Inverse(rtTransform), transform2);
                            }

                            var srcToDest = tg.Mul(tg.Inverse(transform2), transform);

                            Tile destTile;
                            if (srcToDest.ReflectX == false && srcToDest.RotateCw == 0)
                            {
                                destTile = srcTile;
                            }
                            else
                            {
                                destTile = new Tile(new RotatedTile
                                {
                                    Tile = srcTile,
                                    ReflectX = srcToDest.ReflectX,
                                    RotateCw = srcToDest.RotateCw,
                                });
                            }

                            // Found where we want to rotate from
                            sg.Entries.Add(new Entry
                            {
                                Src = srcTile,
                                Tf = srcToDest,
                                Dest = destTile,
                            });
                            Expand(sg);
                            goto start;
                        }
                    }
                }
            }
        }




        /// <summary>
        /// Stores a set of tiles related to each other by transformations.
        /// If we have two key value pairs (k1, v1) and (k2, v2) in Tiles, then 
        /// we can apply rortaion (k1.Inverse() * k2) to rotate v1 to v2.
        /// </summary>
        private class SubGroup
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
