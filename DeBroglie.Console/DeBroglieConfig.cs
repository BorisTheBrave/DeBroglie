using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeBroglie.Console
{

    public class DeBroglieConfig
    {
        public string Dest { get; set; }

        public string Src { get; set; }

        [JsonConverter(typeof(ModelConverter))]
        public ModelConfig Model { get; set; }

        public string PeriodicInput { get; set; } = "true";

        public bool IsPeriodicInput => PeriodicInput.ToLower() == "true";

        public string Periodic { get; set; } = "false";

        public bool IsPeriodic => Periodic.ToLower() == "true";

        public int Width { get; set; } = 48;

        public int Height { get; set; } = 48;

        public int Depth { get; set; } = 48;

        public bool Backtrack { get; set; }

        public List<TileData> Tiles { get; set; }

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

        public int N { get; set; } = 2;

        public int Symmetry { get; set; } = 8;

        public string Ground { get; set; }
    }

    public class Adjacent : ModelConfig
    {
        public const string ModelTypeString = "adjacent";

        public override string Type => ModelTypeString;
    }

    public class TileData
    {
        public string Value { get; set; }

        public string ChangeFrequency { get; set; }

        public string ReflectX { get; set; }

        public string ReflectY { get; set; }

        public string RotateCw { get; set; }

        public string RotateCcw { get; set; }

        public bool NoRotate { get; set; }
    }

    public abstract class ConstraintConfig
    {
        public virtual string Type { get; }
    }

    public class PathConfig : ConstraintConfig
    {
        public const string TypeString = "path";

        public override string Type => TypeString;

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
