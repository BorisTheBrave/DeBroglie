using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie
{
    public class TileRotationBuilder
    {
        private Dictionary<Tile, RotationGroup> tileToRotationGroup = new Dictionary<Tile, RotationGroup>();

        private int rotationalSymmetry = 4;

        public void NoRotate(Tile tile)
        {
            GetGroup(tile, out var rg);
        }

        public void Add(Tile src, int rotate, bool reflectX, Tile dest)
        {
            var r = new Rotation
            {
                ReflectX = reflectX,
                Rotate = rotate,
            };

            GetGroup(src, out var srcRg);
            GetGroup(dest, out var destRg);
            // Groups need merging
            if(srcRg != destRg)
            {
                var srcR = srcRg.GetRotations(src)[0];
                var destR = destRg.GetRotations(dest)[0];

                // Arrange destRG so that it is relatively rotated
                // to srcRG as specified by r.
                destRg.Permute(rot => Mul(rot, destR.Inverse(), srcR, r));

                // Attempt to copy over tiles
                srcRg.Entries.AddRange(destRg.Entries);
                foreach (var kv in destRg.Tiles)
                {
                    Set(srcRg, kv.Key, kv.Value, $"record rotation from {src} to {dest} by {r}");
                }
            }
            srcRg.Entries.Add(new Entry
            {
                Src = src,
                R = r,
                Dest = dest,
            });
            Expand(srcRg);
        }

        private bool Set(RotationGroup rg, Rotation r, Tile tile, string action)
        {
            if(rg.Tiles.TryGetValue(r, out var current))
            {
                if(current != tile)
                {
                    throw new Exception($"Cannot {action}: conflict between {current} and {tile}");
                }
                return false;
            }
            rg.Tiles[r] = tile;
            tileToRotationGroup[tile] = rg;
            return true;
        }

        public TileRotation Build()
        {
            IDictionary<Tuple<int, bool>, Tile> GetDict(Tile t, RotationGroup rg)
            {
                var r = rg.GetRotations(t)[0];
                return rg.Tiles.ToDictionary(kv =>
                {
                    var rot = Mul(r.Inverse(), kv.Key);
                    return Tuple.Create(rot.Rotate, rot.ReflectX);
                }, kv => kv.Value);
            }

            return new TileRotation(tileToRotationGroup.ToDictionary(kv => kv.Key, kv => GetDict(kv.Key, kv.Value)));
        }

        private void GetGroup(Tile tile, out RotationGroup rg)
        {
            if(tileToRotationGroup.TryGetValue(tile, out rg))
            {
                return;
            }

            rg = new RotationGroup();
            rg.Tiles[new Rotation()] = tile;
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
                            expanded = expanded || Set(rg, Mul(kv.Key, entry.R), entry.Dest, "resolve conflicting rotations");
                        }
                        if (kv.Value == entry.Dest)
                        {
                            expanded = expanded || Set(rg, Mul(kv.Key, entry.R.Inverse()), entry.Src, "resolve conflicting rotations");
                        }
                    }
                }
            } while (expanded);
        }

        private Rotation Mul(Rotation a, Rotation b)
        {
            var r = new Rotation
            {
                Rotate = (b.ReflectX ? -a.Rotate : a.Rotate) + b.Rotate,
                ReflectX = a.ReflectX ^ b.ReflectX,
            };
            r.Rotate = (r.Rotate + rotationalSymmetry) % rotationalSymmetry;
            return r;
        }

        private Rotation Mul(Rotation a, Rotation b, Rotation c)
        {
            return Mul(Mul(a, b), c);
        }

        private Rotation Mul(Rotation a, Rotation b, Rotation c, Rotation d)
        {
            return Mul(Mul(Mul(a, b), c), d);
        }


        /// <summary>
        /// Stores a set of tiles related to each other by rotations and transformations.
        /// If we have two key value pairs (k1, v1) and (k2, v2) in Tiles, then 
        /// we can apply rortaion (k1.Inverse() * k2) to rotate v1 to v2.
        /// </summary>
        private class RotationGroup
        {
            public List<Entry> Entries { get; set; } = new List<Entry>();
            public Dictionary<Rotation, Tile> Tiles { get; set; } = new Dictionary<Rotation, Tile>();

            // A tile may appear multiple times in a rotation group if it is symmetric in some way.
            public List<Rotation> GetRotations(Tile tile)
            {
                return Tiles.Where(kv => kv.Value == tile).Select(x => x.Key).ToList();
            }
        
            public void Permute(Func<Rotation, Rotation> f)
            {
                Tiles = Tiles.ToDictionary(kv => f(kv.Key), kv => kv.Value);
            }
        }

        private class Entry
        {
            public Tile Src { get; set; }
            public Rotation R { get; set; }
            public Tile Dest { get; set; }
        }

        private class Rotation
        {
            public int Rotate { get; set; }
            public bool ReflectX { get; set; }

            public override bool Equals(object obj)
            {
                if(obj is Rotation other)
                {
                    return Rotate == other.Rotate && ReflectX == other.ReflectX;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Rotate * 2 + (ReflectX ? 1 : 0);
            }

            public override string ToString()
            {
                return $"({Rotate}, {ReflectX})";
            }

            public Rotation Inverse()
            {
                return new Rotation
                {
                    Rotate = ReflectX ? Rotate : -Rotate,
                    ReflectX = ReflectX,
                };
            }
        }
    }
}
