using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Constraints
{
    [Flags]
    public enum BorderSides
    {
        None = 0,
        XMin = 0x01,
        XMax = 0x02,
        YMin = 0x04,
        YMax = 0x08,
        ZMin = 0x10,
        ZMax = 0x20,
        All = 0x3F,
    }

    public class BorderConstraint : ITileConstraint
    {
        public Tile Tile { get; set; }
        public BorderSides Sides { get; set; } = BorderSides.All;
        public BorderSides ExcludeSides { get; set; } = BorderSides.None;

        public CellStatus Check(TilePropagator propagator)
        {
            return CellStatus.Undecided;
        }

        private bool Match(BorderSides sides, bool xmin, bool xmax, bool ymin, bool ymax, bool zmin, bool zmax)
        {
            return
                xmin && sides.HasFlag(BorderSides.XMin) ||
                xmax && sides.HasFlag(BorderSides.XMax) ||
                ymin && sides.HasFlag(BorderSides.YMin) ||
                ymax && sides.HasFlag(BorderSides.YMax) ||
                zmin && sides.HasFlag(BorderSides.ZMin) ||
                zmax && sides.HasFlag(BorderSides.ZMax);

        }

        public CellStatus Init(TilePropagator propagator)
        {
            var width = propagator.Topology.Width;
            var height = propagator.Topology.Height;
            var depth = propagator.Topology.Depth;
            for (var x = 0; x < width; x++)
            {
                var xmin = x == 0;
                var xmax = x == width - 1;

                for (var y = 0; y < height; y++)
                {
                    var ymin = y == 0;
                    var ymax = y == height - 1;

                    for (var z = 0; z < depth; z++)
                    {
                        var zmin = z == 0;
                        var zmax = z == depth - 1;

                        if(Match(Sides, xmin, xmax, ymin, ymax, zmin, zmax) && 
                           !Match(ExcludeSides, xmin, xmax, ymin, ymax, zmin, zmax))
                        {
                            propagator.Select(x, y, z, Tile);
                        }
                    }
                }
            }
            return CellStatus.Undecided;
        }
    }
}
