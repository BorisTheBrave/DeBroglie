using DeBroglie.Rot;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Constraints
{
    public class MirrorConstraint : ITileConstraint
    {
        public TileRotation TileRotation { get; set; }

        public Resolution Init(TilePropagator propagator)
        {
            // Strictly speaking, no initialization is needed.
            // In practise, this is important to stop WFC from blundering
            // into easy contradictions.

            // TODO

            return Resolution.Undecided;
        }

        public Resolution Check(TilePropagator propagator)
        {
            var topology = propagator.Topology;
            var reflectX = new Rotation(0, true);
            foreach(var i in topology.Indicies)
            {
                topology.GetCoord(i, out var x, out var y, out var z);
                var x2 = topology.Width - 1 - x;

                foreach(var tile in propagator.TileModel.Tiles)
                {
                    if (TileRotation.Rotate(tile, reflectX, out var tile2))
                    {
                        if (propagator.IsBanned(x, y, z, tile) && !propagator.IsBanned(x2, y, z, tile2))
                        {
                            propagator.Ban(x2, y, z, tile2);
                        }
                    }
                }
            }
            return Resolution.Undecided;
        }
    }
}
