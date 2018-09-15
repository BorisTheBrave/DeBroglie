using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeBroglie.Console
{

    public class DeBroglieConfig
    {
        /// <summary>
        /// The file to load as the initial sample.
        /// </summary>
        public string Dest { get; set; }

        /// <summary>
        /// The file to write the result to. The file has the same format as Src. 
        /// </summary>
        public string Src { get; set; }

        /// <summary>
        /// The directory that Src and Dest are relative to. 
        /// BaseDirectory itself is relative to the directory of the config file, and defaults to that directory.
        /// </summary>
        public string BaseDirectory { get; set; }

        /// <summary>
        /// Specifies the model to use. Defaults to adjacent.
        /// </summary>
        [JsonConverter(typeof(ModelConverter))]
        public ModelConfig Model { get; set; }

        /// <summary>
        /// Shorthand for setting PeriodicInputX, PeriodicInputY, PeriodicInputZ.
        /// </summary>
        public bool PeriodicInput { set { PeriodicInputX = PeriodicInputY = PeriodicInputZ = value; } }

        /// <summary>
        /// Does the input image wrap around on the x axis?
        /// </summary>
        public bool PeriodicInputX { get; set; } = false;

        /// <summary>
        /// Does the input image wrap around on the y axis?
        /// </summary>
        public bool PeriodicInputY { get; set; } = false;

        /// <summary>
        /// Does the input image wrap around on the z axis?
        /// </summary>
        public bool PeriodicInputZ { get; set; } = false;

        /// <summary>
        /// Shorthand for setting PeriodicX, PeriodicY, PeriodicZ.
        /// </summary>
        public bool Periodic { set { PeriodicX = PeriodicY = PeriodicZ = value; } }

        /// <summary>
        /// Should the output wrap around on the x axis.
        /// </summary>
        public bool PeriodicX { get; set; } = false;

        /// <summary>
        /// Should the output wrap around on the y axis.
        /// </summary>
        public bool PeriodicY { get; set; } = false;

        /// <summary>
        /// Should the output wrap around on the z axis.
        /// </summary>
        public bool PeriodicZ { get; set; } = false;

        /// <summary>
        /// Length of the x-axis in pixels / tiles of the output result.
        /// </summary>
        public int Width { get; set; } = 48;

        /// <summary>
        /// Length of the y-axis in pixels / tiles of the output result.
        /// </summary>
        public int Height { get; set; } = 48;

        /// <summary>
        /// Length of the z-axis in pixels / tiles of the output result.
        /// </summary>
        public int Depth { get; set; } = 48;

        /// <summary>
        /// Shorthand for adding a pair of border constraints. 
        /// The first one constrains the bottom of the output to be the specified tile. 
        /// The second bans the tile from all other locations. 
        /// The bottom is taken to be ymax for 2d generation, zmin for 3d.
        /// </summary>
        public string Ground { get; set; }

        /// <summary>
        /// Shorthand for setting reflectionalSymmetry and rotationalSymmetry. 
        /// If even, reflections are on, and rotations is half symmetry. 
        /// Otherwise reflections are off and rotations are equal to symmetry.
        /// </summary>
        public int Symmetry
        {
            set
            {
                if (value % 2 == 0)
                {
                    RotationalSymmetry = value / 2;
                    ReflectionalSymmetry = true;
                }
                else
                {
                    RotationalSymmetry = value;
                    ReflectionalSymmetry = false;
                }
            }
        }

        /// <summary>
        /// If set, extra copies of the Src are used as samples.
        /// </summary>
        public int RotationalSymmetry { get; set; } = 1;

        /// <summary>
        /// If set, extra copies of the Src are used as samples.
        /// </summary>
        public bool ReflectionalSymmetry { get; set; } = false;

        /// <summary>
        /// Specifies if backtracking is enabled.
        /// </summary>
        public bool Backtrack { get; set; }

        /// <summary>
        /// Dumps snapshots of the output while the generation process is running. Experimenta.
        /// </summary>
        public bool Animate { get; set; }

        /// <summary>
        /// Specifies various per-tile information.
        /// </summary>
        public List<TileData> Tiles { get; set; }

        /// <summary>
        /// Specifies constraints to add.
        /// </summary>
        [JsonConverter(typeof(ConstraintArrayConverter))]
        public List<ConstraintConfig> Constraints { get; set; }
    }

    public abstract class ModelConfig
    {
        public virtual string Type { get; }
    }

    public class Overlapping : ModelConfig
    {
        public const string ModelTypeString = "overlapping";

        public override string Type => ModelTypeString;

        /// <summary>
        /// Shorthand for setting `NX`, `NY` and `NZ`
        /// </summary>
        public int N { set { NX = NY = NZ = value; } }

        /// <summary>
        /// Size of the rectangles to sample along the x-axis. Default 2.
        /// </summary>
        public int NX { get; set; } = 2;

        /// <summary>
        /// Size of the rectangles to sample along the y-axis. Default 2.
        /// </summary>
        public int NY { get; set; } = 2;

        /// <summary>
        /// Size of the rectangles to sample along the y-axis. Default 2.
        /// </summary>
        public int NZ { get; set; } = 2;
    }

    public class Adjacent : ModelConfig
    {
        public const string ModelTypeString = "adjacent";

        public override string Type => ModelTypeString;
    }

    public class TileData
    {
        public string Value { get; set; }

        public string MultiplyFrequency { get; set; }

        public string TileSymmetry { get; set; }

        public string ReflectX { get; set; }

        public string ReflectY { get; set; }

        public string RotateCw { get; set; }

        public string RotateCcw { get; set; }

        public TileRotationTreatment? RotationTreatment { get; set; }
    }

    public abstract class ConstraintConfig
    {
        public virtual string Type { get; }
    }

    public class PathConfig : ConstraintConfig
    {
        public const string TypeString = "path";

        public override string Type => TypeString;

        /// <summary>
        /// The set of tiles that are considered "on the path".
        /// </summary>
        public string[] PathTiles { get; set; }
    }

    public class BorderConfig : ConstraintConfig
    {
        public const string TypeString = "border";

        public override string Type => TypeString;

        public string Tile { get; set; }

        public string Sides { get; set; }

        public string ExcludeSides { get; set; }

        public bool InvertArea { get; set; }

        public bool Ban { get; set; }
    }
}
