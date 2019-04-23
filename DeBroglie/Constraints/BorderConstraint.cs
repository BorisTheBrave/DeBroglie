using System;
using System.Collections.Generic;
using System.Linq;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Used by <see cref="BorderConstraint"/> to indicate what area affected.
    /// </summary>
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

    /// <summary>
    /// BorderConstraint class restricts what tiles can be selected in various regions of the output. 
    /// 
    /// For each affected location, BorderConstratin calls Select with the Tile specified.If the Ban field is set, then it calls Ban instead of Select.
    /// </summary>
    public class BorderConstraint : ITileConstraint
    {
        /// <summary>
        /// The tiles to select or ban fromthe  border area.
        /// </summary>
        public Tile[] Tiles { get; set; }

        /// <summary>
        /// A set of flags specifying which sides of the output are affected by the constraint. 
        /// </summary>
        public BorderSides Sides { get; set; } = BorderSides.All;

        /// <summary>
        /// These locations are subtracted from the ones specified in <see cref="Sides"/>. Defaults to empty.
        /// </summary>
        public BorderSides ExcludeSides { get; set; } = BorderSides.None;

        /// Inverts the area specified by <see cref="Sides"/> and <see cref="ExcludeSides"/>
        public bool InvertArea { get; set; }

        /// <summary>
        /// If true, ban <see cref="Tile"/> from the area. Otherwise, select it (i.e. ban every other tile).
        /// </summary>
        public bool Ban { get; set; }

        public void Check(TilePropagator propagator)
        {
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

        public void Init(TilePropagator propagator)
        {
            var tiles = propagator.CreateTileSet(Tiles);

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

                        var match = (Match(Sides, xmin, xmax, ymin, ymax, zmin, zmax) &&
                           !Match(ExcludeSides, xmin, xmax, ymin, ymax, zmin, zmax)) != InvertArea;

                        if (match)
                        {
                            if (Ban)
                            {
                                propagator.Ban(x, y, z, tiles);
                            }
                            else
                            {
                                propagator.Select(x, y, z, tiles);
                            }
                        }
                    }
                }
            }
        }
    }
}
